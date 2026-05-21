using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIShop : PanelBase
{
    [SerializeField] private Button m_buttonClose;

    [Header("Configs")]
    [SerializeField] private ShopData m_listCoinPackConfig;
    [SerializeField] private ShopData m_listBundlePackConfig;

    [Header("List Packs")]
    [SerializeField] private List<CoinPack> m_listCoinPack;
    [SerializeField] private List<BundlePack> m_listBundlePack;

    private void Awake()
    {
        m_buttonClose.onClick.AddListener(OnButtonCloseClicked);

        for (int i = 0; i < m_listCoinPack.Count; i++)
        {
            m_listCoinPack[i].SetData(m_listCoinPackConfig.listShopPack[i]);
        }

        for (int i = 0; i < m_listBundlePack.Count; i++)
        {
            m_listBundlePack[i].SetData(m_listBundlePackConfig.listShopPack[i]);
        }
    }

    private void OnButtonCloseClicked()
    {
        Hide();
    }
}
