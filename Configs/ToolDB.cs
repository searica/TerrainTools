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
        public float? levelRadius;
        public bool? raise;
        public float? raiseRadius;
        public float? raisePower;
        public float? raiseDelta;
        public bool? smooth;
        public float? smoothRadius;
        public float? smoothPower;
        public bool? clearPaint;
        public float? paintRadius;
        public Piece.Requirement[] requirements;
        public bool invertGhost;

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
            float? levelRadius = null,
            bool? raise = null,
            float? raiseRadius = null,
            float? raisePower = null,
            float? raiseDelta = null,
            bool? smooth = null,
            float? smoothRadius = null,
            float? smoothPower = null,
            bool? clearPaint = null,
            float? paintRadius = null,
            Piece.Requirement[] requirements = null,
            bool invertGhost = false
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
            this.levelRadius = levelRadius;

            this.raise = raise;
            this.raiseRadius = raiseRadius;
            this.raisePower = raisePower;
            this.raiseDelta = raiseDelta;

            this.smooth = smooth;
            this.smoothRadius = smoothRadius;
            this.smoothPower = smoothPower;

            this.clearPaint = clearPaint;
            this.paintRadius = paintRadius;

            this.requirements = requirements;
            this.invertGhost = invertGhost;
        }
    }
}