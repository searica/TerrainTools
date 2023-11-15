using UnityEngine;

namespace TerrainTools.Visualization
{
    internal class IconCache
    {
        private static Texture2D _remove;
        private static Texture2D _cross;
        private static Texture2D _undo;
        private static Texture2D _redo;
        private static Texture2D _box;
        private static Texture2D _pavedRoadSquare;
        private static Texture2D _pavedRoadPath;
        private static Texture2D _pavedRoadPathSquare;
        private static Texture2D _mudRoadSquare;
        private static Texture2D _mudRoadPathSquare;
        private static Texture2D _replantSquare;
        private static Texture2D _cultivateSquare;
        private static Texture2D _cultivatePath;
        private static Texture2D _cultivatePathSquare;
        private static Texture2D _raiseSquare;

        internal static Texture2D CultivateSquare
        {
            get
            {
                if (_cultivateSquare == null)
                {
                    _cultivateSquare = TerrainTools.LoadTextureFromResources("cultivate_v2_square.png");
                }
                return _cultivateSquare;
            }
        }

        internal static Texture2D CultivatePathSquare
        {
            get
            {
                if (_cultivatePathSquare == null)
                {
                    _cultivatePathSquare = TerrainTools.LoadTextureFromResources("cultivate_v2_path_square.png");
                }
                return _cultivatePathSquare;
            }
        }

        internal static Texture2D CultivatePath
        {
            get
            {
                if (_cultivatePath == null)
                {
                    _cultivatePath = TerrainTools.LoadTextureFromResources("cultivate_v2_path.png");
                }
                return _cultivatePath;
            }
        }

        internal static Texture2D ReplantSquare
        {
            get
            {
                if (_replantSquare == null)
                {
                    _replantSquare = TerrainTools.LoadTextureFromResources("replant_v2_square.png");
                }
                return _replantSquare;
            }
        }

        internal static Texture2D RaiseSquare
        {
            get
            {
                if (_raiseSquare == null)
                {
                    _raiseSquare = TerrainTools.LoadTextureFromResources("raise_v2_square.png");
                }
                return _raiseSquare;
            }
        }

        internal static Texture2D MudRoadPathSquare
        {
            get
            {
                if (_mudRoadPathSquare == null)
                {
                    _mudRoadPathSquare = TerrainTools.LoadTextureFromResources("path_v2_square.png");
                }
                return _mudRoadPathSquare;
            }
        }

        internal static Texture2D MudRoadSquare
        {
            get
            {
                if (_mudRoadSquare == null)
                {
                    _mudRoadSquare = TerrainTools.LoadTextureFromResources("mud_road_v2_square.png");
                }
                return _mudRoadSquare;
            }
        }

        internal static Texture2D PavedRoadPath
        {
            get
            {
                if (_pavedRoadPath == null)
                {
                    _pavedRoadPath = TerrainTools.LoadTextureFromResources("paved_road_v2_path.png");
                }
                return _pavedRoadPath;
            }
        }

        internal static Texture2D PavedRoadPathSquare
        {
            get
            {
                if (_pavedRoadPathSquare == null)
                {
                    _pavedRoadPathSquare = TerrainTools.LoadTextureFromResources("paved_road_v2_path_square.png");
                }
                return _pavedRoadPathSquare;
            }
        }

        internal static Texture2D PavedRoadSquare
        {
            get
            {
                if (_pavedRoadSquare == null)
                {
                    _pavedRoadSquare = TerrainTools.LoadTextureFromResources("paved_road_v2_square.png");
                }
                return _pavedRoadSquare;
            }
        }

        internal static Texture2D Remove
        {
            get
            {
                if (_remove == null)
                {
                    _remove = TerrainTools.LoadTextureFromResources("remove.png");
                }
                return _remove;
            }
        }

        internal static Texture2D Cross
        {
            get
            {
                if (_cross == null)
                {
                    _cross = TerrainTools.LoadTextureFromResources("cross.png");
                }
                return _cross;
            }
        }

        internal static Texture2D Undo
        {
            get
            {
                if (_undo == null)
                {
                    _undo = TerrainTools.LoadTextureFromResources("undo.png");
                }
                return _undo;
            }
        }

        internal static Texture2D Redo
        {
            get
            {
                if (_redo == null)
                {
                    _redo = TerrainTools.LoadTextureFromResources("redo.png");
                }
                return _redo;
            }
        }

        internal static Texture2D Box
        {
            get
            {
                if (_box == null)
                {
                    _box = TerrainTools.LoadTextureFromResources("box.png");
                }
                return _box;
            }
        }
    }
}