using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HexaFall.Gameplay.Config;
using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;
using HexaFall.Gameplay.CoreController;
using HexaFall.Gameplay.Validation;
using UnityEngine;
using System;

namespace HexaFall.Gameplay.Runtime
{
    public partial class LevelController : SingletonMono<LevelController>
    {

        [SerializeField] private GameplayTuningConfig tuningConfig;
        [SerializeField] private GridBoardController gridBoardController;
        [SerializeField] private StackBoardController stackBoardController;
        [SerializeField] private WaitingAreaController waitingArea;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip boxSelectedClip;
        [SerializeField] private AudioClip blockCollectedClip;
        [SerializeField] private AudioClip boxClearedClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip failClip;
        [SerializeField] private BoosterManager boosterManager;

        private readonly ILevelValidator levelValidator = new LevelValidator();
        private bool isLevelEnded;
        private bool isCollecting;
        private bool collectionRequested;
        private int activeFlights = 0;
        private Dictionary<string, int> pendingFills = new Dictionary<string, int>();
        private HashSet<ColorType> runningRoutines = new HashSet<ColorType>();

        public LevelData CurrentLevelData { get; private set; }

        private float BoxMoveDuration => tuningConfig == null ? 0.45f : tuningConfig.BoxMoveDuration;
        private float BlockFlyDuration => tuningConfig == null ? 0.35f : tuningConfig.BlockFlyDuration;
        private float BoxClearDuration => tuningConfig == null ? 0.2f : tuningConfig.BoxClearDuration;
        private float CollectPulseDuration => Mathf.Max(0.08f, BlockFlyDuration * 0.5f);
        private int WaitingWarningFreeSlots => tuningConfig == null ? 1 : tuningConfig.WaitingWarningFreeSlots;
        private bool EnableBasicHaptics => tuningConfig == null || tuningConfig.EnableBasicHaptics;

        private float ValidFillAngle => tuningConfig == null ? 30f : tuningConfig.ValidFillAngle;
        private Vector3 StackForwardDirection => tuningConfig == null ? Vector3.back : tuningConfig.StackForwardDirection;
        private Vector2 ActiveConveyorZone => tuningConfig == null ? new Vector2(0.2f, 0.8f) : tuningConfig.ActiveConveyorZone;

        private GameController GameController => GameController.Instance; 

        private void Start()
        {
        }

        public void SetData(LevelData level)
        {
            CurrentLevelData = level;
            var validation = levelValidator.Validate(level, tuningConfig);
            if (!validation.IsValid)
            {
                LogValidationErrors(level, validation.Errors);
                return;
            }

            isLevelEnded = false;
            isCollecting = false;
            collectionRequested = false;
            activeFlights = 0;
            pendingFills.Clear();
            runningRoutines.Clear();
            BindViews();
            boosterManager?.InitializeBoosters(this, gridBoardController, waitingArea);
            ApplyViews(new List<GameplayEvent>());
            gridBoardController?.EvaluateFrozenLockStates();
        }

        public void RetryLevel()
        {
            if (CurrentLevelData == null)
            {
                Debug.LogWarning("GameplayController cannot retry because no level is loaded.");
                return;
            }

            StopAllCoroutines();
            SetData(CurrentLevelData);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                bool isTargeting = boosterManager != null && boosterManager.IsTargeting;
                if (!CanGridSelectBox() && !isTargeting) return;

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.SphereCastAll(ray, 0.3f);
                
                BoxController bestBox = null;
                float bestScore = float.MaxValue;

                foreach (var hit in hits)
                {
                    var box = hit.collider.GetComponentInParent<BoxController>();
                    bool isValid = false;
                    if (isTargeting && boosterManager.ActiveBooster != null)
                    {
                        isValid = boosterManager.ActiveBooster.IsValidTarget(box);
                    }
                    else if (box != null)
                    {
                        isValid = box.CanBeSelected;
                    }
                    
                    if (box != null && isValid)
                    {
                        // Calculate distance from the ray to the center of the box
                        float distToRay = Vector3.Cross(ray.direction, box.transform.position - ray.origin).magnitude;
                        
                        // We prefer the box closest to the center of the ray (where the user actually tapped)
                        // hit.distance is added as a very small tie-breaker
                        float score = distToRay + (hit.distance * 0.001f);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestBox = box;
                        }
                    }
                }

                if (bestBox != null && gridBoardController != null)
                {
                    if (boosterManager != null && boosterManager.IsTargeting)
                    {
                        boosterManager.NotifyTargetSelected(bestBox);
                        return;
                    }
                    gridBoardController.TrySelectBox(bestBox.BoxId);
                }
            }
        }

        private bool CanGridSelectBox()
        {
            return !isLevelEnded && waitingArea != null && waitingArea.HasFreeSlot;
        }

        private void OnGridBoxSelected(BoxController box)
        {
            if (box == null || waitingArea == null)
                return;

            var path = gridBoardController == null ? new List<Vector3>() : gridBoardController.FindPathToTopExit(box);
            var exitPos = path.Count > 0 ? path[path.Count - 1] : box.transform.position;

            if (!waitingArea.TryAdd(box.BoxId, exitPos))
                return;

            var sentPosition = box.GridPosition;

            HandleM3Triggers(sentPosition);

            gridBoardController?.RefreshPickableBoxes();
            gridBoardController?.ApplyState();
            waitingArea.ApplyState();
            PlayFeedback(GameplayEventType.BoxSelected);

            if (gridBoardController != null)
            {
                waitingArea.SetFastMode(gridBoardController.AreAllBoxesPicked());
            }

            StartCoroutine(MoveSelectedBoxToWaiting(box, path));
        }

        private IEnumerator MoveSelectedBoxToWaiting(BoxController box, IReadOnlyList<Vector3> path)
        {
            var waitingIndex = Mathf.Max(0, waitingArea.IndexOf(box.BoxId));
            var slotTrans = waitingArea.GetSlotTransform(waitingIndex);
            
            yield return box.PlayMoveAlongGridPathThenJump(path, slotTrans, tuningConfig.boxMoveStepDuration, tuningConfig.boxMoveStepDuration);

            var currentIndex = waitingArea.IndexOf(box.BoxId);
            if (currentIndex >= 0)
            {
                box.transform.localScale = Vector3.one;
            }

            RequestCollection();
        }

        private void RequestCollection()
        {
            if (isLevelEnded)
            {
                return;
            }

            collectionRequested = true;
            if (!isCollecting)
            {
                StartCoroutine(ResolveCollectionQueue());
            }
        }

        private IEnumerator ResolveCollectionQueue()
        {
            isCollecting = true;
            while (collectionRequested && !isLevelEnded)
            {
                collectionRequested = false;
                var events = new List<GameplayEvent>();
                yield return ResolveCollections(events);

                yield return RunM3PostClear(events);

                gridBoardController?.RefreshPickableBoxes();
                EvaluateEndState(events);
                ApplyViews(events);
            }

            isCollecting = false;
        }

        private BoxController FindBox(string boxId)
        {
            return gridBoardController == null ? null : gridBoardController.FindBox(boxId);
        }

        private IEnumerator RunM3PostClear(List<GameplayEvent> events)
        {
            if (gridBoardController == null) yield break;

            var revealedStacks = stackBoardController?.RevealHiddenStacksInCurrentRow();
            if (revealedStacks != null)
            {
                foreach (var s in revealedStacks)
                {
                    yield return s.PlayReveal(0.25f);
                    events.Add(new GameplayEvent(GameplayEventType.StackRevealed));
                }
            }
        }

        private void HandleM3Triggers(GridPosition initialSentPosition)
        {
            if (gridBoardController == null) return;

            var emptiedCells = new Queue<GridPosition>();
            emptiedCells.Enqueue(initialSentPosition);

            bool anyChanges = false;

            // Thawing only happens once per box pick, not per cascaded empty cell
            var thawed = gridBoardController.TickFrozenBoxes();
            if (thawed.Count > 0) anyChanges = true;

            while (emptiedCells.Count > 0)
            {
                int count = emptiedCells.Count;
                for (int i = 0; i < count; i++)
                {
                    var pos = emptiedCells.Dequeue();

                    gridBoardController.UnlockAdjacentFrozenBoxes(pos);

                    var revealed = gridBoardController.RevealAdjacentHiddenBoxes(pos);
                    foreach (var b in revealed)
                    {
                        StartCoroutine(b.PlayReveal(0.2f));
                        anyChanges = true;
                    }

                    var resolvedPins = gridBoardController.ResolvePinTriggers(pos);
                    foreach (var pin in resolvedPins)
                    {
                        StartCoroutine(pin.PlayBreak(0.3f));
                        foreach (var pinPos in pin.OccupiedPositions)
                            emptiedCells.Enqueue(pinPos);
                        anyChanges = true;
                    }
                }

                var readyKeys = gridBoardController.ResolveKeyActivations();
                foreach (var key in readyKeys)
                {
                    emptiedCells.Enqueue(key.Position);
                    if (gridBoardController.TryGetLock(key.Color, out var lk) && !lk.IsDestroyed)
                    {
                        StartCoroutine(key.PlayActivate(lk.KeyTargetPos.position, 0.35f));
                        StartCoroutine(lk.PlayDestroy(0.35f, 0.35f));
                        emptiedCells.Enqueue(lk.Position);
                    }
                    else
                    {
                        StartCoroutine(key.PlayActivate(Vector3.zero, 0f));
                    }
                    anyChanges = true;
                }

                var tunnelBoxes = new List<BoxController>();
                var activatedTunnels = new List<TunnelController>();
                var removedTunnels = new List<GridPosition>();
                gridBoardController.ResolveTunnelReleases(tunnelBoxes, activatedTunnels, removedTunnels);
                if (tunnelBoxes.Count > 0)
                {
                    anyChanges = true;
                    for (int i = 0; i < tunnelBoxes.Count; i++)
                    {
                        StartCoroutine(activatedTunnels[i].PlayRelease(0.3f));
                        StartCoroutine(tunnelBoxes[i].PlayEmergeFromTunnel(activatedTunnels[i].transform.position, 0.4f));
                    }
                }

                foreach (var tp in removedTunnels)
                {
                    emptiedCells.Enqueue(tp);
                    anyChanges = true;
                }
            }

            if (anyChanges)
                gridBoardController.RefreshPickableBoxes();
        }

        private IEnumerator ResolveCollections(List<GameplayEvent> events)
        {
            while (true)
            {
                bool anyNeedsCollection = false;

                foreach (var boxId in waitingArea.WaitingBoxIds)
                {
                    if (string.IsNullOrEmpty(boxId)) continue;
                    var box = FindBox(boxId);
                    if (box != null && box.IsArrivedInWaitingArea && !box.IsCleared && !box.IsFull)
                    {
                        pendingFills.TryGetValue(boxId, out int pending);
                        if (/*box.FillCount +*/ pending < box.Capacity)
                        {
                            anyNeedsCollection = true;
                            if (!runningRoutines.Contains(box.TargetColor))
                            {
                                StartCoroutine(CollectColorRoutine(box, events));
                            }
                        }
                    }
                }

                if (runningRoutines.Count == 0 && activeFlights == 0)
                {
                    break;
                }

                yield return null;
            }
        }

        private IEnumerator CollectColorRoutine(BoxController boxController, List<GameplayEvent> events)
        {
            var color = boxController.TargetColor;

            runningRoutines.Add(color);

            while (!isLevelEnded)
            {
                var targetBoxes = GetArrivedWaitingBoxesById().Values.Where(b => b.TargetColor == color && !b.IsCleared && !b.IsFull).ToList();
                if (targetBoxes.Count == 0) break;

                var stacks = FindAllMatchingStacks(color);

                if ((stacks == null || stacks.Count <= 0) && stackBoardController != null && stackBoardController.IsFlowing)
                {
                    yield return null;
                    continue;
                }

                if (stacks == null || stacks.Count <= 0)
                {
                    break;
                }

                int maxI = 0;
                //float deltaDelay = 0.1f;
                bool anyBlockSent = false;

                foreach (var stack in stacks)
                {
                    BoxController nearestBox = null;
                    float minSqrDist = float.MaxValue;

                    foreach (var box in targetBoxes)
                    {
                        pendingFills.TryGetValue(box.BoxId, out int pending);
                        if (pending >= box.Capacity) continue;

                        if (!waitingArea.IsInActiveZone(box.BoxId, ActiveConveyorZone.x, ActiveConveyorZone.y))
                        {
                            continue;
                        }

                        Vector3 dir = box.CollectionWorldPosition - stack.transform.position;
                        dir.y = 0;
                        if (dir != Vector3.zero)
                        {
                            float angle = Vector3.Angle(StackForwardDirection, dir.normalized);
                            if (angle <= ValidFillAngle)
                            {
                                float sqrDist = dir.sqrMagnitude;
                                if (sqrDist < minSqrDist)
                                {
                                    minSqrDist = sqrDist;
                                    nearestBox = box;
                                }
                            }
                        }
                    }

                    if (nearestBox == null)
                    {
                        continue;
                    }

                    float currentDelay = 0f;
                    int i = 0;
                    while (stack.HasColor(color))
                    {
                        pendingFills.TryGetValue(nearestBox.BoxId, out int pending);
                        if (pending >= nearestBox.Capacity)
                        {
                            break;
                        }

                        var topBlock = stack.PopBlockOfColor(color);
                        var startPos = topBlock.transform.position;
                        var sourcePosition = stack.Position;

                        if (!pendingFills.ContainsKey(nearestBox.BoxId))
                        {
                            pendingFills[nearestBox.BoxId] = 0;
                        }
                        pendingFills[nearestBox.BoxId]++;

                        StartCoroutine(AnimateBlockToBox(stack, color, startPos, nearestBox, sourcePosition, events, currentDelay, topBlock));
                        
                        //float nextDelta = Mathf.Max(0.02f, deltaDelay - (i * 0.012f));
                        currentDelay += tuningConfig.delayEachBlockFill;

                        i++;
                        anyBlockSent = true;
                        maxI = Mathf.Max(i, maxI);
                    }
                }

                if (!anyBlockSent)
                {
                    yield return null;
                }
                else
                {
                    foreach (var stack in stacks)
                    {
                        if (!stack.HasBlocks && stackBoardController != null)
                        {
                            yield return stackBoardController.FlowStacksToLowestPlaceholders(maxI * tuningConfig.delayEachBlockFill);
                            var revealed = stackBoardController.RevealHiddenStacksInCurrentRow();
                            if (revealed != null)
                            {
                                foreach (var s in revealed)
                                {
                                    yield return s.PlayReveal(0.25f);
                                    events.Add(new GameplayEvent(GameplayEventType.StackRevealed));
                                }
                            }
                            break;
                        }
                    }
                }

            }

            runningRoutines.Remove(color);
        }

        private IEnumerator AnimateBlockToBox(StackController stack, ColorType color, Vector3 startPos, BoxController box, GridPosition sourcePosition, List<GameplayEvent> events, float delay = 0, HexaBlock topBlock = null)
        {
            activeFlights++;

            yield return new WaitForSeconds(delay); 

            yield return stack.PlayDetachedBlockFlight(color, startPos, box.TargetBlockFly, BlockFlyDuration, topBlock);

            box.AddBlock();
            events.Add(new GameplayEvent(GameplayEventType.BlockCollected, box.BoxId, sourcePosition, color, box.FillCount));
            PlayFeedback(GameplayEventType.BlockCollected);
            
            if (box.gameObject.activeInHierarchy)
            {
                box.StartCoroutine(box.PlayCollectPulse(CollectPulseDuration));
            }

            if (box.IsFull)
            {
                box.IsCleared = true;
                box.IsInWaitingArea = false;
                yield return box.PlayClear(BoxClearDuration);
                waitingArea.Remove(box.BoxId);
                waitingArea.ApplyState();
                events.Add(new GameplayEvent(GameplayEventType.BoxCleared, box.BoxId, null, box.TargetColor, box.FillCount));
                PlayFeedback(GameplayEventType.BoxCleared);
            }

            activeFlights--;

            RequestCollection();
        }

        private Dictionary<string, BoxController> GetArrivedWaitingBoxesById()
        {
            var boxes = new Dictionary<string, BoxController>();
            if (waitingArea == null)
            {
                return boxes;
            }

            foreach (var boxId in waitingArea.WaitingBoxIds)
            {
                if (string.IsNullOrEmpty(boxId)) continue;
                var box = FindBox(boxId);
                if (box != null && box.IsArrivedInWaitingArea)
                {
                    boxes[boxId] = box;
                }
            }

            return boxes;
        }

        private StackController FindMatchingStack(ColorType targetColor)
        {
            return stackBoardController == null ? null : stackBoardController.FindMatchingEligibleStack(targetColor);
        }

        private List<StackController> FindAllMatchingStacks(ColorType targetColor, bool lowestRowOnly = true)
        {
            return stackBoardController == null ? new List<StackController>() : stackBoardController.FindAllMatchingEligibleStacks(targetColor, lowestRowOnly);
        }

        private bool CanAnyWaitingBoxCollect()
        {
            if (waitingArea == null)
            {
                return false;
            }

            foreach (var boxId in waitingArea.WaitingBoxIds)
            {
                if (string.IsNullOrEmpty(boxId)) continue;
                var box = FindBox(boxId);
                if (box != null && !box.IsCleared && !box.IsFull && FindMatchingStack(box.TargetColor) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasMovingWaitingBox()
        {
            if (waitingArea == null)
            {
                return false;
            }

            foreach (var boxId in waitingArea.WaitingBoxIds)
            {
                if (string.IsNullOrEmpty(boxId)) continue;
                var box = FindBox(boxId);
                if (box != null && !box.IsArrivedInWaitingArea)
                {
                    return true;
                }
            }

            return false;
        }

        private void EvaluateEndState(List<GameplayEvent> events)
        {
            if (gridBoardController != null && gridBoardController.AreAllBoxesCleared())
            {
                isLevelEnded = true;
                events.Add(new GameplayEvent(GameplayEventType.PuzzleWon));
                PlayFeedback(GameplayEventType.PuzzleWon);
                return;
            }

            if (waitingArea != null && waitingArea.IsFull && !HasMovingWaitingBox() && !CanAnyWaitingBoxCollect())
            {
                isLevelEnded = true;
                events.Add(new GameplayEvent(GameplayEventType.PuzzleFailedOutOfSpace));
                PlayFeedback(GameplayEventType.PuzzleFailedOutOfSpace);
            }
        }

        private void BindViews()
        {
            gridBoardController?.Build(CurrentLevelData.gridCellBoardData, CanGridSelectBox, OnGridBoxSelected);
            gridBoardController?.BuildM3Mechanics(CurrentLevelData);
            stackBoardController?.Build(CurrentLevelData.stackBoard);
            waitingArea?.Build(CurrentLevelData.waitingSlots, tuningConfig.MaximumWaitingSlots, WaitingWarningFreeSlots, tuningConfig.VisibleConveyorZone);
        }

        private void ApplyViews(IReadOnlyList<GameplayEvent> events)
        {
            gridBoardController?.ApplyState();
            stackBoardController?.ApplyState();
            waitingArea?.ApplyState();

            if (events == null)
            {
                return;
            }

            foreach (var gameplayEvent in events)
            {
                if (gameplayEvent.EventType == GameplayEventType.PuzzleWon)
                {
                    //ShowWin();
                    GameController.WinLevel();
                }
                else if (gameplayEvent.EventType == GameplayEventType.PuzzleFailedOutOfSpace)
                {
                    //ShowLose();
                    GameController.LoseGame();
                }
            }
        }

        private void PlayFeedback(GameplayEventType eventType)
        {
            var clip = GetClip(eventType);
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }

            if (EnableBasicHaptics && (eventType == GameplayEventType.BoxCleared || eventType == GameplayEventType.PuzzleWon || eventType == GameplayEventType.PuzzleFailedOutOfSpace))
            {
                Handheld.Vibrate();
            }
        }

        private AudioClip GetClip(GameplayEventType eventType)
        {
            switch (eventType)
            {
                case GameplayEventType.BoxSelected:
                    return boxSelectedClip;
                case GameplayEventType.BlockCollected:
                    return blockCollectedClip;
                case GameplayEventType.BoxCleared:
                    return boxClearedClip;
                case GameplayEventType.PuzzleWon:
                    return winClip;
                case GameplayEventType.PuzzleFailedOutOfSpace:
                    return failClip;
                default:
                    return null;
            }
        }

        private static void LogValidationErrors(LevelData level, IReadOnlyList<string> errors)
        {
            var levelName = level == null ? "<missing>" : level.name;
            Debug.LogError($"Cannot load level '{levelName}'. Validation failed:\n- {string.Join("\n- ", errors)}");
        }

        internal float GetCurrentLevelProgress()
        {
            //throw new NotImplementedException();
            return 0;
        }
    }
}
