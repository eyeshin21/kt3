using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using UnityEngine;
using Lean.Pool;
using HexaFall.Gameplay.Runtime;

namespace HexaFall.Gameplay.CoreController
{
    public sealed class StackBoardController : MonoBehaviour
    {
        [SerializeField] private StackPlaceholder placeholderPrefab;
        [SerializeField] private StackController stackPrefab;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private Vector2 cellSpacing = new Vector2(1f, 1f);

        private float stackFlowDuration => GameController.Instance.GameplayTuningConfig.stackFlowDuration;

        private readonly List<StackController> views = new List<StackController>();
        private int columns;
        private bool isFlowing = false;

        public IReadOnlyList<StackController> Views => views;
        public bool IsFlowing => isFlowing;

        public void Build(StackBoardData boardData)
        {
            if (boardData == null || stackPrefab == null)
            {
                Debug.LogError($"{nameof(StackBoardController)} on '{name}' requires board data and a stack prefab.", this);
                return;
            }

            columns = boardData.Columns;
            ClearChildren();
            SpawnPlaceholders(boardData);

            for (int i = 0; i < boardData.Stacks.Count; i++)
            {
                var stack = boardData.Stacks[i];
                var position = stack.useExplicitPosition ? stack.position : IndexToStaggeredPosition(i, boardData.Columns);
                var view = LeanPool.Spawn(stackPrefab, GetRoot());
                view.transform.localPosition = ToLocalPosition(position, boardData.Columns);
                view.Initialize((GridPosition)position, stack.blocksBottomToTop, stack.isHidden);
                views.Add(view);
            }
        }

        public void ApplyState()
        {
            foreach (var view in views)
            {
                view.ApplyVisualState();
            }
        }

        public StackController FindMatchingEligibleStack(ColorType targetColor)
        {
            var occupiedStacks = views.Where(stack => stack.HasBlocks && !stack.IsMoving).ToList();
            if (occupiedStacks.Count == 0)
            {
                return null;
            }

            var lowestRow = occupiedStacks.Min(stack => stack.Position.Row);
            return occupiedStacks
                .Where(stack => stack.Position.Row == lowestRow && stack.HasColor(targetColor))
                .OrderBy(stack => stack.Position.Column)
                .FirstOrDefault();
        }

        internal List<StackController> FindAllMatchingEligibleStacks(ColorType targetColor, bool lowestRowOnly = true)
        {
            var occupiedStacks = views.Where(stack => stack.HasBlocks && !stack.IsMoving).ToList();
            if (occupiedStacks.Count == 0)
            {
                return new List<StackController>();
            }

            if (lowestRowOnly)
            {
                var lowestRow = occupiedStacks.Min(stack => stack.Position.Row);
                return occupiedStacks
                    .Where(stack => stack.Position.Row == lowestRow && stack.HasColor(targetColor))
                    .OrderBy(stack => stack.Position.Column)
                    .ToList();
            }
            else
            {
                return occupiedStacks
                    .Where(stack => stack.HasColor(targetColor))
                    .OrderBy(stack => stack.Position.Row).ThenBy(stack => stack.Position.Column)
                    .ToList();
            }
        }

        public IEnumerator FlowStacksToLowestPlaceholders(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);

            if (isFlowing) yield break;
            isFlowing = true;

            var grid = new Dictionary<GridPosition, StackController>();
            int topRow = 0;
            foreach (var stack in views.Where(stack => stack.HasBlocks))
            {
                grid[stack.Position] = stack;
                if (stack.Position.Row > topRow)
                {
                    topRow = stack.Position.Row;
                }
            }

            var movingStacks = new HashSet<StackController>();
            bool moved;
            do
            {
                moved = false;
                for (int r = 0; r <= topRow; r++)
                {
                    int colsInRow = GetColumnsInRow(r, columns);
                    for (int c = 0; c < colsInRow; c++)
                    {
                        var pos = new GridPosition(r, c);
                        if (!grid.ContainsKey(pos))
                        {
                            int leftCol = (r % 2 == 0) ? c - 1 : c;
                            int rightCol = (r % 2 == 0) ? c : c + 1;
                            
                            var posLeft = new GridPosition(r + 1, leftCol);
                            var posRight = new GridPosition(r + 1, rightCol);

                            StackController pulledStack = null;
                            if (r % 2 == 0)
                            {
                                if (grid.TryGetValue(posLeft, out var stackLeft))
                                {
                                    pulledStack = stackLeft;
                                    grid.Remove(posLeft);
                                }
                                else if (grid.TryGetValue(posRight, out var stackRight))
                                {
                                    pulledStack = stackRight;
                                    grid.Remove(posRight);
                                }
                            }
                            else
                            {
                                if (grid.TryGetValue(posRight, out var stackRight))
                                {
                                    pulledStack = stackRight;
                                    grid.Remove(posRight);
                                }
                                else if (grid.TryGetValue(posLeft, out var stackLeft))
                                {
                                    pulledStack = stackLeft;
                                    grid.Remove(posLeft);
                                }
                            }

                            if (pulledStack != null)
                            {
                                grid[pos] = pulledStack;
                                    
                                pulledStack.SetPosition(pos);
                                pulledStack.IsMoving = true;
                                movingStacks.Add(pulledStack);
                                moved = true;
                            }
                        }
                    }
                }
            } while (moved);

            var slideRoutines = new List<Coroutine>();
            foreach (var stack in movingStacks)
            {
                slideRoutines.Add(StartCoroutine(stack.PlaySlideToLocal(ToLocalPosition(stack.Position, columns), stackFlowDuration)));
            }

            foreach (var routine in slideRoutines)
            {
                yield return routine;
            }

            foreach (var stack in movingStacks)
            {
                stack.IsMoving = false;
            }

            isFlowing = false;
        }

        private void SpawnPlaceholders(StackBoardData boardData)
        {
            if (placeholderPrefab == null)
            {
                return;
            }

            for (int row = 0; row < boardData.Rows; row++)
            {
                int colsInRow = GetColumnsInRow(row, boardData.Columns);
                for (int column = 0; column < colsInRow; column++)
                {
                    var position = new GridPosition(row, column);
                    var placeholder = LeanPool.Spawn(placeholderPrefab, GetRoot());
                    placeholder.Initialize(position);
                    placeholder.transform.localPosition = ToLocalPosition(position, boardData.Columns);
                }
            }
        }

        private Vector3 ToLocalPosition(GridPosition position, int baseWidth)
        {
            int rowWidth = GetColumnsInRow(position.Row, baseWidth);
            var centeredColumn = position.Column - (rowWidth - 1) * 0.5f;
            return new Vector3(centeredColumn * cellSpacing.x, 0f, position.Row * cellSpacing.y);
        }

        private int GetColumnsInRow(int row, int baseColumns)
        {
            return (row % 2 == 1) ? baseColumns - 1 : baseColumns;
        }

        private GridPosition IndexToStaggeredPosition(int index, int baseColumns)
        {
            int cycleSize = baseColumns + baseColumns - 1;
            int cycles = index / cycleSize;
            int remainder = index % cycleSize;

            if (remainder < baseColumns)
            {
                return new GridPosition(cycles * 2, remainder);
            }
            else
            {
                return new GridPosition(cycles * 2 + 1, remainder - baseColumns);
            }
        }

        private Transform GetRoot()
        {
            return stackRoot == null ? transform : stackRoot;
        }

        public List<StackController> RevealHiddenStacksInCurrentRow()
        {
            var revealed = new List<StackController>();

            var occupied = views.Where(s => s.HasBlocks).ToList();
            if (occupied.Count == 0) return revealed;

            int lowestRow = occupied.Min(s => s.Position.Row);

            foreach (var stack in occupied)
            {
                if (stack.Position.Row == lowestRow && stack.IsHidden)
                {
                    stack.SetHidden(false);
                    revealed.Add(stack);
                }
            }

            return revealed;
        }

        private void ClearChildren()
        {
            foreach (var view in views)
            {
                view.ClearBlocks();
            }
            views.Clear();
            var root = GetRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                LeanPool.Despawn(root.GetChild(i).gameObject);
            }
        }

    }
}
