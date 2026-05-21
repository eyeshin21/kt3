using UnityEngine;

namespace GameTitan
{
    public class PinToSafeArea : MonoBehaviour
    {
        private Rect lastSafeArea;
        private RectTransform parentRectTransform;
        public RectTransform[] rectTransforms;
        public Vector2 widthTest;
        public Vector2 heightTest;

        private void Awake()
        {
            parentRectTransform = this.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safeAreaRect = Screen.safeArea;

            float scaleRatio = parentRectTransform.rect.width / Screen.width;

            Debug.Log(string.Format("Safe xMin {0}, xMax {1}, yMin {2}, yMax {3}, screenwidth {4}, screenwidthHight {5}, scaleRatio {6}", safeAreaRect.xMin, safeAreaRect.xMax, safeAreaRect.yMin, safeAreaRect.yMax
                , Screen.width, Screen.height, scaleRatio));

//#if !UNITY_EDITOR
//            var left = widthTest.x;
//            var right = widthTest.y;
//            var bottom = heightTest.x;
//            var top = heightTest.y;
//#else
            var left = safeAreaRect.xMin * scaleRatio;
            var right = -(Screen.width - safeAreaRect.xMax) * scaleRatio;
            var bottom = safeAreaRect.yMin * scaleRatio;
            var top = -(Screen.height - safeAreaRect.yMax) * scaleRatio;
//#endif

            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];
                rectTransform.offsetMin = new Vector2(left, bottom);
                rectTransform.offsetMax = new Vector2(right, top);
            }


            lastSafeArea = Screen.safeArea;
        }
    }
}