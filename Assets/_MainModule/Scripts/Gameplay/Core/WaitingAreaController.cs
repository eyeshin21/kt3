using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lean.Pool;
using TMPro;

namespace HexaFall.Gameplay.CoreController
{
    /// <summary>
    /// Waiting-area component that owns waiting box IDs and slot rendering.
    /// </summary>
    public sealed partial class WaitingAreaController : MonoBehaviour
    {
        [SerializeField] private Transform slotRoot;
        [SerializeField] private WaitingSlotController slotPrefab;
        [SerializeField] private TextMeshPro m_textSlotOccupied;
        [SerializeField] private Material m_materialConveyorTranslate;
        [SerializeField] private float m_conveyorTextureScrollSpeed = 0.05f;

        [SerializeField] private float slotSpacing = 1f;
        [SerializeField] private float conveyorSpeed = 1f;

        private readonly List<string> waitingBoxIds = new List<string>();
        private readonly List<WaitingSlotController> slots = new List<WaitingSlotController>();
        private float currentConveyorOffset = 0f;
        private int capacity;
        private int warningFreeSlots;
        private Vector2 visibleConveyorZone = new Vector2(0.2f, 0.8f);

        public IReadOnlyList<string> WaitingBoxIds => waitingBoxIds;
        public int Capacity => capacity;
        public bool HasFreeSlot => waitingBoxIds.Any(id => string.IsNullOrEmpty(id));
        public bool IsFull => !HasFreeSlot;
        public int FreeSlots => waitingBoxIds.Count(id => string.IsNullOrEmpty(id));

        public void Build(int waitingCapacity, int warningThreshold, Vector2 visibleZone)
        {
            capacity = waitingCapacity;
            warningFreeSlots = Mathf.Max(0, warningThreshold);
            waitingBoxIds.Clear();
            for (int i = 0; i < capacity; i++) waitingBoxIds.Add(null);
            currentConveyorOffset = 0f;
            visibleConveyorZone = visibleZone;
            ClearChildren();
            RebuildSlots();
            m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";

            if (m_materialConveyorTranslate != null)
            {
                m_materialConveyorTranslate.SetTextureOffset("_BaseMap", new Vector2(0, 0));
            }
        }

        public void ApplyState()
        {
            if (slots.Count != capacity)
            {
                RebuildSlots();
            }
            else
            {
                var warning = IsWarningState();
                for (int i = 0; i < capacity; i++)
                {
                    bool hasBox = !string.IsNullOrEmpty(waitingBoxIds[i]);
                    slots[i].SetState(hasBox, warning && !hasBox);
                }
            }
        }

        public bool TryAdd(string boxId, Vector3? targetPos = null)
        {
            if (string.IsNullOrWhiteSpace(boxId) || IsFull || waitingBoxIds.Contains(boxId))
            {
                m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";
                return false;
            }

            int index = -1;
            if (targetPos.HasValue)
            {
                float minDist = float.MaxValue;
                for (int i = 0; i < capacity; i++)
                {
                    if (string.IsNullOrEmpty(waitingBoxIds[i]))
                    {
                        float dist = (GetWorldSlotPosition(i) - targetPos.Value).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            index = i;
                        }
                    }
                }
            }
            else
            {
                index = waitingBoxIds.FindIndex(string.IsNullOrEmpty);
            }

            if (index >= 0)
            {
                waitingBoxIds[index] = boxId;
                m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";
                return true;
            }

            m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";

            return false;
        }

        public bool IsInActiveZone(string boxId, float minRatio, float maxRatio)
        {
            int index = waitingBoxIds.IndexOf(boxId);
            if (index < 0 || index >= slots.Count) return false;
            
            float totalWidth = capacity * slotSpacing;
            if (totalWidth <= 0f) return true;

            float startX = -((capacity - 1) * slotSpacing * 0.5f) - slotSpacing * 0.5f;
            float localX = slots[index].transform.localPosition.x;
            
            float ratio = (localX - startX) / totalWidth;
            return ratio >= minRatio && ratio <= maxRatio;
        }

        public void Remove(string boxId)
        {
            int index = waitingBoxIds.IndexOf(boxId);
            if (index >= 0)
            {
                waitingBoxIds[index] = null;
            }

            m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";
        }

        public bool Contains(string boxId)
        {
            return waitingBoxIds.Contains(boxId);
        }

        public int IndexOf(string boxId)
        {
            return waitingBoxIds.IndexOf(boxId);
        }

        public bool IsWarningState()
        {
            return warningFreeSlots > 0 && FreeSlots <= warningFreeSlots;
        }

        public Vector3 GetWorldSlotPosition(int occupiedIndex)
        {
            if (occupiedIndex >= 0 && occupiedIndex < slots.Count && slots[occupiedIndex] != null)
            {
                return slots[occupiedIndex].transform.position;
            }
            var root = GetRoot();
            return root.TransformPoint(GetSlotPosition(occupiedIndex, Mathf.Max(1, capacity)));
        }

        public Transform GetSlotTransform(int occupiedIndex)
        {
            if (occupiedIndex >= 0 && occupiedIndex < slots.Count && slots[occupiedIndex] != null)
            {
                return slots[occupiedIndex].transform;
            }
            return GetRoot();
        }

        public IEnumerator PlayCompaction(IReadOnlyDictionary<string, BoxController> waitingBoxes, float duration)
        {
            if (waitingBoxes == null)
            {
                yield break;
            }

            var movers = new List<IEnumerator>();
            for (int i = 0; i < waitingBoxIds.Count; i++)
            {
                if (waitingBoxes.TryGetValue(waitingBoxIds[i], out var box) && box != null)
                {
                    box.transform.SetParent(GetSlotTransform(i), true);
                    movers.Add(box.PlaySlideToLocal(Vector3.zero, duration));
                }
            }

            foreach (var mover in movers)
            {
                yield return mover;
            }
        }

        private void Update()
        {
            if (capacity <= 0 || slots.Count == 0) return;

            currentConveyorOffset += conveyorSpeed * Time.deltaTime;
            
            float totalWidth = (capacity + 0.5f) * (slotSpacing);
            if (totalWidth > 0f)
            {
                currentConveyorOffset %= totalWidth;
            }
            
            if (m_materialConveyorTranslate != null)
            {
                m_materialConveyorTranslate.SetTextureOffset("_BaseMap", new Vector2(0, m_materialConveyorTranslate.GetTextureOffset("_BaseMap").y - m_conveyorTextureScrollSpeed));
            }

            UpdateSlotPositions();
        }

        private void UpdateSlotPositions()
        {
            float totalWidth = (capacity + 0.5f) * slotSpacing;
            float startX = -((capacity - 1) * slotSpacing * 0.5f) - slotSpacing * 0.75f;

            for (int i = 0; i < slots.Count; i++)
            {
                float baseLocalX = i * slotSpacing;
                float currentX = baseLocalX + currentConveyorOffset;
                
                if (totalWidth > 0f)
                {
                    currentX %= totalWidth;
                    if (currentX < 0) currentX += totalWidth;
                }

                float finalX = startX + currentX;
                slots[i].transform.localPosition = new Vector3(finalX, 0f, 0f);

                if (totalWidth > 0f)
                {
                    float ratio = currentX / totalWidth;
                    float scale = 1f;

                    if (ratio < visibleConveyorZone.x)
                    {
                        scale = visibleConveyorZone.x > 0f ? ratio / visibleConveyorZone.x : 1f;
                    }
                    else if (ratio > visibleConveyorZone.y)
                    {
                        scale = visibleConveyorZone.y < 1f ? (1f - ratio) / (1f - visibleConveyorZone.y) : 1f;
                    }

                    slots[i].transform.localScale = new Vector3(scale, scale, scale);
                }
            }
        }

        private void RebuildSlots()
        {
            if (capacity <= 0 || slotPrefab == null)
            {
                return;
            }

            var root = GetRoot();
            
            while (slots.Count < capacity)
            {
                var slot = LeanPool.Spawn(slotPrefab, root);
                slots.Add(slot);
            }
            
            while (slots.Count > capacity)
            {
                var extraSlot = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);
                if (extraSlot != null) LeanPool.Despawn(extraSlot.gameObject);
            }

            var warning = IsWarningState();
            for (int i = 0; i < capacity; i++)
            {
                bool hasBox = !string.IsNullOrEmpty(waitingBoxIds[i]);
                slots[i].SetState(hasBox, warning && !hasBox);
            }
            UpdateSlotPositions();
        }

        private Vector3 GetSlotPosition(int index, int slotCapacity)
        {
            var centeredIndex = index - (slotCapacity - 1) * 0.5f;
            return new Vector3(centeredIndex * slotSpacing, 0f, 0f);
        }

        private Transform GetRoot()
        {
            return slotRoot == null ? transform : slotRoot;
        }

        private void ClearChildren()
        {
            slots.Clear();
            var root = GetRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                LeanPool.Despawn(root.GetChild(i).gameObject);
            }
        }
    }
}
