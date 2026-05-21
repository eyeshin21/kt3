using System.Collections.Generic;
using HexaFall.Gameplay.Config;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;

namespace HexaFall.Gameplay.Validation
{
    public sealed class LevelValidator : ILevelValidator
    {
        public LevelValidationResult Validate(LevelData level, GameplayTuningConfig tuning)
        {
            var errors = new List<string>();

            if (level == null)
            {
                errors.Add("LevelData is missing.");
                return new LevelValidationResult(errors);
            }

            ValidateLevelRoot(level, tuning, errors);
            ValidateGrid(level.gridCellBoardData, tuning, errors);
            ValidateStackBoard(level.stackBoard, tuning, errors);
            ValidateColorQuantities(level, errors);
            ValidateM3Lists(level, errors);
            return new LevelValidationResult(errors);
        }

        private static void ValidateColorQuantities(LevelData level, List<string> errors)
        {
            if (level.gridCellBoardData == null || level.stackBoard == null)
            {
                return;
            }

            var boxColorCounts = new Dictionary<ColorType, int>();
            foreach (var cell in level.gridCellBoardData.gridCells)
            {
                if (cell != null && cell.cellType != GridCellType.Empty && cell.cellType != GridCellType.DeadCell && cell.box != null)
                {
                    if (cell.box.targetColor != ColorType.None)
                    {
                        if (!boxColorCounts.ContainsKey(cell.box.targetColor))
                            boxColorCounts[cell.box.targetColor] = 0;
                        boxColorCounts[cell.box.targetColor] += cell.box.capacity;
                    }
                }
            }

            if (level.tunnels != null)
            {
                foreach (var tunnel in level.tunnels)
                {
                    if (tunnel.contents != null)
                    {
                        foreach (var box in tunnel.contents)
                        {
                            if (box != null && box.targetColor != ColorType.None)
                            {
                                if (!boxColorCounts.ContainsKey(box.targetColor))
                                    boxColorCounts[box.targetColor] = 0;
                                boxColorCounts[box.targetColor] += box.capacity;
                            }
                        }
                    }
                }
            }

            var stackColorCounts = new Dictionary<ColorType, int>();
            foreach (var stack in level.stackBoard.Stacks)
            {
                if (stack != null && stack.blocksBottomToTop != null)
                {
                    foreach (var color in stack.blocksBottomToTop)
                    {
                        if (color != ColorType.None)
                        {
                            if (!stackColorCounts.ContainsKey(color))
                                stackColorCounts[color] = 0;
                            stackColorCounts[color]++;
                        }
                    }
                }
            }

            var allColors = new HashSet<ColorType>(boxColorCounts.Keys);
            allColors.UnionWith(stackColorCounts.Keys);

            foreach (var color in allColors)
            {
                boxColorCounts.TryGetValue(color, out int required);
                stackColorCounts.TryGetValue(color, out int available);

                if (required != available)
                {
                    errors.Add($"Color mismatch for {color}: Grid requires {required} blocks, but StackBoard provides {available} blocks.");
                }
            }
        }

        private static void ValidateLevelRoot(LevelData level, GameplayTuningConfig tuning, List<string> errors)
        {
            if (level.level <= 0)
            {
                errors.Add("Level must be greater than zero.");
            }

            if (tuning == null)
            {
                errors.Add("GameplayTuningConfig is missing.");
                return;
            }

            if (tuning.MinimumWaitingSlots > tuning.MaximumWaitingSlots)
            {
                errors.Add("GameplayTuningConfig minimum waiting slots cannot exceed maximum waiting slots.");
            }

            if (level.waitingSlots < tuning.MinimumWaitingSlots || level.waitingSlots > tuning.MaximumWaitingSlots)
            {
                errors.Add($"WaitingSlots {level.waitingSlots} must be between {tuning.MinimumWaitingSlots} and {tuning.MaximumWaitingSlots}.");
            }
        }

        private static void ValidateGrid(GridCellBoardData grid, GameplayTuningConfig tuning, List<string> errors)
        {
            if (grid == null)
            {
                errors.Add("GridCellBoardData is missing.");
                return;
            }

            if (grid.width <= 0 || grid.height <= 0)
            {
                errors.Add("GridCellBoardData width and height must be greater than zero.");
            }

            var occupiedPositions = new HashSet<string>();
            var boxIds = new HashSet<string>();

            bool hasStartingSelectableBox = false;

            for (int i = 0; i < grid.gridCells.Count; i++)
            {
                var cell = grid.gridCells[i];
                if (cell == null)
                {
                    errors.Add($"Grid cell at index {i} is missing.");
                    continue;
                }

                ValidateGridCellBounds(grid, cell, i, occupiedPositions, errors);
                ValidateGridCellType(cell, i, errors);
                ValidateBoxCell(cell, i, tuning, boxIds, errors, ref hasStartingSelectableBox);
            }

            //if (!hasStartingSelectableBox)
            //{
            //    errors.Add("At least one standard box must be selectable from explicit starts or the default player-facing row.");
            //}
        }


        private static void ValidateGridCellBounds(GridCellBoardData grid, GridCellDefinition cell, int index, HashSet<string> occupiedPositions, List<string> errors)
        {
            var position = cell.position;
            if (position.Row < 0 || position.Row >= grid.height || position.Column < 0 || position.Column >= grid.width)
            {
                errors.Add($"Grid cell at index {index} has out-of-bounds position ({position.Row}, {position.Column}).");
            }

            var key = GetPositionKey(position);
            if (!occupiedPositions.Add(key))
            {
                errors.Add($"Duplicate grid cell position ({position.Row}, {position.Column}).");
            }
        }

        private static void ValidateGridCellType(GridCellDefinition cell, int index, List<string> errors)
        {
            // All M1 and M3 types are valid; warn on truly unknown values only.
            switch (cell.cellType)
            {
                case GridCellType.Empty:
                case GridCellType.StandardBox:
                case GridCellType.DeadCell:
                case GridCellType.MysteryBox:
                case GridCellType.FrozenBox:
                case GridCellType.KeyCell:
                case GridCellType.LockCell:
                case GridCellType.TunnelCell:
                case GridCellType.PinCell:
                case GridCellType.PinTailCell:
                    break;
                default:
                    errors.Add($"Grid cell at index {index} uses unknown cell type {cell.cellType}.");
                    break;
            }
        }

        private static void ValidateBoxCell(GridCellDefinition cell, int index, GameplayTuningConfig tuning, HashSet<string> boxIds, List<string> errors, ref bool hasStartingSelectable)
        {
            switch (cell.cellType)
            {
                case GridCellType.Empty:
                case GridCellType.DeadCell:
                case GridCellType.KeyCell:
                case GridCellType.LockCell:
                case GridCellType.TunnelCell:
                case GridCellType.PinCell:
                case GridCellType.PinTailCell:
                    return; // validated separately in ValidateM3Lists

                case GridCellType.StandardBox:
                case GridCellType.MysteryBox:
                case GridCellType.FrozenBox:
                    break;

                default:
                    return;
            }

            var box = cell.box;
            if (box == null)
            {
                errors.Add($"Box cell at index {index} (type={cell.cellType}) must include BoxDefinition.");
                return;
            }

            ValidateBoxDefinition(box, index, tuning, boxIds, errors);

            // M3-specific box validation
            if (cell.cellType == GridCellType.MysteryBox)
                ValidateMysteryBox(box, index, errors);

            if (cell.cellType == GridCellType.FrozenBox)
                ValidateFrozenBox(box, index, errors);
        }

        private static void ValidateMysteryBox(BoxDefinition box, int index, List<string> errors)
        {
            // isHidden is implicitly true by cellType; targetColor must still be valid
            if (box.targetColor == ColorType.None)
                errors.Add($"MysteryBox '{box.boxId}' at index {index} must have a valid targetColor.");
        }

        private static void ValidateFrozenBox(BoxDefinition box, int index, List<string> errors)
        {
            if (box.frozenDurability < 1)
                errors.Add($"FrozenBox '{box.boxId}' at index {index} must have frozenDurability >= 1.");
        }

        private static void ValidateBoxDefinition(BoxDefinition box, int index, GameplayTuningConfig tuning, HashSet<string> boxIds, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(box.boxId))
            {
                errors.Add($"Box at grid cell index {index} must have a non-empty BoxId.");
            }
            else if (!boxIds.Add(box.boxId))
            {
                errors.Add($"Duplicate BoxId '{box.boxId}'.");
            }

            if (box.targetColor == ColorType.None)
            {
                errors.Add($"Box '{box.boxId}' must use a valid target color.");
            }

            if (box.capacity <= 0)
            {
                errors.Add($"Box '{box.boxId}' capacity must be greater than zero.");
            }
            else if (tuning != null && box.capacity > tuning.MaximumBoxCapacity)
            {
                errors.Add($"Box '{box.boxId}' capacity {box.capacity} exceeds maximum {tuning.MaximumBoxCapacity}.");
            }
        }

        private static void ValidateStackBoard(StackBoardData stackBoard, GameplayTuningConfig tuning, List<string> errors)
        {
            if (stackBoard == null)
            {
                errors.Add("StackBoardData is missing.");
                return;
            }

            if (stackBoard.Columns <= 0 || stackBoard.Rows <= 0)
            {
                errors.Add("StackBoardData columns and rows must be greater than zero.");
            }

            var capacity = stackBoard.Columns * stackBoard.Rows;
            if (stackBoard.Stacks.Count > capacity)
            {
                errors.Add($"Stack count {stackBoard.Stacks.Count} exceeds board capacity {capacity}.");
            }

            ValidateStacks(stackBoard, tuning, errors);
        }

        private static void ValidateStacks(StackBoardData stackBoard, GameplayTuningConfig tuning, List<string> errors)
        {
            var occupiedPositions = new HashSet<string>();

            for (int i = 0; i < stackBoard.Stacks.Count; i++)
            {
                var stack = stackBoard.Stacks[i];
                if (stack == null)
                {
                    errors.Add($"Stack at index {i} is missing.");
                    continue;
                }

                var resolvedPosition = ResolveStackPosition(stackBoard, stack, i);
                ValidateStackPosition(stackBoard, resolvedPosition, i, occupiedPositions, errors);
                ValidateStackBlocks(stack, i, tuning, errors);
            }
        }

        private static GridPosition ResolveStackPosition(StackBoardData stackBoard, StackDefinition stack, int index)
        {
            if (stack.useExplicitPosition)
            {
                return stack.position;
            }

            var row = stackBoard.Columns <= 0 ? 0 : index / stackBoard.Columns;
            var column = stackBoard.Columns <= 0 ? index : index % stackBoard.Columns;
            return new GridPosition(row, column);
        }

        private static void ValidateStackPosition(StackBoardData stackBoard, GridPosition position, int index, HashSet<string> occupiedPositions, List<string> errors)
        {
            if (position.Row < 0 || position.Row >= stackBoard.Rows || position.Column < 0 || position.Column >= stackBoard.Columns)
            {
                errors.Add($"Stack at index {index} has out-of-bounds position ({position.Row}, {position.Column}).");
            }

            var key = GetPositionKey(position);
            if (!occupiedPositions.Add(key))
            {
                errors.Add($"Duplicate stack position ({position.Row}, {position.Column}).");
            }
        }

        private static void ValidateStackBlocks(StackDefinition stack, int index, GameplayTuningConfig tuning, List<string> errors)
        {
            if (stack.blocksBottomToTop == null || stack.blocksBottomToTop.Count == 0)
            {
                errors.Add($"Stack at index {index} must contain at least one block.");
                return;
            }

            if (tuning != null && stack.blocksBottomToTop.Count > tuning.MaximumStackBlocks)
            {
                errors.Add($"Stack at index {index} exceeds maximum block limit ({stack.blocksBottomToTop.Count} > {tuning.MaximumStackBlocks}).");
            }

            for (int blockIndex = 0; blockIndex < stack.blocksBottomToTop.Count; blockIndex++)
            {
                if (stack.blocksBottomToTop[blockIndex] == ColorType.None)
                {
                    errors.Add($"Stack at index {index} has invalid block color at block index {blockIndex}.");
                }
            }
        }

        private static string GetPositionKey(GridPosition position)
        {
            return $"{position.Row}:{position.Column}";
        }


        private static void ValidateM3Lists(LevelData level, List<string> errors)
        {
            ValidateKeyLockPairs(level, errors);
            ValidateTunnels(level, errors);
            ValidatePins(level, errors);
            ValidateMysteryStacks(level, errors);
        }

        private static void ValidateKeyLockPairs(LevelData level, List<string> errors)
        {
            if (level.keys == null || level.locks == null) return;

            var keyColors  = new Dictionary<ColorType, int>();
            var lockColors = new Dictionary<ColorType, int>();

            foreach (var key in level.keys)
            {
                if (key == null) continue;
                keyColors.TryGetValue(key.color, out int kc);
                keyColors[key.color] = kc + 1;
            }

            foreach (var lk in level.locks)
            {
                if (lk == null) continue;
                lockColors.TryGetValue(lk.color, out int lc);
                lockColors[lk.color] = lc + 1;
            }

            var allColors = new HashSet<ColorType>(keyColors.Keys);
            allColors.UnionWith(lockColors.Keys);
            foreach (var color in allColors)
            {
                keyColors.TryGetValue(color,  out int k);
                lockColors.TryGetValue(color, out int l);
                if (k != 1 || l != 1)
                    errors.Add($"Key/Lock mismatch for color {color}: {k} key(s) and {l} lock(s). Each color must have exactly one of each.");
            }
        }

        private static void ValidateTunnels(LevelData level, List<string> errors)
        {
            if (level.tunnels == null) return;

            var grid = level.gridCellBoardData;
            for (int i = 0; i < level.tunnels.Count; i++)
            {
                var t = level.tunnels[i];
                if (t == null) { errors.Add($"Tunnel at index {i} is null."); continue; }

                if (t.contents == null || t.contents.Count == 0)
                    errors.Add($"Tunnel '{t.tunnelId}' at index {i} must have at least one queued box.");

                var target = t.position;
                switch (t.direction)
                {
                    case FacingDirection.Left:  target = new GridPosition(t.position.Row, t.position.Column - 1); break;
                    case FacingDirection.Right: target = new GridPosition(t.position.Row, t.position.Column + 1); break;
                    case FacingDirection.Up:    target = new GridPosition(t.position.Row - 1, t.position.Column); break;
                    case FacingDirection.Down:  target = new GridPosition(t.position.Row + 1, t.position.Column); break;
                }

                if (grid != null)
                {
                    bool outOfBounds = target.Row < 0 || target.Row >= grid.height ||
                                       target.Column < 0 || target.Column >= grid.width;
                    if (outOfBounds)
                        errors.Add($"Tunnel '{t.tunnelId}' release target is out of grid bounds.");
                }
            }
        }

        private static void ValidatePins(LevelData level, List<string> errors)
        {
            if (level.pins == null) return;

            var grid = level.gridCellBoardData;
            for (int i = 0; i < level.pins.Count; i++)
            {
                var pin = level.pins[i];
                if (pin == null) { errors.Add($"Pin at index {i} is null."); continue; }

                if (pin.length < 1)
                    errors.Add($"Pin '{pin.pinId}' at index {i} must have length >= 1.");

                // Validate all occupied cells are in bounds
                if (grid != null)
                {
                    for (int s = 0; s < pin.length; s++)
                    {
                        var cell = PinCellAt(pin.headPosition, pin.direction, s);
                        bool outOfBounds = cell.Row < 0 || cell.Row >= grid.height ||
                                           cell.Column < 0 || cell.Column >= grid.width;
                        if (outOfBounds)
                        {
                            errors.Add($"Pin '{pin.pinId}' cell at step {s} is out of grid bounds.");
                            break;
                        }
                    }

                    // Validate trigger neighbor is in bounds
                    var trigger = PinTriggerPosition(pin);
                    bool triggerOob = trigger.Row < 0 || trigger.Row >= grid.height ||
                                      trigger.Column < 0 || trigger.Column >= grid.width;
                    if (triggerOob)
                        errors.Add($"Pin '{pin.pinId}' trigger neighbor position is out of grid bounds.");
                }
            }
        }

        private static GridPosition PinCellAt(GridPosition head, PinDirection dir, int step)
        {
            switch (dir)
            {
                case PinDirection.LeftToRight: return new GridPosition(head.Row, head.Column + step);
                case PinDirection.RightToLeft: return new GridPosition(head.Row, head.Column - step);
                case PinDirection.UpToDown:    return new GridPosition(head.Row + step, head.Column);
                case PinDirection.DownToUp:    return new GridPosition(head.Row - step, head.Column);
                default:                       return head;
            }
        }

        private static GridPosition PinTriggerPosition(PinDefinition pin)
        {
            // Trigger = 1 step in the INVERSE direction from the head
            switch (pin.direction)
            {
                case PinDirection.LeftToRight: return new GridPosition(pin.headPosition.Row, pin.headPosition.Column - 1);
                case PinDirection.RightToLeft: return new GridPosition(pin.headPosition.Row, pin.headPosition.Column + 1);
                case PinDirection.UpToDown:    return new GridPosition(pin.headPosition.Row - 1, pin.headPosition.Column);
                case PinDirection.DownToUp:    return new GridPosition(pin.headPosition.Row + 1, pin.headPosition.Column);
                default:                       return pin.headPosition;
            }
        }

        private static void ValidateMysteryStacks(LevelData level, List<string> errors)
        {
            if (level.stackBoard?.Stacks == null) return;

            for (int i = 0; i < level.stackBoard.Stacks.Count; i++)
            {
                var stack = level.stackBoard.Stacks[i];
                if (stack == null || !stack.isHidden) continue;

                if (stack.blocksBottomToTop == null || stack.blocksBottomToTop.Count == 0)
                    errors.Add($"Mystery Stack at index {i} must have at least one block.");

                var pos = ResolveStackPosition(level.stackBoard, stack, i);
                if (pos.Row == 0)
                    errors.Add($"Mystery Stack at index {i} cannot be placed in the lowest row (Row 0).");
            }
        }
    }
}


