using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : SingletonDontDestroyMono<ShopManager>
{
    [Header("Configs")]
    [SerializeField] private ShopData m_listCoinPackConfig;
    [SerializeField] private ShopData m_listBundlePackConfig;
    [SerializeField] private ShopPack m_removeAdsBundleRewardConfig;

    public ShopPack RemoveAdsRewardConfig => m_removeAdsBundleRewardConfig;

    public void BuySucessRemoveAdsBundle()
    {
        foreach (var reward in m_removeAdsBundleRewardConfig.listReward)
        {
            reward.Claim();
        }

        UserManager.Instance.SetPurchasedRemoveAds(true);

        if (UIManager.Instance != null)
        {
            //if (GameController.Instance != null && GameController.Instance.CurrentState != GameState.PLAYING)
            //{
            //    UIManager.Instance.GetPanel<UIHome>().Show();
            //}

            UIManager.Instance.GetPanel<UINotification>().Show("Purchased Successfully");
            UIManager.Instance.GetPanel<UINotification>().Hide();
        }
    }

    public void BusSuccessPack(eIAPKey eIAPKey)
    {
        var pack = m_listCoinPackConfig.listShopPack.Find(x => x.iapKey == eIAPKey);

        if (pack == null)
        {
            pack = m_listBundlePackConfig.listShopPack.Find(x => x.iapKey == eIAPKey);
        }

        if (pack != null)
        {
            foreach (var item in pack.listReward)
            {
                item.Claim();
            }
        }
        else
        {
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.GetPanel<UINotification>().Show("Purchased Successfully");
        }

    }
}
