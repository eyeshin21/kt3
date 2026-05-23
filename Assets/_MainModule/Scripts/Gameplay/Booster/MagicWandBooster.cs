using System.Collections;
using HexaFall.Gameplay.CoreController;
using HexaFall.Gameplay.Runtime;
using UnityEngine;

namespace HexaFall.Gameplay.Booster
{
    public class MagicWandBooster : BoosterBase
    {
        [SerializeField] private int initialCount = 3;

        private LevelController level;
        private GridBoardController grid;
        private WaitingAreaController waitingArea;

        public override bool RequiresTarget => true;
        public override bool CanUse => Count > 0 && waitingArea.HasFreeSlot;
        public override int Count { get; protected set; }

        private void Awake()
        {
            Count = initialCount;
        }

        public override void Initialize(LevelController level, GridBoardController grid, WaitingAreaController waitingArea)
        {
            this.level = level;
            this.grid = grid;
            this.waitingArea = waitingArea;
        }

        public override bool Activate()
        {
            if (!CanUse) return false;
            FireActivated();
            return true;
        }

        public override void Deactivate()
        {
            FireDeactivated();
        }

        public override bool IsValidTarget(BoxController target)
        {
            if (target == null) return false;
            if (target.IsCleared || target.IsInWaitingArea || target.IsHidden || target.FrozenDurability > 0) return false;
            return true;
        }

        public override IEnumerator Execute(BoxController target = null)
        {
            if (target == null || !IsValidTarget(target)) yield break;
            if (!waitingArea.HasFreeSlot) yield break;

            grid.ForceMarkBoxAsWaiting(target.BoxId);            // bypasses frontier check
            yield return level.RunPostBoosterSendCoroutine(target); // move + M3 triggers + collection

            ConsumeOne();
        }
    }
}
