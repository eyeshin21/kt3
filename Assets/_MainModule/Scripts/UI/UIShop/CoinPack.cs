using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinPack : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_textPrice;
    [SerializeField] private TextMeshProUGUI m_textAmount;
    [SerializeField] private Button m_buttonBuy;

    private ShopPack shopPack;    

    private void Awake()
    {
        m_buttonBuy.onClick.AddListener(OnClickBuy);

        m_textPrice.text = CGTeamBridge.Instance.GetProductPriceStringFromStore(shopPack.iapKey);
    }

    public void SetData(ShopPack shopPack)
    {
        this.shopPack = shopPack;
        m_textAmount.text = shopPack.listReward.Find(x => x.type == UserResourceType.Coin).quantity.ToString();
    }

    private void OnClickBuy()
    {
        CGTeamBridge.Instance.Purchase(shopPack.iapKey);
    }
}
