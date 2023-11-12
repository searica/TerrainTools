using UnityEngine;

namespace TerrainTools.Visualization
{
    // Minimal classes describing specific VFXs of specific "ToolOps" in a DSL-like form.
    public class LevelGroundOverlayVisualizer : HoverInfoEnabled
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeTerraformingBounds(secondary);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = false;
            secondary.Enabled = true;
        }
    }

    public class RaiseGroundOverlayVisualizer : HoverInfoEnabled
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
            primary.Enabled = true;
            secondary.Enabled = true;

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

            tertiary.Enabled = true;
        }
    }

    public class PaveRoadOverlayVisualizer : HoverInfoEnabled
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeRecoloringBounds(secondary);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = false;
            secondary.Enabled = true;
        }
    }

    public class CultivateOverlayVisualizer : HoverInfoEnabled
    {
        protected override void Initialize()
        {
            base.Initialize();
            SpeedUp(secondary);
            VisualizeRecoloringBounds(secondary);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = false;
            secondary.Enabled = true;
            hoverInfo.Color = secondary.Color;
        }
    }

    public class SeedGrassOverlayVisualizer : HoverInfoEnabled
    {
        protected override void Initialize()
        {
            base.Initialize();
            Freeze(secondary);
            VisualizeRecoloringBounds(secondary);
            primary.StartSize = 4.0f;
            primary.LocalPosition = new Vector3(0.0f, 2.5f, 0.0f);
            //primary.LocalPosition = new Vector3(0.0f, 2.5f, 0.0f);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.Enabled = true;
            secondary.Enabled = true;
        }
    }

    public class RemoveModificationsOverlayVisualizer : OverlayVisualizer
    {
        protected override void Initialize()
        {
            Freeze(primary);
            SpeedUp(secondary);
            VisualizeTerraformingBounds(primary);
            VisualizeIconInsideTerraformingBounds(secondary, IconCache.Cross);
        }

        protected override void OnRefresh()
        {
            primary.Enabled = true;
            secondary.Enabled = true;
        }
    }

    public abstract class UndoRedoModificationsOverlayVisualizer : OverlayVisualizer
    {
        protected override void Initialize()
        {
            Freeze(primary);
            Freeze(secondary);
            VisualizeRecoloringBounds(primary);
            VisualizeIconInsideRecoloringBounds(secondary, Icon());
        }

        protected override void OnRefresh()
        {
            primary.Enabled = true;
            secondary.Enabled = true;
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