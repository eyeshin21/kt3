using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateScaleBreath : MonoBehaviour
{
    private void OnEnable()
    {
        transform.DOScale(1.1f, 1f)
           .SetEase(Ease.Linear)
           .SetLoops(-1, LoopType.Yoyo)
           .SetUpdate(true);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }

}
