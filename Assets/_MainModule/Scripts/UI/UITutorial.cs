using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UITutorial : PanelBase
{
    [SerializeField] private GameObject m_handObject;
    [SerializeField] private TextMeshProUGUI m_textTut;
    [SerializeField] private GameObject m_backgroundBlack;
    [SerializeField] private GameObject m_textBox;

    [SerializeField] private ButtonBooster m_buttonMagicWand;
    [SerializeField] private ButtonBooster m_buttonBroom;
    [SerializeField] private ButtonBooster m_buttonMagnet;

    [SerializeField] private GameObject m_highlightRegionPickLevel11;
    [SerializeField] private GameObject m_highlightRegionPickLevel12;
    [SerializeField] private GameObject m_highlightTray;
    [SerializeField] private GameObject m_highlightTrayTutExpandTray;

    Tween tweenTutDrag;

    public UnityEvent OnClickTut { get; set; } = new UnityEvent();

    private void Awake()
    {
        m_buttonMagicWand.OnClick.AddListener(OnClickMagicWand);
        m_buttonBroom.OnClick.AddListener(OnClickBroom);
        m_buttonMagnet.OnClick.AddListener(OnClickMagnet);
    }

    public override void Show()
    {
        base.Show();
        m_buttonMagicWand.gameObject.SetActive(false);
        m_buttonBroom.gameObject.SetActive(false);
        m_buttonMagnet.gameObject.SetActive(false);

        ShowHighlightRegionPickLevel11(false);
        ShowHighlightRegionPickLevel12(false);
        ShowHighlightTrayTutLevel1(false);
        ShowHighlightTrayTutExpandTray(false);
    }

    private void OnClickMagnet()
    {
        OnClickTut?.Invoke();
    }

    private void OnClickBroom()
    {
        OnClickTut?.Invoke();
    }

    private void OnClickMagicWand()
    {
        OnClickTut?.Invoke();
    }

    private void OnDisable()
    {
        HideHand();
        HideTutorialClickBooster();
        OnClickTut?.RemoveAllListeners();
    }

    public void ShowText(string content)
    {
        EnableTextBox(true);
        m_textTut.text = content;
    }

    public void ShowHandClick(Vector3 position)
    {
        m_handObject.gameObject.SetActive(true);
        var screenPoint = Camera.main.WorldToScreenPoint(position);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(GetComponent<RectTransform>(), screenPoint, Camera.main, out var worldPoint);
        m_handObject.transform.position = worldPoint;
    }
    

    public void ShowHandDrag()
    {
        m_handObject.gameObject.SetActive(true);
        m_handObject.transform.localPosition = Vector3.zero;

        if(tweenTutDrag != null)
        {
            tweenTutDrag.Kill();
        }

        tweenTutDrag = m_handObject.transform.DOLocalMove(Vector2.one * 50f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    public void ShowTutorialClickBoosterMagicWand()
    {
        ShowHandClick(m_buttonMagicWand.transform.position);
        m_buttonMagicWand.gameObject.SetActive(true);
        m_buttonBroom.gameObject.SetActive(false);
        m_buttonMagnet.gameObject.SetActive(false);
        EnableBackground(true);
    }

    public void ShowTutorialClickBoosterBroom()
    {
        ShowHandClick(m_buttonBroom.transform.position);
        m_buttonMagicWand.gameObject.SetActive(false);
        m_buttonBroom.gameObject.SetActive(true);
        m_buttonMagnet.gameObject.SetActive(false);
        EnableBackground(true);
    }

    public void ShowTutorialClickBoosterMagnet()
    {
        ShowHandClick(m_buttonMagnet.transform.position);
        m_buttonMagicWand.gameObject.SetActive(false);
        m_buttonBroom.gameObject.SetActive(false);
        m_buttonMagnet.gameObject.SetActive(true);
        EnableBackground(true);
    }

    public void HideTutorialClickBooster()
    {
        m_buttonMagicWand.gameObject.SetActive(false);
        m_buttonBroom.gameObject.SetActive(false);
        m_buttonMagnet.gameObject.SetActive(false);
        EnableBackground(false);
    }

    public void HideHand()
    {
        if (tweenTutDrag != null)
        {
            tweenTutDrag.Kill();
            tweenTutDrag = null;
        }
        m_handObject.transform.localPosition = Vector3.zero;
        m_handObject.gameObject.SetActive(false);
    }

    public void EnableBackground(bool active)
    {
        m_backgroundBlack.gameObject.SetActive(active);
    }    

    public void EnableTextBox(bool active)
    {
        m_textBox.gameObject.SetActive(active);
    }

    public void ShowHighlightRegionPickLevel11(bool active)
    {
        m_highlightRegionPickLevel11.SetActive(active);
    }

    public void ShowHighlightRegionPickLevel12(bool active)
    {
        m_highlightRegionPickLevel12.SetActive(active);
    }

    public void ShowHighlightTrayTutLevel1(bool active)
    {
        m_highlightTray.SetActive(active);
    }

    public void ShowHighlightTrayTutExpandTray(bool active)
    {
        m_highlightTrayTutExpandTray.SetActive(active);
    }
}
