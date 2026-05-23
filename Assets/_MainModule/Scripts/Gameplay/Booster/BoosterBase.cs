using System;
using System.Collections;
using HexaFall.Gameplay.CoreController;
using HexaFall.Gameplay.Runtime;
using UnityEngine;

namespace HexaFall.Gameplay.Booster
{
    /// <summary>
    /// Abstract base for all in-level boosters. Each concrete booster owns its own execution logic.
    /// </summary>
    public abstract class BoosterBase : MonoBehaviour
    {
        // ── Lifecycle ─────────────────────────────────────────
        /// <summary>Called once at level load. Inject board references.</summary>
        public abstract void Initialize(LevelController level,
                                        GridBoardController grid,
                                        WaitingAreaController waitingArea);

        // ── Player Interaction ────────────────────────────────
        /// <summary>
        /// Called when the player taps the booster button.
        /// Instant boosters call Execute(null) and return true.
        /// Targeted boosters enter targeting mode and return true.
        /// Returns false if CanUse is false.
        /// </summary>
        public abstract bool Activate();

        /// <summary>Cancel targeting mode (re-tap or invalid target).</summary>
        public abstract void Deactivate();

        /// <summary>
        /// Apply the booster effect.
        /// Targeted: called by BoosterManager when a valid target is tapped.
        /// Instant:  called directly inside Activate().
        /// </summary>
        public abstract IEnumerator Execute(BoxController target = null);

        // ── State Queries ─────────────────────────────────────
        /// <summary>True when this booster needs a target tap before Execute runs.</summary>
        public abstract bool RequiresTarget { get; }

        /// <summary>True when the booster can currently be used (inventory > 0 AND pre-conditions met).</summary>
        public abstract bool CanUse { get; }

        /// <summary>Remaining uses (stubbed as serialized int; economy added in M5).</summary>
        public abstract int Count { get; protected set; }

        // ── Events ────────────────────────────────────────────
        public event Action OnActivated;   // targeting entered / instant effect started
        public event Action OnDeactivated; // targeting cancelled
        public event Action OnUsed;        // effect applied, inventory consumed

        /// <summary>Check if a specific box is a valid target for this booster.</summary>
        public virtual bool IsValidTarget(BoxController target) => false;

        protected void FireActivated()   => OnActivated?.Invoke();
        protected void FireDeactivated() => OnDeactivated?.Invoke();

        protected void ConsumeOne()
        {
            if (Count > 0) Count--;
            OnUsed?.Invoke();
        }
    }
}
