using System;
using System.Collections.Generic;
using UnityEngine;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using TerrainTools.Configs;
using System.Linq;

namespace TerrainTools.Helpers
{
    internal class InitManager
    {
        private static bool HasInitialized = false;
        internal static readonly Dictionary<string, GameObject> ToolRefs = new();

        /// <summary>
        ///     Track the where pieces have been inserted into each piece table. Used
        ///     to manage the insertion position so that the insertion index is
        ///     not dependent on how many other pieces have been added.
        /// </summary>
        private static readonly Dictionary<string, List<int>> InsertionIndexes = new()
        {
            {PieceTables.Hoe, new List<int>()},
            {PieceTables.Cultivator, new List<int>()},
        };

        internal static void InitToolPieces()
        {
            if (HasInitialized) return;
            FixVanillaToolDescriptions();

            foreach (var key in ToolConfigs.ToolConfigsMap.Keys)
            {
                try
                {
                    var toolDB = ToolConfigs.ToolConfigsMap[key];
                    var piecePrefab = MakeToolPiece(toolDB);
                    RegisterPieceInPieceTable(piecePrefab, toolDB.pieceTable, null, toolDB.insertIndex);
                    ToolRefs.Add(key, piecePrefab);
                }
                catch
                {
                    Log.LogWarning($"Failed to create: {key}");
                }
            }

            HasInitialized = true;
        }

        internal static void FixVanillaToolDescriptions()
        {
            SetDescription(
               "mud_road_v2",
               "Levels ground. (Use shift + click to level ground based on where you are pointing)"
            );

            SetDescription(
                "raise_v2",
                "Raise ground based on player position. (Use shift + click to raise ground based on where you are pointing)"
            );

            SetDescription(
                "path_v2",
                "Creates a dirt path without affecting ground height."
            );

            SetDescription(
                "paved_road_v2",
                "Creates a paved path and levels ground based on player position. (Use shift+click to level ground based on where you are pointing)"
            );

            SetDescription(
                "cultivate_v2",
                "Cultivates ground and levels ground based on player position. (Use shift + click to level ground based on where you are pointing)"
            );

            SetDescription(
                "replant_v2",
                "Replants terrain without affecting ground height."
            );
        }

        private static void SetDescription(string prefabName, string description)
        {
            var prefabPiece = PrefabManager.Instance.GetPrefab(prefabName)?.GetComponent<Piece>();
            if (prefabPiece != null)
            {
                prefabPiece.m_description = description;
            }
            else
            {
                Log.LogWarning($"Could not set description for: {prefabName}");
            }
        }

        /// <summary>
        ///     Updates which tools are enabled/disabled to add/remove them from piece tables.
        /// </summary>
        internal static void UpdatePlugin()
        {
            if (!HasInitialized) return;

            ForceUnequipTerrainTools();

            foreach (var key in ToolRefs.Keys)
            {
                var toolPrefabPiece = ToolRefs[key].GetComponent<Piece>();
                toolPrefabPiece.m_enabled = TerrainTools.IsToolEnabled(key);
            }
        }

        /// <summary>
        ///     Forces hammer to be unequipped if it is currently equipped.
        /// </summary>
        private static void ForceUnequipTerrainTools()
        {
            if (Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_cultivator")
            {
                Log.LogWarning("Terrain Tools updated through config change, unequipping cultivator");
                Player.m_localPlayer.HideHandItems();
            }

            if (Player.m_localPlayer?.GetRightItem()?.m_shared.m_name == "$item_hoe")
            {
                Log.LogWarning("Terrain Tools updated through config change, unequipping hoe");
                Player.m_localPlayer.HideHandItems();
            }
        }

        internal static GameObject MakeToolPiece(ToolDB toolDB)
        {
            // prevent duplicates
            if (PieceManager.Instance.GetPiece(toolDB.pieceName) != null) { return null; }

            // clone base prefab and set name
            var toolPrefab = PrefabManager.Instance.CreateClonedPrefab(toolDB.name, toolDB.basePrefab);
            if (toolDB == null) { return null; }

            // customize piece component
            var toolPiece = toolPrefab.GetComponent<Piece>();
            toolPiece.m_icon = Sprite.Create(toolDB.icon, new Rect(0, 0, toolDB.icon.width, toolDB.icon.height), Vector2.zero);
            toolPiece.m_name = toolDB.pieceName;
            toolPiece.m_description = toolDB.pieceDesc;

            // customize terrain op component
            var settings = toolPrefab.GetComponent<TerrainOp>().m_settings;
            settings.m_level = toolDB.level != null ? toolDB.level.Value : settings.m_level;
            settings.m_raise = toolDB.raise != null ? toolDB.raise.Value : settings.m_raise;
            settings.m_smooth = toolDB.smooth != null ? toolDB.smooth.Value : settings.m_smooth;
            settings.m_paintCleared = toolDB.clearPaint != null ? toolDB.clearPaint.Value : settings.m_paintCleared;

            // apply custom visualization overlay if desired
            if (toolDB.overlayType != null)
            {
                toolPrefab.AddComponent(toolDB.overlayType);
            }
            return toolPrefab;
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
        private static void RegisterPieceInPieceTable(GameObject prefab, string pieceTable, string category, int position = -1)
        {
            var piece = prefab.GetComponent<Piece>();
            if (piece == null)
            {
                throw new Exception($"Prefab {prefab.name} has no Piece component attached");
            }

            var table = PieceManager.Instance.GetPieceTable(pieceTable);
            if (table == null)
            {
                throw new Exception($"Could not find PieceTable {pieceTable}");
            }

            if (table.m_pieces.Contains(prefab))
            {
                Log.LogDebug($"Already added piece {prefab.name}");
                return;
            }

            var name = prefab.name;
            var hash = name.GetStableHashCode();

            if (PrefabManager.Instance.GetPrefab(name) == null)
            {
                PrefabManager.Instance.AddPrefab(prefab);
            }

            if (ZNetScene.instance != null && !ZNetScene.instance.m_namedPrefabs.ContainsKey(hash))
            {
                PrefabManager.Instance.RegisterToZNetScene(prefab);
            }

            if (!string.IsNullOrEmpty(category))
            {
                piece.m_category = PieceManager.Instance.AddPieceCategory(pieceTable, category);
            }

            if (position < 0)
            {
                table.m_pieces.Add(prefab);
                InsertionIndexes[pieceTable].Add(table.m_pieces.Count - 1);
            }
            else
            {
                // Shift position to account for how many pieces have been added before it
                var index = position + InsertionIndexes[pieceTable].Where(x => x <= position).Count();

                // Add piece at new insertion point
                table.m_pieces.Insert(index, prefab);
                InsertionIndexes[pieceTable].Add(position);
            }

            Log.LogDebug($"Added piece {prefab.name} | Token: {piece.TokenName()}");
        }
    }
}