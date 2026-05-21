using HexaFall.Gameplay.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    /// <summary>
    /// ScriptableObject describing the authored box-grid layout for a level.
    /// </summary>
    [Serializable]
    public class GridCellBoardData
    {
        public int width;
        public int height;
        public List<GridCellDefinition> gridCells = new List<GridCellDefinition>();

        public GridCellDefinition GetCellAt(GridPosition position)
        {
            return gridCells.Find(cell => cell.position.Equals(position));
        }
    }
}
