using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AnimateImageAlphaBreath : MonoBehaviour
{
    [SerializeField] private Image m_image;
    private void OnEnable()
    {
        m_image.DOFade(0f, 0f);
        m_image.DOFade(1f, 1f)
           .SetEase(Ease.Linear)
           .SetLoops(-1, LoopType.Yoyo)
           .SetUpdate(true);
    }

    private void OnDisable()
    {
        m_image.DOKill();
    }
}
