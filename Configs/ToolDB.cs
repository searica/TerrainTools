using System;
using UnityEngine;

namespace TerrainTools.Configs
{
    internal class ToolDB
    {
        public string name;
        public string basePrefab;
        public string pieceName;
        public string pieceDesc;
        public Texture2D icon;
        public string pieceTable;
        public int insertIndex;
        public Type overlayType;
        public bool? level;
        public bool? raise;
        public bool? smooth;
        public bool? clearPaint;

        public ToolDB(
            string name,
            string basePrefab,
            string pieceName,
            string pieceDesc,
            Texture2D icon,
            string pieceTable,
            int insertIndex = -1,
            Type overlayType = null,
            bool? level = null,
            bool? raise = null,
            bool? smooth = null,
            bool? clearPaint = null
        )
        {
            this.name = name;
            this.basePrefab = basePrefab;
            this.pieceName = pieceName;
            this.pieceDesc = pieceDesc;
            this.icon = icon;
            this.pieceTable = pieceTable;
            this.insertIndex = insertIndex;
            if (overlayType != null)
            {
                this.overlayType = overlayType;
            }
            this.level = level;
            this.raise = raise;
            this.smooth = smooth;
            this.clearPaint = clearPaint;
        }
    }
}