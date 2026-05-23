using System.Collections;
using System.Collections.Generic;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.CoreController;
using UnityEngine;

namespace HexaFall.Gameplay.Runtime
{
    public partial class LevelController
    {
        /// <summary>
        /// Called by SuperPickerBoxBooster after PlayClear completes.
        /// Runs M3 post-clear chain, board refresh, and win/fail evaluation.
        /// </summary>
        public IEnumerator RunPostBoosterClearCoroutine(BoxController box)
        {
            HandleM3Triggers(box.GridPosition);
            gridBoardController?.RefreshPickableBoxes();
            
            var events = new List<GameplayEvent>();
            events.Add(new GameplayEvent(GameplayEventType.BoxCleared,
                                         box.BoxId, null, box.TargetColor, box.FillCount));
            
            yield return RunM3PostClear(events);
            
            EvaluateEndState(events);
            ApplyViews(events);
        }

        /// <summary>
        /// Called by MagicWandBooster to move a box to the waiting area without a frontier check.
        /// Runs HandleM3Triggers, animates the move, then triggers collection.
        /// </summary>
        public IEnumerator RunPostBoosterSendCoroutine(BoxController box)
        {
            var path    = gridBoardController?.FindPathToTopExit(box) ?? new List<Vector3>();
            var exitPos = path.Count > 0 ? path[path.Count - 1] : box.transform.position;

            waitingArea.TryAdd(box.BoxId, exitPos);

            HandleM3Triggers(box.GridPosition);
            gridBoardController?.RefreshPickableBoxes();
            gridBoardController?.ApplyState();
            waitingArea.ApplyState();
            PlayFeedback(GameplayEventType.BoxSelected);

            yield return MoveSelectedBoxToWaiting(box, path);
            // MoveSelectedBoxToWaiting ends with RequestCollection()
        }

        public IEnumerator RunSuperPickerFillCoroutine(BoxController box)
        {
            var color = box.TargetColor;
            int blocksNeeded = box.Capacity - box.FillCount;
            if (blocksNeeded <= 0) yield break;

            yield return box.PlayBoosterActivation(0.3f);

            var stacks = FindAllMatchingStacks(color, false);
            int blocksFilled = 0;
            float currentDelay = 0f;
            float deltaDelay = 0.1f;
            int maxI = 0;

            foreach (var stack in stacks)
            {
                int stackPopCount = 0;
                while (stack.HasColor(color) && blocksFilled < blocksNeeded)
                {
                    var topBlock = stack.PopBlockOfColor(color);
                    var startPos = topBlock.transform.position;
                    
                    StartCoroutine(AnimateBoosterBlockToBox(stack, color, startPos, box, currentDelay, topBlock));
                    
                    blocksFilled++;
                    stackPopCount++;
                    
                    float nextDelta = Mathf.Max(0.02f, deltaDelay - (stackPopCount * 0.012f));
                    currentDelay += nextDelta;
                    maxI = Mathf.Max(stackPopCount, maxI);
                }
                if (blocksFilled >= blocksNeeded) break;
            }

            if (blocksFilled > 0)
            {
                // Wait until all block flights finish
                while (activeFlights > 0)
                {
                    yield return null;
                }

                // Handle stack flow if any stacks were depleted
                foreach (var stack in stacks)
                {
                    if (!stack.HasBlocks && stackBoardController != null)
                    {
                        yield return stackBoardController.FlowStacksToLowestPlaceholders(maxI * deltaDelay);
                        var revealed = stackBoardController.RevealHiddenStacksInCurrentRow();
                        if (revealed != null)
                        {
                            foreach (var s in revealed)
                            {
                                yield return s.PlayReveal(0.25f);
                            }
                        }
                        break;
                    }
                }
                
                // Clear only if box.IsFull
                if (box.IsFull)
                {
                    box.IsCleared = true;
                    box.IsInWaitingArea = false;
                    waitingArea.Remove(box.BoxId);
                    waitingArea.ApplyState();

                    yield return box.PlayClear(BoxClearDuration);
                    yield return RunPostBoosterClearCoroutine(box);
                }
            }
        }

        private IEnumerator AnimateBoosterBlockToBox(StackController stack, ColorType color, Vector3 startPos, BoxController box, float delay, HexaBlock topBlock)
        {
            activeFlights++;

            yield return new WaitForSeconds(delay); 

            yield return stack.PlayDetachedBlockFlight(color, startPos, box.TargetBlockFly, BlockFlyDuration, topBlock);

            box.AddBlock();
            PlayFeedback(GameplayEventType.BlockCollected);
            
            if (box.gameObject.activeInHierarchy)
            {
                box.StartCoroutine(box.PlayCollectPulse(CollectPulseDuration));
            }

            activeFlights--;
        }
    }
}
