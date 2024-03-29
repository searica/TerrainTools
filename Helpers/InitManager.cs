﻿using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TerrainTools.Configs;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace TerrainTools.Helpers {
    internal static class InitManager {
        private static bool HasInitialized = false;
        internal static readonly Dictionary<string, GameObject> ToolRefs = new();

        /// <summary>
        ///     Track the where pieces have been inserted into each piece table. Used
        ///     to manage the insertion position so that the insertion index is
        ///     not dependent on how many other pieces have been added.
        /// </summary>
        private static readonly Dictionary<string, List<int>> InsertionIndexes = new();

        internal static void InitToolPieces() {
            if (HasInitialized) return;
            FixVanillaToolDescriptions();

            foreach (var key in ToolConfigs.ToolConfigsMap.Keys) {
                try {
                    var toolDB = ToolConfigs.ToolConfigsMap[key];
                    toolDB.prefab = MakeToolPiece(toolDB);
                }
                catch {
                    Log.LogWarning($"Failed to create: {key}");
                }
            }

            HasInitialized = true;

            UpdateTools();
        }

        internal static void FixVanillaToolDescriptions() {
            SetDescription(
               "mud_road_v2",
               "Levels ground based on player position. Use shift + click to level ground based on where you are pointing (this will smooth the terrain)."
            );

            SetDescription(
                "raise_v2",
                "Raise grounds up to a maximum of 8 m above it's original height."
            );

            SetDescription(
                "path_v2",
                "Creates a dirt path without affecting ground height."
            );

            SetDescription(
                "paved_road_v2",
                "Creates a paved path and levels ground based on player position. Use shift+click to level ground based on where you are pointing (this will smooth the terrain)."
            );

            SetDescription(
                "cultivate_v2",
                "Cultivates ground and levels ground based on player position. Use shift + click to level ground based on where you are pointing (this will smooth the terrain)."
            );

            SetDescription(
                "replant_v2",
                "Replants terrain without affecting ground height."
            );
        }

        private static void SetDescription(string prefabName, string description) {
            var prefabPiece = PrefabManager.Instance.GetPrefab(prefabName)?.GetComponent<Piece>();
            if (prefabPiece != null) {
                prefabPiece.m_description = description;
            }
            else {
                Log.LogWarning($"Could not set description for: {prefabName}");
            }
        }

        /// <summary>
        ///     Updates which tools are enabled/disabled and which items are enabled/disabled.
        /// </summary>
        internal static void UpdatePlugin() {
            UpdateTools();
            UpdateShovelRecipe();
            ConfigManager.Save();
            TerrainTools.UpdatePlugin = false;
        }

        /// <summary>
        ///     Updates which tools are enabled/disabled to add/remove them from piece tables.
        /// </summary>
        private static void UpdateTools() {
            if (!HasInitialized) {
                return;
            }
            ForceUnequipTerrainTools();

            foreach (var toolDB in ToolConfigs.ToolConfigsMap.Values) {
                RemovePieceInPieceTable(toolDB);
            }

            foreach (var key in ToolConfigs.ToolConfigsMap.Keys) {
                if (TerrainTools.IsToolEnabled(key)) {
                    var toolDB = ToolConfigs.ToolConfigsMap[key];
                    RegisterPieceInPieceTable(toolDB.prefab, toolDB.pieceTable, null, toolDB.insertIndex);
                }
            }
        }

        internal static void UpdatePieceTables() {
            foreach (var key in ToolConfigs.ToolConfigsMap.Keys) {
                try {
                    var toolDB = ToolConfigs.ToolConfigsMap[key];
                    toolDB.prefab = MakeToolPiece(toolDB);
                    RegisterPieceInPieceTable(toolDB.prefab, toolDB.pieceTable, null, toolDB.insertIndex);
                }
                catch {
                    Log.LogWarning($"Failed to create: {key}");
                }
            }
        }

        /// <summary>
        ///     Updates the shovel recipe to be enabled/disabled based on corresponding config entry.
        /// </summary>
        private static void UpdateShovelRecipe() {
            if (Shovel.TryGetShovel(out CustomItem shovel)) {
                shovel.Recipe.Recipe.m_enabled = TerrainTools.IsShovelEnabled;
            }
        }

        /// <summary>
        ///     Forces hammer to be unequipped if it is currently equipped.
        /// </summary>
        private static void ForceUnequipTerrainTools() {
            if (!Player.m_localPlayer) {
                return;
            }

            var rightItem = Player.m_localPlayer.GetRightItem();
            if (rightItem == null) {
                return;
            }

            if (rightItem.m_shared.m_name == "$item_cultivator") {
                Log.LogWarning($"{TerrainTools.PluginName} updated through config change, unequipping cultivator");
                Player.m_localPlayer.HideHandItems();
            }

            if (rightItem.m_shared.m_name == "$item_hoe") {
                Log.LogWarning($"{TerrainTools.PluginName} updated through config change, unequipping hoe");
                Player.m_localPlayer.HideHandItems();
            }
        }

        internal static GameObject MakeToolPiece(ToolDB toolDB) {
            // prevent duplicates
            if (PieceManager.Instance.GetPiece(toolDB.pieceName) != null) { return null; }

            // clone base prefab and set name
            var toolPrefab = PrefabManager.Instance.CreateClonedPrefab(toolDB.name, toolDB.basePrefab);
            if (toolPrefab == null) { return null; }

            // customize piece component
            var toolPiece = toolPrefab.GetComponent<Piece>();
            toolPiece.m_icon = Sprite.Create(toolDB.icon, new Rect(0, 0, toolDB.icon.width, toolDB.icon.height), Vector2.zero);
            toolPiece.m_name = toolDB.pieceName;
            toolPiece.m_description = toolDB.pieceDesc;
            if (toolDB.requirements != null) {
                toolPiece.m_resources = toolDB.requirements;
            }

            // customize terrain op component
            var settings = toolPrefab.GetComponent<TerrainOp>().m_settings;
            settings.m_level = UpdateValueIfNeeded(settings.m_level, toolDB.level);
            settings.m_levelRadius = UpdateValueIfNeeded(settings.m_levelRadius, toolDB.levelRadius);

            settings.m_raise = UpdateValueIfNeeded(settings.m_raise, toolDB.raise);
            settings.m_raiseRadius = UpdateValueIfNeeded(settings.m_raiseRadius, toolDB.raiseRadius);
            settings.m_raisePower = UpdateValueIfNeeded(settings.m_raisePower, toolDB.raisePower);
            settings.m_raiseDelta = UpdateValueIfNeeded(settings.m_raiseDelta, toolDB.raiseDelta);

            settings.m_smooth = UpdateValueIfNeeded(settings.m_smooth, toolDB.smooth);
            settings.m_smoothRadius = UpdateValueIfNeeded(settings.m_smoothRadius, toolDB.smoothRadius);
            settings.m_smoothPower = UpdateValueIfNeeded(settings.m_smoothPower, toolDB.smoothPower);

            settings.m_paintCleared = UpdateValueIfNeeded(settings.m_paintCleared, toolDB.clearPaint);
            settings.m_paintRadius = UpdateValueIfNeeded(settings.m_paintRadius, toolDB.paintRadius);

            // apply custom visualization overlay if desired
            if (toolDB.overlayType != null) {
                toolPrefab.AddComponent(toolDB.overlayType);
            }

            if (toolDB.invertGhost) {
                var ghost = toolPrefab.transform.Find("_GhostOnly");
                if (ghost != null) {
                    ghost.localRotation = Quaternion.Euler(270f, 0f, 0f);
                    ghost.localPosition += new Vector3(0f, 2f, 0f);
                }
            }
            return toolPrefab;
        }

        private static T UpdateValueIfNeeded<T>(T source, T? newValue) where T : struct {
            return newValue != null ? newValue.Value : source;
        }

        private static T UpdateValueIfNeeded<T>(T source, T newValue) {
            return newValue != null ? newValue : source;
        }

        /// <summary>
        ///     Register a single piece prefab into a piece table by name.<br />
        ///     Also adds the prefab to the <see cref="PrefabManager"/> and <see cref="ZNetScene"/> if necessary.<br />
        ///     Custom categories can be referenced if they have been added to the manager before.<br />
        ///     No mock references are fixed.
        /// </summary>
        /// <param name="prefab"><see cref="GameObject"/> with a <see cref="Piece"/> component to add to the table</param>
        /// <param name="pieceTable">Prefab or item name of the PieceTable</param>
        /// <param name="category">Optional category string, does not create new custom categories</param>
        private static void RegisterPieceInPieceTable(GameObject prefab, string pieceTable, string category, int position = -1) {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null) {
                throw new Exception($"Prefab {prefab.name} has no Piece component attached");
            }

            var table = PieceManager.Instance.GetPieceTable(pieceTable);
            if (table == null) {
                throw new Exception($"Could not find PieceTable {pieceTable}");
            }

            if (table.m_pieces.Contains(prefab)) {
                Log.LogDebug($"Already added piece {prefab.name}");
                return;
            }

            var name = prefab.name;
            var hash = name.GetStableHashCode();
            if (ZNetScene.instance != null && !ZNetScene.instance.m_namedPrefabs.ContainsKey(hash)) {
                PrefabManager.Instance.RegisterToZNetScene(prefab);
            }

            if (!string.IsNullOrEmpty(category)) {
                piece.m_category = PieceManager.Instance.AddPieceCategory(pieceTable, category);
            }

            if (!InsertionIndexes.ContainsKey(pieceTable)) {
                InsertionIndexes[pieceTable] = new List<int>();
            }

            // Shift position to account for how many pieces have been added before it and check if OOB
            var index = position + InsertionIndexes[pieceTable].Where(x => x <= position).Count();
            if (index >= table.m_pieces.Count) {
                Log.LogWarning("Piece insertion index is out of bounds");
            }

            if (position >= 0 && index < table.m_pieces.Count) {
                // Add piece at new insertion point
                table.m_pieces.Insert(index, prefab);
                InsertionIndexes[pieceTable].Add(position);
            }
            else {
                table.m_pieces.Add(prefab);
                InsertionIndexes[pieceTable].Add(table.m_pieces.Count - 1);
            }

            Log.LogDebug($"Added piece {prefab.name} | Token: {piece.TokenName()}");
        }


        /// <summary>
        ///     Removes prefab from piece table and updates insertion indexes
        /// </summary>
        /// <param name="toolDB"><see cref="ToolDB"/></param>
        private static void RemovePieceInPieceTable(ToolDB toolDB) {
            var prefab = toolDB.prefab;
            if (!prefab) {
                return;
            }

            var table = PieceManager.Instance.GetPieceTable(toolDB.pieceTable);
            if (table == null) {
                throw new Exception($"Could not find PieceTable {toolDB.pieceTable}");
            }

            // Check if prefab exists in list and get index
            var pos = table.m_pieces.IndexOf(prefab);
            if (pos < 0) {
                return;
            }

            table.m_pieces.Remove(prefab);
            Log.LogDebug($"Removed piece {prefab.name}");

            // Update insertion indexes
            var insertIds = InsertionIndexes[toolDB.pieceTable];
            if (insertIds.Contains(pos)) {
                insertIds.Remove(pos); // remove insertion index

                // update insertion indexes of pieces that were inserted after
                // the one that was removed
                for (int i = 0; i < table.m_pieces.Count; i++) {
                    if (insertIds[i] > pos) {
                        insertIds[i] -= 1;
                    }
                }
            }
        }
    }
}