using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonBooster : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private GameObject m_lockObject;
    [SerializeField] private GameObject m_openObject;
    [SerializeField] private Button m_button;
    [SerializeField] private AnimatedButton m_animatedButton;

    [SerializeField] private TextMeshProUGUI m_textLevelUnlock;
    [SerializeField] private TextMeshProUGUI m_textQuantity;

    //private BoosterManager BoosterManager => GameController.Instance.BoosterManager;

    public UnityEvent OnClick { get; set; } = new UnityEvent();

    private void Awake()
    {
        m_button.onClick.AddListener(OnClickBooster);
    }

    private void OnClickBooster()
    {
        OnClick?.Invoke();

        //CGTeamBridge.Instance.TrackUseBooster(boosterType.ToString(), GameController.Instance.CurrentLevel);
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        //var boosterConfig = GameController.Instance.BoosterManager.BoosterConfig.boosterConfigs[boosterType];
        //bool isUnlock = GameController.Instance.CurrentLevel >= boosterConfig.levelUnlock;
        //m_openObject.SetActive(isUnlock);
        //m_lockObject.SetActive(!isUnlock);
        //m_button.interactable = isUnlock;
        //m_animatedButton.interactable = isUnlock;

        //m_textLevelUnlock.text = $"Level {boosterConfig.levelUnlock}";
        //m_textQuantity.text = BoosterManager.GetBoosterCount(boosterType).ToString();
    }
}
