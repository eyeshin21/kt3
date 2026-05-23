using UnityEngine;

namespace HexaFall.Gameplay.Config
{
    /// <summary>
    /// Data-driven tuning values used by puzzle validation, runtime rules, and basic game-feel timing.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTuningConfig", menuName = "Hexa Fall/Gameplay/Gameplay Tuning Config")]
    public sealed class GameplayTuningConfig : ScriptableObject
    {
        [SerializeField] private int minimumWaitingSlots = 3;
        [SerializeField] private int maximumWaitingSlots = 8;
        [SerializeField] private int defaultBoxCapacity = 6;
        [SerializeField] private int maximumBoxCapacity = 12;
        [SerializeField] private int maximumStackBlocks = 10;
        [SerializeField] private float boxMoveDuration = 0.45f;
        [SerializeField] private float blockFlyDuration = 0.35f;
        [SerializeField] private float boxClearDuration = 0.2f;
        [SerializeField] private int waitingWarningFreeSlots = 1;
        [SerializeField] private bool enableBasicHaptics = true;
        [SerializeField] private float shuffleAnimDuration = 0.25f;
        [SerializeField] private float boosterTargetHighlightDuration = 0.15f;
        public float boxMoveStepDuration = 0.1f;
        public float delayEachBlockFill = 0.1f;
        public float stackFlowDuration = 0.5f;

        [Header("Fill Rules")]
        [SerializeField] private float validFillAngle = 30f;
        [SerializeField] private Vector3 stackForwardDirection = Vector3.back;
        [SerializeField] private Vector2 activeConveyorZone = new Vector2(0.2f, 0.8f);
        [SerializeField] private Vector2 visibleConveyorZone = new Vector2(0.2f, 0.8f);

        /// <summary>
        /// Minimum valid waiting-area capacity for authored levels.
        /// </summary>
        public int MinimumWaitingSlots => minimumWaitingSlots;

        /// <summary>
        /// Maximum valid waiting-area capacity for authored levels.
        /// </summary>
        public int MaximumWaitingSlots => maximumWaitingSlots;

        /// <summary>
        /// Designer-facing default standard-box capacity.
        /// </summary>
        public int DefaultBoxCapacity => defaultBoxCapacity;

        /// <summary>
        /// Maximum valid standard-box capacity for Milestone 1.
        /// </summary>
        public int MaximumBoxCapacity => maximumBoxCapacity;

        /// <summary>
        /// Maximum valid blocks inside a single stack.
        /// </summary>
        public int MaximumStackBlocks => maximumStackBlocks;

        public float BoxMoveDuration => Mathf.Max(0f, boxMoveDuration);
        public float BlockFlyDuration => Mathf.Max(0f, blockFlyDuration);
        public float BoxClearDuration => Mathf.Max(0f, boxClearDuration);
        public int WaitingWarningFreeSlots => Mathf.Max(0, waitingWarningFreeSlots);
        public bool EnableBasicHaptics => enableBasicHaptics;
        public float ShuffleAnimDuration => Mathf.Max(0f, shuffleAnimDuration);
        public float BoosterTargetHighlightDuration => Mathf.Max(0f, boosterTargetHighlightDuration);

        public float ValidFillAngle => validFillAngle;
        public Vector3 StackForwardDirection => stackForwardDirection;
        public Vector2 ActiveConveyorZone => activeConveyorZone;
        public Vector2 VisibleConveyorZone => visibleConveyorZone;
    }
}
