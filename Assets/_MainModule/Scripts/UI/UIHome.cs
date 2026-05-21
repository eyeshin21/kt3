using HexaFall.Gameplay.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHome : PanelBase
{
    [SerializeField] private Button m_buttonPlay;
    [SerializeField] private Button m_buttonSetting;
    [SerializeField] private Button m_buttonShop;
    [SerializeField] private Button m_buttonRemoveAdsBundle;
    [SerializeField] private List<TextMeshProUGUI> m_listLevelText;

    private void Awake()
    {
        m_buttonPlay.onClick.AddListener(OnClickPlay);
        m_buttonSetting.onClick.AddListener(OnClickSetting);
        m_buttonShop.onClick.AddListener(OnClickShop);
        m_buttonRemoveAdsBundle.onClick.AddListener(OnClickRemoveAdsBundle);

        CGTeamBridge.Instance?.ShowBanner();
    }

    private void OnClickRemoveAdsBundle()
    {
        UIManager.Instance.GetPanel<UIRemoveAdsBundle>().Show();
    }

    private void OnClickShop()
    {
        UIManager.Instance.GetPanel<UIShop>().Show();
    }

    private void OnClickSetting()
    {
        UIManager.Instance.GetPanel<UISettingHome>().Show();    
    }

    private void OnClickPlay()
    {
        Hide();
        UIManager.Instance.GetPanel<UIGamePlay>().Show();

        GameController.Instance.StartCurrentLevel();
    }

    private void OnEnable()
    {
        UserManager.Instance.OnLevelChanged += OnLevelChanged;
    }

    private void OnDisable()
    {
        if (UserManager.Instance == null)
        {
            return;
        }

        UserManager.Instance.OnLevelChanged -= OnLevelChanged;
    }

    public override void Show()
    {
        base.Show();
        
        for(int i=0; i<m_listLevelText.Count; i++)
        {
            m_listLevelText[i].text = $"{GameController.Instance.CurrentLevel + i}";
        }

        m_buttonRemoveAdsBundle.gameObject.SetActive(!UserManager.Instance.IsPurchasedRemoveAds);
    }

    override public void Hide()
    {
        base.Hide();
        gameObject.SetActive(false);
    }

    private void OnLevelChanged(int previousLevel, int currentLevel)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
    }
}
