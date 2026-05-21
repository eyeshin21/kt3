using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public enum eIAPKey
{
    remove_ads_bundle,
    coin_pack_1,
    coin_pack_2,
    coin_pack_3,
    coin_pack_4,
    coin_pack_5,
    coin_pack_6
}

[DefaultExecutionOrder(-110)]
public class IAPHandlePurchase : SingletonDontDestroyMono<IAPHandlePurchase>
{
    public void BuyRemoveAdsBundle()
    {
        ShopManager.Instance.BuySucessRemoveAdsBundle();
    }

    public void BuyCoin1()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_1);
    }

    public void BuyCoin2()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_2);
    }


    public void BuyCoin3()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_3);
    }

    public void BuyCoin4()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_4);
    }

    public void BuyCoin5()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_5);
    }

    public void BuyCoin6()
    {
        ShopManager.Instance.BusSuccessPack(eIAPKey.coin_pack_6);
    }
}
