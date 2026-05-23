using System.Collections;
using HexaFall.Gameplay.CoreController;
using HexaFall.Gameplay.Runtime;
using UnityEngine;

namespace HexaFall.Gameplay.Booster
{
    public class AddSlotBooster : BoosterBase
    {
        [SerializeField] private int initialCount = 1;

        private LevelController level;
        private GridBoardController grid;
        private WaitingAreaController waitingArea;

        public override int Count { get; protected set; }
        public override bool RequiresTarget => false;
        public override bool CanUse => Count > 0 && waitingArea != null && !waitingArea.IsAtMaxCapacity;

        public override void Initialize(LevelController level, GridBoardController grid, WaitingAreaController waitingArea)
        {
            this.level = level;
            this.grid = grid;
            this.waitingArea = waitingArea;
            Count = initialCount;
        }

        public override bool Activate()
        {
            if (!CanUse) return false;
            
            FireActivated();
            StartCoroutine(Execute(null));
            return true;
        }

        public override void Deactivate()
        {
            FireDeactivated();
        }

        public override IEnumerator Execute(BoxController target = null)
        {
            if (waitingArea.IsAtMaxCapacity) yield break;

            waitingArea.AddSlot();
            waitingArea.ApplyState();
            ConsumeOne();
        }
    }
}
