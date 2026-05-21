using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    /// <summary>
    /// ScriptableObject describing the authored stack-board layout for a level.
    /// </summary>
    [Serializable]
    public class StackBoardData 
    {
        [SerializeField] private int columns;
        [SerializeField] private int rows;
        [SerializeField] private List<StackDefinition> stacks = new List<StackDefinition>();

        public StackBoardData() { }

        public StackBoardData(int columns, int rows, List<StackDefinition> stacks)
        {
            this.columns = columns;
            this.rows = rows;
            this.stacks = stacks ?? new List<StackDefinition>();
        }

        /// <summary>
        /// Number of columns in the stack board.
        /// </summary>
        public int Columns => columns;

        /// <summary>
        /// Number of rows in the stack board.
        /// </summary>
        public int Rows => rows;

        /// <summary>
        /// Authored stack definitions.
        /// </summary>
        public IReadOnlyList<StackDefinition> Stacks => stacks;
    }
}
