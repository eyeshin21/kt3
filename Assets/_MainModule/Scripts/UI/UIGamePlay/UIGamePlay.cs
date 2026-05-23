using HexaFall.Gameplay.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGamePlay : PanelBase
{
    [SerializeField] private TextMeshProUGUI m_textLevel;

    [SerializeField] private Button m_buttonReplay;
    [SerializeField] private Button m_buttonSetting;

    [SerializeField] private CanvasGroup m_canvasGroup;

    [Header("Button Boosters")]
    [SerializeField] private ButtonBooster m_buttonAddSlot;
    [SerializeField] private ButtonBooster m_buttonSuperPicker;
    [SerializeField] private ButtonBooster m_buttonShuffle;


    [SerializeField] private TextMeshProUGUI m_textTime;

    [SerializeField] private TextMeshProUGUI m_textQuantityMagicWand;
    [SerializeField] private TextMeshProUGUI m_textQuantityBroom;
    [SerializeField] private TextMeshProUGUI m_textQuantityMagnet;

    [SerializeField] private Button m_buttonEnableCheat;
    
    private int countCheat;

    private void Awake()
    {
        m_buttonReplay.onClick.AddListener(OnClickReplay);
        m_buttonSetting.onClick.AddListener(OnClickSetting);
        m_buttonAddSlot.OnClick.AddListener(OnClickAddSlot);
        m_buttonSuperPicker.OnClick.AddListener(OnClickSuperPicker);
        m_buttonShuffle.OnClick.AddListener(OnClickShuffle);

        m_buttonEnableCheat.onClick.AddListener(CountEnableCheat);
    }

    private void CountEnableCheat()
    {
        if (countCheat >= 5)
        {
            UIManager.Instance.GetPanel<UICheat>().Show();
            countCheat = 0;
        }
        countCheat++;
    }

    private void OnEnable()
    {
        UserManager.Instance.OnResourcesChanged += UpdateUI;
        UserManager.Instance.OnLevelChanged += OnLevelChanged;
    }

    private void OnDisable()
    {
        if (UserManager.Instance == null)
        {
            return;
        }

        UserManager.Instance.OnResourcesChanged -= UpdateUI;
        UserManager.Instance.OnLevelChanged -= OnLevelChanged;
    }

    public override void Show()
    {
        base.Show();
        UpdateUI();

    }

    public void UpdateUI()
    {
        m_textLevel.text = $"Level {GameController.Instance.CurrentLevel}";
        //m_textQuantityMagicWand.text = GameController.I   nstance.BoosterManager.GetMagicWandCount().ToString();
        //m_textQuantityBroom.text = GameController.Instance.BoosterManager.GetBroomCount().ToString();
        //m_textQuantityMagnet.text = GameController.Instance.BoosterManager.GetMagnetCount().ToString();

        m_buttonAddSlot.UpdateUI();
        m_buttonSuperPicker.UpdateUI();
        m_buttonShuffle.UpdateUI();
    }

    private void Update()
    {
        //m_textTime.text = TimeSpan.FromSeconds(GameController.Instance.CurrentRemainingTime).ToString(@"mm\:ss");
    }

    private void OnClickShuffle()
    {
        GameController.Instance.BoosterManager.TryActivate(HexaFall.Gameplay.Booster.BoosterType.Shuffle);
        UpdateUI();
    }

    private void OnClickSuperPicker()
    {
        GameController.Instance.BoosterManager.TryActivate(HexaFall.Gameplay.Booster.BoosterType.SuperPickerBox);
        UpdateUI();
    }

    private void OnClickAddSlot()
    {
        GameController.Instance.BoosterManager.TryActivate(HexaFall.Gameplay.Booster.BoosterType.AddSlot);
        UpdateUI();
    }

    private void OnClickSetting()
    {
        GameController.Instance.PauseGame();
    }


    private void OnClickReplay()
    {
        GameController.Instance.RestartLevel();

        //UIManager.Instance.GetPanel<UIHome>().Show();
        //Hide();
    }

    private void OnLevelChanged(int previousLevel, int currentLevel)
    {
        UpdateUI();
    }

    public void SetLockSettingAndReplay(bool isLock)
    {
        m_buttonReplay.interactable = !isLock;
        m_buttonSetting.interactable = !isLock;
    }
        
    public void HideUI()
    {
        m_canvasGroup.alpha = MathF.Abs( m_canvasGroup.alpha -1);
    }

}
