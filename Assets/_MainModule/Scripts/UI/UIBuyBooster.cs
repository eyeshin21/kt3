using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIBuyBooster : PanelBase
{
    [SerializeField] private SerializedDictionary<BoosterType, GameObject> m_listIconBooster;
    [SerializeField] private Button m_buttonBuy;
    [SerializeField] private Button m_buttonClose;
    [SerializeField] private Button m_buttonClaim;
    [SerializeField] private TextMeshProUGUI m_titleUnlock;
    [SerializeField] private TextMeshProUGUI m_titleBuy;
    [SerializeField] private TextMeshProUGUI m_textPriceCoin;
    [SerializeField] private TextMeshProUGUI m_textDes;
    [SerializeField] private TextMeshProUGUI m_textName;

    private BoosterType currentBoosterType;
    //private BoosterManager BoosterManager => GameController.Instance.BoosterManager;

    //private BoosterConfigData CurrentBoosterConfig => BoosterManager.BoosterConfig.boosterConfigs[currentBoosterType];

    public UnityEvent OnBought = new();

    public UnityEvent OnClaimed = new();

    public UnityEvent OnClose = new();

    private void Awake()
    {
        m_buttonBuy.onClick.AddListener(OnClickBuy);
        m_buttonClose.onClick.AddListener(OnClickClose);
        m_buttonClaim.onClick.AddListener(OnClickClaim);
    }

    private void OnClickClaim()
    {
        m_buttonClaim.interactable = false;
        //GameController.Instance.BoosterManager.InitBonus(currentBoosterType);
        Hide();
        OnClaimed?.Invoke();
    }

    public void Show(BoosterType boosterType, bool isBoosterUnlock = false)
    {
        base.Show();

        currentBoosterType = boosterType;

        foreach (var item in m_listIconBooster.Values)
        {
            item.SetActive(false);
        }
        m_listIconBooster[boosterType].SetActive(true);

        m_buttonBuy.gameObject.SetActive(!isBoosterUnlock);
        m_buttonClaim.gameObject.SetActive(isBoosterUnlock);
        m_buttonClaim.interactable = true;

        m_titleUnlock.gameObject.SetActive(isBoosterUnlock);
        m_titleBuy.gameObject.SetActive(!isBoosterUnlock);
        m_buttonClose.gameObject.SetActive(!isBoosterUnlock);

        //m_textPriceCoin.text = CurrentBoosterConfig.price.ToString();

        //m_textDes.text = CurrentBoosterConfig.des;

        //m_textName.text = CurrentBoosterConfig.name;

    }    

    private void OnClickClose()
    {
        Hide();
        OnClose?.Invoke();
    }

    private void OnClickBuy()
    {
        //bool boughtSuccessfully = GameController.Instance.BoosterManager.BuyBooster(currentBoosterType);
        //if (!boughtSuccessfully)
        //{
        //    return;
        //}

        Hide();
        OnBought?.Invoke();
    }
}
