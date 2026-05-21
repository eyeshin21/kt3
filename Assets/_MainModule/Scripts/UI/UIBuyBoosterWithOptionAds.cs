using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum BoosterType
{
    MAGIC_WAND,
    BROOM,
    MAGNET
}


public class UIBuyBoosterWithOptionAds : PanelBase
{
    [SerializeField] private SerializedDictionary<BoosterType, GameObject> m_listIconBooster;
    [SerializeField] private SerializedDictionary<BoosterType, GameObject> m_listIconBoosterOptionAds;
    [SerializeField] private Button m_buttonBuy;
    [SerializeField] private Button m_buttonClose;
    [SerializeField] private Button m_buttonBuyByAds;
    [SerializeField] private TextMeshProUGUI m_titleBooster1;
    [SerializeField] private TextMeshProUGUI m_titleBooster2;
    [SerializeField] private TextMeshProUGUI m_textQuantityBuyByCoin;
    [SerializeField] private TextMeshProUGUI m_textQuantityBuyByAds;
    [SerializeField] private TextMeshProUGUI m_textPriceCoin;

    private BoosterType currentBoosterType;

    public UnityEvent OnBought = new();

    public UnityEvent OnClose = new();

    public UnityEvent OnBuyByAds = new();

    private void Awake()
    {
        m_buttonBuy.onClick.AddListener(OnClickBuy);
        m_buttonClose.onClick.AddListener(OnClickClose);
        m_buttonBuyByAds.onClick.AddListener(OnClickBuyByAds);
        OnBuyByAds.AddListener(RewardSuccess);
    }

    public void RewardSuccess()
    {
    }

    private void OnClickBuyByAds()
    {
        m_buttonBuyByAds.interactable = false;
        m_buttonBuy.interactable = false;
        Hide();
        CGTeamBridge.Instance.ShowRewarded("BuyBooster", null, OnBuyByAds, null);
    }

    public void Show(BoosterType boosterType)
    {
        base.Show();
        currentBoosterType = boosterType;

        foreach (var item in m_listIconBooster.Values)
        {
            item.SetActive(false);
        }
        m_listIconBooster[boosterType].SetActive(true);

        foreach (var item in m_listIconBoosterOptionAds.Values)
        {
            item.SetActive(false);
        }
        m_listIconBoosterOptionAds[boosterType].SetActive(true);

        //m_titleBooster1.text = CurrentBoosterConfig.name;
        //m_titleBooster2.text = CurrentBoosterConfig .name;

        //m_textQuantityBuyByCoin.text = $"X{CurrentBoosterConfig.quantityBuyByCoin}";
        //m_textQuantityBuyByAds.text = $"X{CurrentBoosterConfig.quantityBuyByAds}";

        //m_textPriceCoin.text = CurrentBoosterConfig.price.ToString();

        m_buttonBuyByAds.interactable = true;
        m_buttonBuy.interactable = true;

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
        //    UIManager.Instance.GetPanel<UINotification>().Show("Not enough coins!");
        //    return;
        //}
            
        Hide();
        m_buttonBuyByAds.interactable = false;
        m_buttonBuy.interactable = false;
        OnBought?.Invoke();
    }
}
