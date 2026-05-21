using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinBar : CurrencyBar
{
    [SerializeField] Button btn;
    public Button BtnBuy => btn;
    [SerializeField] Image _iconPlus;

    public bool enableAutoSync;

    private void Awake()
    {
        if (btn != null)
        {
            btn.onClick.AddListener(delegate
            {
                UIManager.Instance.GetPanel<UIShop>().Show();
            });
        }
    }
    private void Start()
    {
        SetNumberStart(UserManager.Instance.Coins);
    }

    private void OnEnable()
    {
        UserManager.Instance.OnResourcesChanged += UpdateCoinValue;

        SetNumberStart(UserManager.Instance.Coins);
    }

    private void OnDisable()
    {
        if(UserManager.Instance != null)
        UserManager.Instance.OnResourcesChanged -= UpdateCoinValue;
    }

    public void UpdateCoinValue()
    {
        if (!enableAutoSync)
        {
            return;
        }


        SetTextNumberAnim(UserManager.Instance.Coins);
    }
}
