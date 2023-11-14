using HarmonyLib;
using TerrainTools.Extensions;
using TerrainTools.Visualization;
using UnityEngine;

namespace TerrainTools.Helpers
{
    [HarmonyPatch]
    internal class HardnessModifier
    {
        internal static float lastOriginalPower;
        internal static float lastModdedPower;
        internal static float lastTotalDelta;
        private static float lastDisplayedPower;
        private const float MinPower = 1f;
        private const float MaxPower = 30f;

        [HarmonyPatch(typeof(Player))]
        internal class PlayerPatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.LowerThanNormal)]
            [HarmonyPatch(nameof(Player.Update))]
            private static void UpdatePrefix(Player __instance)
            {
                if (
                    __instance == null
                    || !__instance.InPlaceMode()
                    || Hud.IsPieceSelectionVisible()
                )
                {
                    if (lastOriginalPower != 0)
                    {
                        lastOriginalPower = 0;
                        lastModdedPower = 0;
                        lastTotalDelta = 0;
                        SetPower(__instance, 0);
                    }
                    return;
                }

                if (ShouldModifyHardness())
                {
                    SetPower(__instance, Input.mouseScrollDelta.y * TerrainTools.RadiusScrollScale);
                }
            }
        }

        internal static bool ShouldModifyHardness()
        {
            return TerrainTools.IsEnableHardnessModifier && Input.GetKey(TerrainTools.HardnessKey) && Input.mouseScrollDelta.y != 0;
        }

        [HarmonyPatch(typeof(TerrainOp))]
        internal class TerrainOpPatch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            [HarmonyPatch(nameof(TerrainOp.Awake))]
            private static void AwakePrefix(TerrainOp __instance)
            {
                if (__instance is null || __instance.gameObject is null || __instance.gameObject.HasComponent<OverlayVisualizer>())
                {
                    return;
                }

                if (__instance.m_settings.m_raise)
                {
                    __instance.m_settings.m_raisePower = ModifyPower(__instance.m_settings.m_raisePower, lastTotalDelta);
                    Log.LogInfo($"Applying raise Power {__instance.m_settings.m_raisePower}", LogLevel.Medium);
                }

                if (__instance.m_settings.m_smooth)
                {
                    __instance.m_settings.m_smoothPower = ModifyPower(__instance.m_settings.m_smoothPower, lastTotalDelta);
                    Log.LogInfo($"Applying smooth Power {__instance.m_settings.m_smoothPower}", LogLevel.Medium);
                }
            }
        }

        private static float ModifyPower(float power, float delta)
        {
            return Mathf.Clamp(power + delta, MinPower, MaxPower);
        }

        private static void SetPower(Player player, float delta)
        {
            var piece = player.GetSelectedPiece();
            if (piece is null || piece.gameObject is null || piece.gameObject.HasComponent<OverlayVisualizer>())
            {
                return;
            }

            var terrainOp = piece.gameObject.GetComponent<TerrainOp>();
            if (terrainOp == null) { return; }

            Log.LogInfo($"Adjusting Power by {delta}", LogLevel.Medium);

            float originalPower = 0;
            float moddedPower = ModifyPower(lastModdedPower, delta);
            lastTotalDelta += delta;

            if (lastOriginalPower == 0)
            {
                if (terrainOp.m_settings.m_raise && originalPower < terrainOp.m_settings.m_raisePower)
                {
                    originalPower = terrainOp.m_settings.m_raisePower;
                    moddedPower = ModifyPower(terrainOp.m_settings.m_raisePower, delta);
                }
                if (terrainOp.m_settings.m_smooth && originalPower < terrainOp.m_settings.m_smoothPower)
                {
                    originalPower = terrainOp.m_settings.m_smoothPower;
                    moddedPower = ModifyPower(terrainOp.m_settings.m_smoothPower, delta);
                }

                lastOriginalPower = originalPower;
            }
            lastModdedPower = moddedPower;

            if (lastOriginalPower > 0 && Mathf.Abs(lastDisplayedPower - lastModdedPower) >= 1)
            {
                Log.LogInfo($"total delta {lastTotalDelta}", LogLevel.Medium);

                var toolIcon = player.m_placementGhost?.GetComponent<Piece>()?.m_icon;
                if (toolIcon != null)
                {
                    lastDisplayedPower = Mathf.Round(moddedPower);
                    player.Message(MessageHud.MessageType.Center, $"Terrain tool hardness: {lastDisplayedPower}", icon: toolIcon);
                }
            }
        }
    }
}