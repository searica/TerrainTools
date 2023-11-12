using UnityEngine;

namespace TerrainTools.Visualization
{
    // Minimal classes describing specific VFXs of specific "ToolOps" in a DSL-like form.
    public class LevelGroundOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeTerraformingBounds(secondary);
        }
    }

    public class RaiseGroundOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            base.Initialize();
            Freeze(secondary);
            Freeze(tertiary);
            VisualizeTerraformingBounds(secondary);
            VisualizeTerraformingBounds(tertiary);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            if (Keybindings.GridModeEnabled)
            {
                GroundLevelSpinner.Refresh();
                secondary.LocalPosition = new Vector3(0f, GroundLevelSpinner.Value, 0f);

                if (GroundLevelSpinner.Value > 0f)
                {
                    hoverInfo.Text = $"h: +{secondary.LocalPosition.y:0.00}";
                }
                else
                {
                    hoverInfo.Text = $"x: {secondary.Position.x:0}, y: {secondary.Position.z:0}, h: {secondary.Position.y:0.00000}";
                }
            }
            tertiary.Enabled = Keybindings.GridModeEnabled;
        }
    }

    public class PaveRoadOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeRecoloringBounds(secondary);
        }
    }

    public class CultivateOverlayVisualizer : PaveRoadOverlayVisualizer
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            hoverInfo.Color = secondary.Color;
        }
    }

    public class SeedGrassOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            base.Initialize();
            Freeze(secondary);
            VisualizeRecoloringBounds(secondary);
            primary.LocalPosition = new Vector3(0, 2.0f, 0);
        }

        protected override void OnRefresh()
        {
            if (Keybindings.GridModeEnabled)
            {
                primary.StartSize = 4.0f;
                primary.LocalPosition = new Vector3(0.5f, 2.5f, 0.5f);
            }
            else
            {
                primary.StartSize = 5.5f;
                primary.LocalPosition = new Vector3(0, 2.5f, 0);
            }
            base.OnRefresh();
        }

        protected override void OnEnableGrid()
        {
            base.OnEnableGrid();
            primary.Enabled = false;
            primary.StartSize = 4.0f;
            primary.LocalPosition = new Vector3(0.5f, 2.5f, 0.5f);
            primary.Enabled = true;
        }

        protected override void OnDisableGrid()
        {
            primary.Enabled = false;
            primary.LocalPosition = new Vector3(0, 2.5f, 0);
            primary.StartSize = 5.5f;
            primary.Enabled = true;
            base.OnDisableGrid();
        }
    }

    public class RemoveModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            Freeze(primary);
            SpeedUp(secondary);
            VisualizeTerraformingBounds(primary);
            VisualizeIconInsideTerraformingBounds(secondary, IconCache.Cross);
        }
    }

    public abstract class UndoRedoModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void Initialize()
        {
            Freeze(primary);
            Freeze(secondary);
            VisualizeRecoloringBounds(primary);
            VisualizeIconInsideRecoloringBounds(secondary, Icon());
        }

        protected abstract Texture2D Icon();
    }

    public class UndoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D Icon() => IconCache.Undo;
    }

    public class RedoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D Icon() => IconCache.Redo;
    }
}