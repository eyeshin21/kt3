using System;
using System.Collections.Generic;
using HexaFall.Gameplay.Booster;
using HexaFall.Gameplay.CoreController;
using UnityEngine;

namespace HexaFall.Gameplay.Runtime
{
    public class BoosterManager : MonoBehaviour
    {
        [SerializeField] private SuperPickerBoxBooster superPickerBooster;
        [SerializeField] private MagicWandBooster      magicWandBooster;
        [SerializeField] private ShuffleBooster        shuffleBooster;
        [SerializeField] private AddSlotBooster        addSlotBooster;

        public BoosterBase ActiveBooster { get; private set; }
        public bool        IsTargeting   => ActiveBooster != null && ActiveBooster.RequiresTarget;

        public event Action<BoosterBase> OnBoosterUsed;

        private GridBoardController grid;
        private WaitingAreaController waitingArea;

        private void Awake()
        {
            if (superPickerBooster == null) superPickerBooster = GetComponent<SuperPickerBoxBooster>() ?? gameObject.AddComponent<SuperPickerBoxBooster>();
            if (magicWandBooster == null) magicWandBooster = GetComponent<MagicWandBooster>() ?? gameObject.AddComponent<MagicWandBooster>();
            if (shuffleBooster == null) shuffleBooster = GetComponent<ShuffleBooster>() ?? gameObject.AddComponent<ShuffleBooster>();
            if (addSlotBooster == null) addSlotBooster = GetComponent<AddSlotBooster>() ?? gameObject.AddComponent<AddSlotBooster>();
        }

        public void InitializeBoosters(LevelController level,
                                       GridBoardController grid,
                                       WaitingAreaController waitingArea)
        {
            this.grid = grid;
            this.waitingArea = waitingArea;
            foreach (var b in AllBoosters())
            {
                if (b == null) continue; // Safety check in case serialized fields are empty
                b.Initialize(level, grid, waitingArea);
                b.OnUsed += () => OnBoosterUsed?.Invoke(b);
            }
        }

        public void TryActivate(HexaFall.Gameplay.Booster.BoosterType type, Transform sourceTransform = null)
        {
            var booster = GetBooster(type);

            Debug.Log($"Trying to activate booster {type} (can use: {booster?.CanUse})");

            if (booster == null || !booster.CanUse) return;

            if (ActiveBooster == booster) { CancelActiveBooster(); return; } // toggle off

            CancelActiveBooster();
            
            if (booster.RequiresTarget)
            {
                ActiveBooster = booster;
                booster.Activate();
                UpdateHighlights(true);
            }
            else
            {
                if (booster is AddSlotBooster addSlot) {
                    addSlot.SetSourceTransform(sourceTransform);
                }
                // Instant boosters execute immediately without entering targeting state
                booster.Activate();
            }
        }

        /// <summary>
        /// Called by LevelController.Update() when a board tap occurs during targeting mode.
        /// </summary>
        public void NotifyTargetSelected(BoxController target)
        {
            if (ActiveBooster == null || !ActiveBooster.RequiresTarget) return;
            UpdateHighlights(false);
            var booster = ActiveBooster;
            ActiveBooster = null;
            StartCoroutine(booster.Execute(target));
        }

        public void CancelActiveBooster()
        {
            if (ActiveBooster == null) return;
            UpdateHighlights(false);
            ActiveBooster.Deactivate();
            ActiveBooster = null;
        }

        private void UpdateHighlights(bool active)
        {
            if (grid == null) return;
            foreach (var box in grid.Boxes)
            {
                if (active && ActiveBooster != null && ActiveBooster.IsValidTarget(box))
                {
                    box.SetBoosterHighlight(true);
                }
                else
                {
                    box.SetBoosterHighlight(false);
                }
            }
        }

        private BoosterBase GetBooster(HexaFall.Gameplay.Booster.BoosterType type) => type switch
        {
            HexaFall.Gameplay.Booster.BoosterType.SuperPickerBox => superPickerBooster,
            HexaFall.Gameplay.Booster.BoosterType.MagicWand      => magicWandBooster,
            HexaFall.Gameplay.Booster.BoosterType.Shuffle        => shuffleBooster,
            HexaFall.Gameplay.Booster.BoosterType.AddSlot        => addSlotBooster,
            _                          => null,
        };

        private IEnumerable<BoosterBase> AllBoosters()
        {
            yield return superPickerBooster;
            yield return magicWandBooster;
            yield return shuffleBooster;
            yield return addSlotBooster;
        }
    }
}
