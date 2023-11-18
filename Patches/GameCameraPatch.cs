using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using TerrainTools.Helpers;
using TerrainTools.Visualization;

namespace TerrainTools.Patches
{
    [HarmonyPatch(typeof(GameCamera))]
    internal class GameCameraPatch
    {
        /// <summary>
        ///     Transpiler to allow blocking camera zoom based on selected piece
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(GameCamera.UpdateCamera))]
        private static IEnumerable<CodeInstruction> UpdateCameraTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            /* Target this IL code to be able to block camera zoom based on the piece being placed

			if ((!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !InventoryGui.IsVisible() && !StoreGui.IsVisible() && !Menu.IsVisible() && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !localPlayer.InCutscene() && (!localPlayer.InPlaceMode() || localPlayer.InRepairMode() || !localPlayer.CanRotatePiece() || localPlayer.GetPlacementStatus() == Player.PlacementStatus.NoRayHits || ZInput.IsGamepadActive()))

			IL_00b0: ldloc.0
			IL_00b1: callvirt instance bool Player::CanRotatePiece()
			IL_00b6: brfalse.s IL_00c9

             */
            return new CodeMatcher(instructions)
                .MatchForward(
                    useEnd: false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Player), nameof(Player.CanRotatePiece)))
                )
                .SetInstructionAndAdvance(Transpilers.EmitDelegate(UpdateCamera_BlockScroll_Delegate))
                .InstructionEnumeration();
        }

        private static bool UpdateCamera_BlockScroll_Delegate(Player localPlayer)
        {
            var selectedPiece = localPlayer?.GetSelectedPiece();
            if (selectedPiece?.gameObject != null)
            {
                if (selectedPiece.gameObject.GetComponentInChildren<RaiseGroundOverlayVisualizer>() ||
                    RadiusModifier.ShouldModifyRadius() ||
                    HardnessModifier.ShouldModifyHardness())
                {
                    return true;
                }
            }

            return localPlayer.CanRotatePiece();
        }
    }
}