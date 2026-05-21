using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIUsingBooster : PanelBase
{
    [SerializeField] private SerializedDictionary<BoosterType, GameObject> m_listIconBooster;
    [SerializeField] private Button m_buttonClose;

    public UnityEvent OnClose = new();

    private void Awake()
    {
        m_buttonClose.onClick.AddListener(OnClickClose);
    }

    private void OnDisable()
    {
        OnClose.RemoveAllListeners();
    }

    public void Show(BoosterType boosterType)
    {
        foreach (var item in m_listIconBooster.Values)
        {
            item.SetActive(false);
        }
        m_listIconBooster[boosterType].SetActive(true);
    }

    private void OnClickClose()
    {
        Hide();
        OnClose?.Invoke();
    }
}
