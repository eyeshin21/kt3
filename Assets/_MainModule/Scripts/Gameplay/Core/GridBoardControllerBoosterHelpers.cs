using System.Collections.Generic;
using System.Linq;
using HexaFall.Gameplay.Data;
using HexaFall.Gameplay.Core;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class GridBoardController
    {
        /// <summary>Returns all boxes eligible for the Shuffle booster.</summary>
        /// Standard/Mystery/Frozen, not cleared, not waiting, not pin-blocked.
        public List<BoxController> GetEligibleShuffleBoxes()
        {
            var result = new List<BoxController>();
            foreach (var cell in cells)
            {
                var cellType = boardData.GetCellAt(cell.GridPosition)?.cellType ?? GridCellType.Empty;
                if (cellType != GridCellType.StandardBox) continue;
                    
                var box = cell.BoxController;
                if (box == null || box.IsCleared || box.IsInWaitingArea) continue;
                result.Add(box);
            }
            return result;
        }

        /// <summary>
        /// Physically swaps BoxControllers between cells and updates their grid positions.
        /// Leaves the boxes at their old world positions so they can be animated to Vector3.zero locally.
        /// </summary>
        public void ApplyPhysicalShuffle(List<BoxController> eligible)
        {
            var currentCells = new List<GridCellController>();
            foreach (var box in eligible)
            {
                currentCells.Add(cells.Find(c => c.GridPosition == box.GridPosition));
            }

            foreach (var cell in currentCells)
            {
                if (cell != null) cell.DetachBoxController();
            }

            var shuffledCells = currentCells.ToList();
            for (int i = shuffledCells.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffledCells[i], shuffledCells[j]) = (shuffledCells[j], shuffledCells[i]);
            }

            for (int i = 0; i < eligible.Count; i++)
            {
                var box = eligible[i];
                var newCell = shuffledCells[i];
                
                var oldWorldPos = box.transform.position;
                
                newCell.SetBoxController(box);
                box.SetGridPosition(newCell.GridPosition);
                
                box.transform.position = oldWorldPos;
            }
        }

        /// <summary>
        /// Force-marks a box as waiting without the frontier check.
        /// Used only by MagicWandBooster.
        /// </summary>
        public void ForceMarkBoxAsWaiting(string boxId)
        {
            var box = FindBox(boxId);
            if (box != null) box.IsInWaitingArea = true;
        }

        public bool IsStandardBox(GridPosition position)
        {
            var cellType = boardData?.GetCellAt(position)?.cellType ?? GridCellType.Empty;
            return cellType == GridCellType.StandardBox;
        }
    }
}
