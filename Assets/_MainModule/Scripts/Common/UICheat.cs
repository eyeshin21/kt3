using HexaFall.Gameplay.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICheat : PanelBase
{
    [SerializeField] private string m_password; 
    [SerializeField] private TMP_InputField m_inputLevel;
    [SerializeField] private TMP_InputField m_inputTime;
    [SerializeField] private TMP_InputField m_inputIndexLevel;
    [SerializeField] private TMP_InputField m_inputPassword;

    [SerializeField] private Button m_buttonHideUI;
    [SerializeField] private Button m_buttonCheatLevel;
    [SerializeField] private Button m_buttonCheatCoin;
    [SerializeField] private Button m_buttonShowDebug;
    [SerializeField] private Button m_buttonClose;

    private void Awake()
    {
        m_inputLevel.onSubmit.AddListener(OnEnterLevel);
        m_inputTime.onSubmit.AddListener(OnEnterTime);
        m_inputIndexLevel.onSubmit.AddListener(OnEnterIndexLevel);
        //m_inputIndexLevel.text = GameController.Instance.IndexLevel.ToString();

        m_buttonCheatLevel.onClick.AddListener(OnCheatLevel);
        m_buttonHideUI.onClick.AddListener(OnHideUI);
        m_buttonCheatCoin.onClick.AddListener(OnCheatCoin);
        m_buttonShowDebug.onClick.AddListener(OnClickShowDebug);
        m_buttonClose.onClick.AddListener(OnClickClose);
        
    }

    private void OnClickClose()
    {
        Hide();
    }

    private void OnClickShowDebug()
    {
        if (m_inputPassword.text != m_password) return;
        var reporter = FindObjectOfType<Reporter>(true);
        reporter.gameObject.SetActive(!reporter.gameObject.activeInHierarchy);
    }

    private void OnCheatCoin()
    {
        if (m_inputPassword.text != m_password) return;
        UserManager.Instance.AddCoins(100000);
    }

    private void OnHideUI()
    {
        if (m_inputPassword.text != m_password) return;
        UIManager.Instance.GetPanel<UIGamePlay>().HideUI();
    }

    private void OnCheatLevel()
    {
        if (m_inputPassword.text != m_password) return;
        GameController.Instance.IndexLevel = int.Parse(m_inputIndexLevel.text);
        int level = int.Parse(m_inputLevel.text);
        GameController.Instance.CurrentLevelData = null;
        GameController.Instance.CurrentLevel = level;
        GameController.Instance.StartCurrentLevel();
        UIManager.Instance.GetPanel<UISettingGamePlay>().Hide();
        Show();
    }

    private void OnEnterIndexLevel(string arg0)
    {
        if (m_inputPassword.text != m_password) return;
        //GameController.Instance.IndexLevel = int.Parse(arg0);
    }

    private void OnEnterTime(string arg0)
    {
        if (m_inputPassword.text != m_password) return;
        //GameController.Instance.CurrentRemainingTime = float.Parse(arg0);
        UIManager.Instance.GetPanel<UISettingGamePlay>().Hide();
    }

    private void OnEnterLevel(string arg0)
    {
        if (m_inputPassword.text != m_password) return;
        int level = int.Parse(arg0);
        //GameController.Instance.CurrentLevel = level;
        //GameController.Instance.StartCurrentLevel();
        UIManager.Instance.GetPanel<UISettingGamePlay>().Hide();
    }
}
