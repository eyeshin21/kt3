using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRemoveAdsBundle : PanelBase
{
    [SerializeField] private TextMeshProUGUI m_textPrice;
    [SerializeField] private Button m_buttonClose;
    [SerializeField] private Button m_buttonBuy;

    [SerializeField] private RewardItem m_itemCoin;
    [SerializeField] private RewardItem m_itemMagicWand;
    [SerializeField] private RewardItem m_itemBroom;    
    [SerializeField] private RewardItem m_itemMagnet;

    private ShopPack removeAdsBundleRewardConfig => ShopManager.Instance.RemoveAdsRewardConfig;


    private void Awake()
    {
        m_buttonClose.onClick.AddListener(OnClickClose);
        m_buttonBuy.onClick.AddListener(OnClickBuy);

        UpdateUI();
    }

    private void UpdateUI()
    {
        m_textPrice.text = CGTeamBridge.Instance.GetProductPriceStringFromStore(eIAPKey.remove_ads_bundle);
        foreach (var reward in removeAdsBundleRewardConfig.listReward)
        {
            switch (reward.type)
            {
                case UserResourceType.Coin:
                    m_itemCoin.SetData(reward);
                    break;
                case UserResourceType.MagicWand:
                    m_itemMagicWand.SetData(reward);    
                    break;
                case UserResourceType.Broom:
                    m_itemBroom.SetData(reward);
                    break;
                case UserResourceType.Magnet:
                    m_itemMagnet.SetData(reward);
                    break;
            }
        }
    }

    private void OnClickBuy()
    {
        CGTeamBridge.Instance.Purchase(eIAPKey.remove_ads_bundle);
    }

    private void OnPurchaseSuccess()
    {
        // Handle the purchase success, remove ads 

        foreach (var reward in removeAdsBundleRewardConfig.listReward)
        {
            reward.Claim();
        }

        UserManager.Instance.SetPurchasedRemoveAds(true);

        UIManager.Instance.GetPanel<UIHome>().Show();

        UIManager.Instance.GetPanel<UINotification>().Show("Purchased Successfully");
        Hide();
    }

    private void OnClickClose()
    {
        Hide();
    }
}
