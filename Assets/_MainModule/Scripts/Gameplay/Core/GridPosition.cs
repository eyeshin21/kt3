using System;
using UnityEngine;

namespace HexaFall.Gameplay.Core
{
    /// <summary>
    /// Serializable integer coordinate used by authored level data and runtime puzzle state.
    /// </summary>
    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>, IComparable<GridPosition>
    {
        [SerializeField] private int row;
        [SerializeField] private int column;

        /// <summary>
        /// Zero-based row coordinate.
        /// </summary>
        public int Row => row;

        /// <summary>
        /// Zero-based column coordinate.
        /// </summary>
        public int Column => column;

        /// <summary>
        /// Creates a grid position with zero-based row and column values.
        /// </summary>
        public GridPosition(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        /// <summary>
        /// Compares positions by row, then column.
        /// </summary>
        public int CompareTo(GridPosition other)
        {
            var rowComparison = row.CompareTo(other.row);
            return rowComparison != 0 ? rowComparison : column.CompareTo(other.column);
        }

        /// <summary>
        /// Returns true when both coordinates match.
        /// </summary>
        public bool Equals(GridPosition other)
        {
            return row == other.row && column == other.column;
        }

        /// <summary>
        /// Returns true when the object is an equal grid position.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        /// <summary>
        /// Returns a stable hash code for dictionary and set usage.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (row * 397) ^ column;
            }
        }

        /// <summary>
        /// Formats the position for diagnostics.
        /// </summary>
        public override string ToString()
        {
            return $"({row}, {column})";
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}
