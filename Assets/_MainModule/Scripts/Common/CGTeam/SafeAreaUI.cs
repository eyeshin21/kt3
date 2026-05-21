using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaUI : MonoBehaviour
{
    RectTransform rectTransform;

    [SerializeField] bool isSafeBottom;
    [SerializeField] bool isSafeTop;

    // Start is called before the first frame update
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (isSafeBottom)
        {
            rectTransform.anchorMin = ScreenExtension.Instance.minAnchor;
        }

        if (isSafeTop)
        {
            rectTransform.anchorMax = ScreenExtension.Instance.maxAnchor;
        }
    }
}
