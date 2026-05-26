using UnityEngine;
using System.Collections;
using DG.Tweening;
using TMPro;

namespace HexaFall.Gameplay.CoreController
{
    public sealed partial class WaitingAreaController
    {
        [SerializeField] private TextMeshPro m_textAdd1;    
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

        public IEnumerator PlayAddSlotAnimation(Transform buttonTransform)
        {
            if (capacity >= maxCapacity) yield break;

            if (buttonTransform != null && m_textSlotOccupied != null)
            {
                var canvas = buttonTransform.GetComponentInParent<Canvas>();
                Camera uiCam = canvas != null ? canvas.worldCamera : null;
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, buttonTransform.position);
                
                GameObject textObj = m_textAdd1.gameObject;
                textObj.SetActive(true);
                var tmp = m_textAdd1;
                
                Vector3 startWorld = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 5f));
                textObj.transform.position = startWorld;
                
                Sequence seq = DOTween.Sequence();
                seq.Append(textObj.transform.DOMove(m_textSlotOccupied.transform.position, 0.5f).SetEase(Ease.InBack));
                //seq.Join(textObj.transform.DOScale(Vector3.one * 0.5f, 0.5f).SetEase(Ease.InQuad));
                seq.AppendCallback(() => textObj.SetActive(false));
                
                yield return seq.WaitForCompletion();
                m_textSlotOccupied.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 1f);
            }

            int oldCapacity = capacity;
            capacity++;
            waitingBoxIds.Add(null);
            RebuildSlots();
            m_textSlotOccupied.text = $"{Capacity - FreeSlots}/{Capacity}";

            displayedCapacity = oldCapacity;
            yield return DOTween.To(() => displayedCapacity, x => displayedCapacity = x, capacity, 0.6f)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }
    }
}
