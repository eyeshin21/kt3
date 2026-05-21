using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    /// <summary>
    /// Visual placeholder for a stack-board cell.
    /// </summary>
    public sealed class StackPlaceholder : MonoBehaviour
    {
        /// <summary>
        /// Stack-board position represented by this placeholder.
        /// </summary>
        public GridPosition Position { get; private set; }

        /// <summary>
        /// Initializes the placeholder position.
        /// </summary>
        public void Initialize(GridPosition position)
        {
            Position = position;
        }
    }
}
