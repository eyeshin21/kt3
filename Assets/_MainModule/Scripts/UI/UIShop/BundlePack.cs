using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BundlePack : MonoBehaviour
{
    [SerializeField] private RewardItem m_itemGold;
    [SerializeField] private RewardItem m_itemMagicWand;
    [SerializeField] private RewardItem m_itemMagnet;
    [SerializeField] private RewardItem m_itemBroom;
    [SerializeField] private RewardItem m_itemHeart;
    [SerializeField] private Button m_buttonBuy;
    [SerializeField] private TextMeshProUGUI m_textTitle;
    [SerializeField] private TextMeshProUGUI m_textPrice;

    private ShopPack shopPack;  

    public UnityEvent OnBuySuccess = new UnityEvent();

    private void Awake()
    {
        m_buttonBuy.onClick.AddListener(OnClickBuy);

        m_textPrice.text = CGTeamBridge.Instance.GetProductPriceStringFromStore(shopPack.iapKey);
    }

    private void OnClickBuy()
    {
        CGTeamBridge.Instance.Purchase(shopPack.iapKey);
    }

    private void OnPurchaseSuccess()
    {
        foreach (var item in shopPack.listReward)
        {
            item.Claim();
        }
        OnBuySuccess?.Invoke();
    }

    internal void SetData(ShopPack shopPack)
    {
        this.shopPack = shopPack;

        m_textTitle.text = shopPack.title;
        foreach (var item in shopPack.listReward)
        {
            switch (item.type)
            {
                case UserResourceType.Coin:
                    m_itemGold.SetData(item);
                    break;
                case UserResourceType.MagicWand:
                    m_itemMagicWand.SetData(item);
                    break;
                case UserResourceType.Magnet:
                    m_itemMagnet.SetData(item);
                    break;
                case UserResourceType.Broom:
                    m_itemBroom.SetData(item);
                    break;
                case UserResourceType.Heart:
                    m_itemHeart.SetData(item);
                    break;
            }
        }

    }
}
