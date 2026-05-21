using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettingGamePlay : PanelBase
{
    [SerializeField] ButtonSetting m_buttonSound;
    [SerializeField] ButtonSetting m_buttonMusic;
    [SerializeField] ButtonSetting m_buttonVibration;
    [SerializeField] private Button m_buttonClose;
    [SerializeField] private Button m_buttonHome;


    private int countCheat = 0;

    private void Awake()
    {
        m_buttonSound.OnClick.AddListener(OnClickSound);
        m_buttonMusic.OnClick.AddListener(OnClickMusic);
        m_buttonVibration.OnClick.AddListener(OnClickVibration);

        m_buttonHome.onClick.AddListener(OnClickHome);
        m_buttonClose.onClick.AddListener(OnClickClose);


        UpdateUI();
    }

    private void OnClickVibration()
    {
        HapticFeedbackManager.Instance.Vibrate = !HapticFeedbackManager.Instance.Vibrate;
        UpdateUI();
    }

    private void OnClickMusic()
    {
        AudioManager.Instance.IsMusicEnabled = !AudioManager.Instance.IsMusicEnabled;
        UpdateUI();
    }

    private void OnClickSound()
    {
        AudioManager.Instance.IsSoundEnabled = !AudioManager.Instance.IsSoundEnabled;
        UpdateUI();
    }

    private void UpdateUI()
    {
        m_buttonMusic.SetActive(AudioManager.Instance.IsMusicEnabled);
        m_buttonSound.SetActive(AudioManager.Instance.IsSoundEnabled);
        m_buttonVibration.SetActive(HapticFeedbackManager.Instance.Vibrate);
    }    

    private void OnClickClose()
    {
        //GameController.Instance.ResumeGame();
        Hide();
    }

    private void OnClickHome()
    {
        //GameController.Instance.EndGame();
        UIManager.Instance.GetPanel<UIHome>().Show();
        UIManager.Instance.GetPanel<UIGamePlay>().Hide();
        Hide();

        //CGTeamBridge.Instance.OnGameAbandoned(GameController.Instance.CurrentLevel, GameController.Instance.GamePlayBoard.GetCurrenPictureProgress());
    }
}
