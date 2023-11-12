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
                    _cultivateSquare = TerrainTools.LoadTextureFromDisk("cultivate_v2_square.png");
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
                    _cultivatePathSquare = TerrainTools.LoadTextureFromDisk("cultivate_v2_path_square.png");
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
                    _cultivatePath = TerrainTools.LoadTextureFromDisk("cultivate_v2_path.png");
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
                    _replantSquare = TerrainTools.LoadTextureFromDisk("replant_v2_square.png");
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
                    _raiseSquare = TerrainTools.LoadTextureFromDisk("raise_v2_square.png");
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
                    _mudRoadPathSquare = TerrainTools.LoadTextureFromDisk("path_v2_square.png");
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
                    _mudRoadSquare = TerrainTools.LoadTextureFromDisk("mud_road_v2_square.png");
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
                    _pavedRoadPath = TerrainTools.LoadTextureFromDisk("paved_road_v2_path.png");
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
                    _pavedRoadPathSquare = TerrainTools.LoadTextureFromDisk("paved_road_v2_path_square.png");
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
                    _pavedRoadSquare = TerrainTools.LoadTextureFromDisk("paved_road_v2_square.png");
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
                    _remove = TerrainTools.LoadTextureFromDisk("remove.png");
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
                    _cross = TerrainTools.LoadTextureFromDisk("cross.png");
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
                    _undo = TerrainTools.LoadTextureFromDisk("undo.png");
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
                    _redo = TerrainTools.LoadTextureFromDisk("redo.png");
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
                    _box = TerrainTools.LoadTextureFromDisk("box.png");
                }
                return _box;
            }
        }
    }
}