using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class WaitingAreaController
    {
        private int maxCapacity;

        public int  MaxCapacity      => maxCapacity;
        public bool IsAtMaxCapacity  => capacity >= maxCapacity;

        /// <summary>
        /// Extended Build overload used when booster system is active.
        /// Reads maxCapacity from tuningConfig.MaximumWaitingSlots, passed in by LevelController.
        /// </summary>
        public void Build(int startingCapacity, int maxCap, int warningThreshold, Vector2 visibleZone)
        {
            maxCapacity = maxCap;
            Build(startingCapacity, warningThreshold, visibleZone); // delegates to existing Build()
        }

        /// <summary>Adds one waiting slot up to maxCapacity. Spawns a slot visual.</summary>
        public void AddSlot()
        {
            if (capacity >= maxCapacity) return;
            capacity++;
            waitingBoxIds.Add(null);
            RebuildSlots();
            m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";
        }
    }
}
