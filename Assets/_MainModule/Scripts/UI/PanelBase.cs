using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class PanelBase : MonoBehaviour, IUIHandle
{
    [SerializeField] private Image m_background;
    [SerializeField] private Transform m_board;

    public virtual void Hide()
    {
        //gameObject.SetActive(false);
        if (m_background != null)
        {
            m_background.DOFade(0, 0.1f).SetEase(Ease.Linear);
        }

        if (m_board != null)
        {
            m_board.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
             gameObject.SetActive(false);
        }
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        transform.parent.SetAsLastSibling();
        if (m_background != null)
        {
            m_background.color = new Color(0, 0, 0, 0);
            m_background.DOFade(0.95f, 0.25f).SetEase(Ease.Linear);
        }

        if (m_board != null)
        {
            m_board.localScale = Vector3.zero;
            m_board.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }
}
