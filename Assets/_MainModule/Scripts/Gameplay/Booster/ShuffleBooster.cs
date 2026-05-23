using System.Collections;
using HexaFall.Gameplay.CoreController;
using HexaFall.Gameplay.Runtime;
using UnityEngine;

namespace HexaFall.Gameplay.Booster
{
    public class ShuffleBooster : BoosterBase
    {
        [SerializeField] private int initialCount = 1;
        [SerializeField] private float shuffleAnimDuration = 0.25f;

        private LevelController level;
        private GridBoardController grid;
        private WaitingAreaController waitingArea;

        public override int Count { get; protected set; }
        public override bool RequiresTarget => false;
        public override bool CanUse => Count > 0;

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
            var eligible = grid.GetEligibleShuffleBoxes();
            if (eligible.Count < 2) yield break;

            float anticipationDuration = 0.2f;
            foreach (var box in eligible)
            {
                box.StartCoroutine(box.PlayShuffleAnticipation(anticipationDuration));
            }
            yield return new WaitForSeconds(anticipationDuration);

            grid.ApplyPhysicalShuffle(eligible);

            float flightDuration = 0.5f;
            foreach (var box in eligible)
            {
                box.StartCoroutine(box.PlayShuffleFlight(box.transform.parent.position, flightDuration));
            }
            yield return new WaitForSeconds(flightDuration);

            grid.RefreshPickableBoxes();
            grid.ApplyState();

            float landingDuration = 0.25f;
            foreach (var box in eligible)
            {
                box.StartCoroutine(box.PlayShuffleLanding(landingDuration));
            }
            yield return new WaitForSeconds(landingDuration);

            ConsumeOne();
        }
    }
}
