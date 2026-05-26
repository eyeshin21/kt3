using System;
using System.Collections.Generic;
using System.Linq;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using UnityEngine;
using Lean.Pool;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class GridBoardController : MonoBehaviour
    {
        [SerializeField] private GridCellController cellPrefab;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private Vector2 cellSpacing = new Vector2(1f, 1f);

        private readonly List<GridCellController> cells = new List<GridCellController>();
        private Action<BoxController> onBoxSelected;
        private Func<bool> canSelectBox;
        private GridCellBoardData boardData;

        private readonly List<PinController>    pins    = new List<PinController>();
        private readonly List<KeyController>    keys    = new List<KeyController>();
        private readonly List<LockController>   lockMap = new List<LockController>();
        private readonly List<TunnelController> tunnels = new List<TunnelController>();

        private readonly Dictionary<ColorType, KeyController>  keysByColor  = new Dictionary<ColorType, KeyController>();
        private readonly Dictionary<ColorType, LockController> locksByColor = new Dictionary<ColorType, LockController>();

        public IReadOnlyList<GridCellController> Cells => cells;

        private IEnumerable<BoxController> CellBoxes => cells
            .Where(cell => cell.BoxController != null)
            .Select(cell => cell.BoxController);

        private readonly List<BoxController> releasedBoxes = new List<BoxController>();

        public IReadOnlyList<BoxController> Boxes =>
            CellBoxes.Concat(releasedBoxes.Where(b => b != null)).ToList();

        public void RegisterReleasedBox(BoxController box)
        {
            if (box != null && !releasedBoxes.Contains(box))
                releasedBoxes.Add(box);
        }

        public void Build(GridCellBoardData gridCellBoardData, Func<bool> canSelect, Action<BoxController> onSelected)
        {
            if (gridCellBoardData == null || cellPrefab == null)
            {
                Debug.LogError($"{nameof(GridBoardController)} on '{name}' requires board data and a cell prefab.", this);
                return;
            }

            boardData = gridCellBoardData;
            canSelectBox = canSelect;
            onBoxSelected = onSelected;
            ClearChildren();

            for (int row = 0; row < boardData.height; row++)
            {
                for (int column = 0; column < boardData.width; column++)
                {
                    var position = new GridPosition(row, column);
                    var cell = LeanPool.Spawn(cellPrefab, GetRoot());
                    cell.transform.localPosition = ToLocalPosition(position, boardData.width);
                    cell.Initialize(position, boardData.GetCellAt(position), boardData, TrySelectBox);
                    cells.Add(cell);
                }
            }

            RefreshPickableBoxes();
        }

        public void BuildM3Mechanics(LevelData level)
        {
            if (level == null) return;

            if (level.pins != null)
            {
                foreach (var def in level.pins)
                {
                    if (def == null) continue;
                    var headCell = cells.Find(c => c.GridPosition == def.headPosition);
                    if (headCell == null)
                    {
                        Debug.LogWarning($"[GridBoardController] Pin '{def.pinId}' head position {def.headPosition} has no matching cell.");
                        continue;
                    }
                    var pin = headCell.SpawnPin(def);
                    RegisterPin(pin);
                }
            }

            if (level.keys != null)
            {
                foreach (var def in level.keys)
                {
                    if (def == null) continue;
                    var cell = cells.Find(c => c.GridPosition == def.position);
                    if (cell == null)
                    {
                        Debug.LogWarning($"[GridBoardController] Key '{def.keyId}' position {def.position} has no matching cell.");
                        continue;
                    }
                    var key = cell.SpawnKey(def);
                    RegisterKey(key);
                }
            }

            if (level.locks != null)
            {
                foreach (var def in level.locks)
                {
                    if (def == null) continue;
                    var cell = cells.Find(c => c.GridPosition == def.position);
                    if (cell == null)
                    {
                        Debug.LogWarning($"[GridBoardController] Lock '{def.lockId}' position {def.position} has no matching cell.");
                        continue;
                    }
                    var lk = cell.SpawnLock(def);
                    RegisterLock(lk);
                }
            }

            if (level.tunnels != null)
            {
                foreach (var def in level.tunnels)
                {
                    if (def == null) continue;
                    var cell = cells.Find(c => c.GridPosition == def.position);
                    if (cell == null)
                    {
                        Debug.LogWarning($"[GridBoardController] Tunnel '{def.tunnelId}' position {def.position} has no matching cell.");
                        continue;
                    }
                    var tunnel = cell.SpawnTunnel(def);
                    RegisterTunnel(tunnel);
                }
            }
        }

        public void ApplyState()
        {
            foreach (var box in Boxes)
            {
                box.ApplyVisualState();
            }
        }

        public void RefreshPickableBoxes()
        {
            releasedBoxes.RemoveAll(b => b == null || b.IsCleared);

            foreach (var box in Boxes)
            {
                box.IsPickable = !box.IsInWaitingArea && !box.IsCleared && HasPathToTopRow(box.GridPosition);
            }
        }

        public BoxController FindBox(string boxId)
        {
            return Boxes.FirstOrDefault(box => box.BoxId == boxId);
        }

        public bool HasActiveBoxAt(GridPosition position)
        {
            return Boxes.Any(box => box.GridPosition == position && !box.IsInWaitingArea && !box.IsCleared);
        }

        public bool AreAllBoxesCleared()
        {
            if (Boxes.Count == 0) return false;
            if (!Boxes.All(box => box.IsCleared)) return false;

            if (keys.Any(k => !k.IsActivated)) return false;
            if (lockMap.Any(l => !l.IsDestroyed)) return false;

            return true;
        }

        public bool AreAllBoxesPicked()
        {
            if (Boxes.Count == 0) return false;
            return Boxes.All(box => box.IsCleared || box.IsInWaitingArea);
        }

        public IReadOnlyList<Vector3> FindPathToTopExit(BoxController box)
        {
            if (box == null || boardData == null)
            {
                return Array.Empty<Vector3>();
            }

            var start = box.GridPosition;
            var queue = new Queue<GridPosition>();
            var visited = new HashSet<GridPosition>();
            var parent = new Dictionary<GridPosition, GridPosition>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Row == 0 && IsPassable(current, start))
                {
                    return BuildWorldPath(parent, start, current);
                }

                foreach (var next in GetNeighbors(current))
                {
                    if (visited.Contains(next) || !IsInside(next) || !IsPassable(next, start))
                    {
                        continue;
                    }

                    visited.Add(next);
                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            return new[] { GetWorldPosition(start) };
        }

        public Vector3 GetWorldPosition(GridPosition position)
        {
            return GetRoot().TransformPoint(ToLocalPosition(position, boardData == null ? 1 : boardData.width));
        }

        public void TrySelectBox(string boxId)
        {
            var box = FindBox(boxId);
            if (box == null || !box.IsPickable || box.IsInWaitingArea || box.IsCleared)
            {
                Debug.LogWarning("Box is not selectable.");
                return;
            }

            if (canSelectBox != null && !canSelectBox.Invoke())
            {
                Debug.LogWarning("Waiting area is full or gameplay input is disabled.");
                return;
            }

            box.IsInWaitingArea = true;
            box.IsPickable = false;
            onBoxSelected?.Invoke(box);
        }

        private IReadOnlyList<Vector3> BuildWorldPath(Dictionary<GridPosition, GridPosition> parent, GridPosition start, GridPosition exit)
        {
            var positions = new List<GridPosition>();
            var current = exit;
            while (current != start)
            {
                positions.Add(current);
                current = parent[current];
            }

            positions.Reverse();
            positions.Add(new GridPosition(-1, exit.Column));
            return positions.Select(GetWorldPosition).ToList();
        }

        private bool HasPathToTopRow(GridPosition start)
        {
            if (boardData == null)
            {
                return false;
            }

            var queue = new Queue<GridPosition>();
            var visited = new HashSet<GridPosition>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Row == 0 && IsPassable(current, start))
                {
                    return true;
                }

                foreach (var next in GetNeighbors(current))
                {
                    if (visited.Contains(next) || !IsInside(next) || !IsPassable(next, start))
                    {
                        continue;
                    }

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private bool IsPassable(GridPosition position, GridPosition start)
        {
            if (position == start)
            {
                return true;
            }

            var cell = boardData.GetCellAt(position);
            if (cell == null || cell.cellType == GridCellType.Empty)
            {
                return true;
            }

            if (cell.cellType == GridCellType.StandardBox ||
                cell.cellType == GridCellType.MysteryBox  ||
                cell.cellType == GridCellType.FrozenBox)
            {
                return !HasActiveBoxAt(position);
            }

            if (cell.cellType == GridCellType.KeyCell)
            {
                var key = keys.Find(k => k.Position == position);
                return key == null || key.IsActivated;
            }
            
            if (cell.cellType == GridCellType.LockCell)
            {
                var lk = lockMap.Find(l => l.Position == position);
                return lk == null || lk.IsDestroyed;
            }

            if (cell.cellType == GridCellType.PinCell || cell.cellType == GridCellType.PinTailCell)
            {
                var pin = pins.Find(p => !p.IsRemoved && p.OccupiedPositions.Contains(position));
                return pin == null;
            }

            if (cell.cellType == GridCellType.TunnelCell)
            {
                var tunnel = tunnels.Find(t => !t.IsRemoved && t.Position == position);
                return tunnel == null;
            }

            return false;
        }

        private bool IsInside(GridPosition position)
        {
            return position.Row >= 0 && position.Row < boardData.height && position.Column >= 0 && position.Column < boardData.width;
        }

        private static IEnumerable<GridPosition> GetNeighbors(GridPosition position)
        {
            yield return new GridPosition(position.Row - 1, position.Column);
            yield return new GridPosition(position.Row, position.Column - 1);
            yield return new GridPosition(position.Row, position.Column + 1);
            yield return new GridPosition(position.Row + 1, position.Column);
        }

        public static bool IsOrthogonalAdjacent(GridPosition a, GridPosition b)
        {
            return (a.Row == b.Row && Mathf.Abs(a.Column - b.Column) == 1) ||
                   (a.Column == b.Column && Mathf.Abs(a.Row - b.Row) == 1);
        }

        private Vector3 ToLocalPosition(GridPosition position, int width)
        {
            var centeredColumn = position.Column - (width - 1) * 0.5f;
            return new Vector3(centeredColumn * cellSpacing.x, 0f, -position.Row * cellSpacing.y);
        }

        private Transform GetRoot()
        {
            return cellRoot == null ? transform : cellRoot;
        }

        private void ClearChildren()
        {
            foreach (var cell in cells)
            {
                cell.Clear();
            }
            cells.Clear();
            pins.Clear();
            keys.Clear();
            lockMap.Clear();
            tunnels.Clear();
            
            foreach (var box in releasedBoxes)
            {
                if (box != null) LeanPool.Despawn(box.gameObject);
            }
            releasedBoxes.Clear();
            
            keysByColor.Clear();
            locksByColor.Clear();
            var root = GetRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
                LeanPool.Despawn(root.GetChild(i).gameObject);
        }

        public void RegisterPin(PinController pin)
        {
            if (pin != null) pins.Add(pin);
        }

        public void RegisterKey(KeyController key)
        {
            if (key == null) return;
            keys.Add(key);
            keysByColor[key.Color] = key;
        }

        public void RegisterLock(LockController lk)
        {
            if (lk == null) return;
            lockMap.Add(lk);
            locksByColor[lk.Color] = lk;
        }

        public void RegisterTunnel(TunnelController tunnel)
        {
            if (tunnel != null) tunnels.Add(tunnel);
        }

        public bool IsCellEffectivelyEmpty(GridPosition position)
        {
            if (boardData == null) return true;
            if (!IsInside(position)) return true;

            if (HasActiveBoxAt(position)) return false;

            var cell = boardData.GetCellAt(position);
            if (cell == null) return true;

            switch (cell.cellType)
            {
                case GridCellType.Empty:
                case GridCellType.DeadCell:
                    return false;

                case GridCellType.StandardBox:
                case GridCellType.MysteryBox:
                case GridCellType.FrozenBox:
                    return !HasActiveBoxAt(position);

                case GridCellType.KeyCell:
                    var key = keys.Find(k => k.Position == position);
                    return key == null || key.IsActivated;

                case GridCellType.LockCell:
                    var lk = lockMap.Find(l => l.Position == position);
                    return lk == null || lk.IsDestroyed;

                case GridCellType.TunnelCell:
                    var tunnel = tunnels.Find(t => !t.IsRemoved && t.Position == position);
                    return tunnel == null;

                case GridCellType.PinCell:
                case GridCellType.PinTailCell:
                    var pin = pins.Find(p => !p.IsRemoved && p.OccupiedPositions.Contains(position));
                    return pin == null || pin.IsRemoved;

                default:
                    return true;
            }
        }

        public List<PinController> ResolvePinTriggers(GridPosition sentBoxPosition)
        {
            var resolved = new List<PinController>();
            foreach (var pin in pins)
            {
                if (pin.IsRemoved) continue;
                if (pin.TriggerNeighborPosition != sentBoxPosition) continue;

                pin.Remove();
                resolved.Add(pin);
            }
            return resolved;
        }

        public List<BoxController> TickFrozenBoxes()
        {
            var thawed = new List<BoxController>();
            foreach (var box in Boxes)
            {
                if (box.IsCleared || box.IsInWaitingArea) continue;
                if (box.FrozenDurability <= 0) continue;
                if (box.IsFrozenLocked) continue;
                if (box.ReduceFrozen()) // returns true when just reached 0
                    thawed.Add(box);
            }
            return thawed;
        }

        public void UnlockAdjacentFrozenBoxes(GridPosition vacatedPosition)
        {
            foreach (var box in Boxes)
            {
                if (box.IsCleared || box.IsInWaitingArea) continue;
                if (box.FrozenDurability <= 0 || !box.IsFrozenLocked) continue;
                
                if (IsOrthogonalAdjacent(box.GridPosition, vacatedPosition))
                {
                    box.IsFrozenLocked = false;
                }
            }
        }

        public void EvaluateFrozenLockStates()
        {
            foreach (var box in Boxes)
            {
                if (box.IsCleared || box.IsInWaitingArea || box.FrozenDurability <= 0) continue;
                
                bool adjacentEmpty = false;
                foreach (var neighbor in GetNeighbors(box.GridPosition))
                {
                    if (IsCellEffectivelyEmpty(neighbor))
                    {
                        adjacentEmpty = true;
                        break;
                    }
                }

                if (adjacentEmpty)
                {
                    box.IsFrozenLocked = false;
                }
            }
        }

        public List<BoxController> RevealAdjacentHiddenBoxes(GridPosition vacatedPosition)
        {
            var revealed = new List<BoxController>();
            foreach (var box in Boxes)
            {
                if (!box.IsHidden || box.IsCleared || box.IsInWaitingArea) continue;
                if (IsOrthogonalAdjacent(box.GridPosition, vacatedPosition))
                {
                    if (box.TryReveal())
                        revealed.Add(box);
                }
            }
            return revealed;
        }

        public List<KeyController> ResolveKeyActivations()
        {
            var ready = new List<KeyController>();
            foreach (var key in keys)
            {
                if (key.IsActivated) continue;
                if (key.CanActivate(this))
                    ready.Add(key);
            }
            return ready;
        }

        public void ResolveTunnelReleases(List<BoxController> releasedBoxes, List<TunnelController> activatedTunnels, List<GridPosition> removedTunnels)
        {
            foreach (var tunnel in tunnels)
            {
                if (!tunnel.HasContent) continue;

                var targetPos  = tunnel.ReleaseTargetPosition;
                var targetCell = cells.Find(c => c.GridPosition == targetPos);
                Transform cellParent = targetCell != null ? targetCell.transform : null;

                var box = tunnel.TryRelease(this, cellParent, TrySelectBox);
                if (box != null)
                {
                    RegisterReleasedBox(box);
                    releasedBoxes.Add(box);
                    activatedTunnels.Add(tunnel);
                    if (!tunnel.HasContent)
                        removedTunnels.Add(tunnel.Position);
                }
            }
        }

        public bool TryGetLock(ColorType color, out LockController result)
            => locksByColor.TryGetValue(color, out result);
    }
}
