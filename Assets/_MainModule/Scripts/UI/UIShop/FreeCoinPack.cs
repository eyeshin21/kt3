using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FreeCoinPack : MonoBehaviour
{
    [SerializeField] private Button m_buttonGet;
    [SerializeField] private Button m_buttonDisable;
    [SerializeField] private Button m_buttonOutOfTurns;
    [SerializeField] private TextMeshProUGUI m_textCoinQuantity;
    [SerializeField] private TextMeshProUGUI m_textCoolDown;

    public Action<int> OnGotCoin { get; set; }

    private int coinQuantity = 20;

    private int intervalReceiveFreeCoin = 21600;

    private int maxTimeReceiveFreeCoin = 2;

    private double timeCoolDown;

    private UnityEvent OnBuyByAds = new();

    private DateTime LastTimeReceiveFreeCoin
    {
        get
        {
            return new DateTime(long.Parse(PlayerPrefs.GetString("LastTimeReceiveFreeCoin", "0")));
        }
        set
        {
            PlayerPrefs.SetString("LastTimeReceiveFreeCoin", new DateTimeOffset(value).Ticks.ToString());
        }
    }

    private int CurrentTimeReceiveFreeCoin
    {
        get => PlayerPrefs.GetInt("CurrentTimeReceiveFreeCoin", 0);
        set => PlayerPrefs.SetInt("CurrentTimeReceiveFreeCoin", value);
    }

    public int GetCurrentLastTimeReceiveFreeCoin() => PlayerPrefs.GetInt("LastTimeReceiveFreeCoin", 0);

    public void SetCurrentTimeReceiveFreeCoin(int value)
    {
        PlayerPrefs.SetInt("CurrentTimeReceiveFreeCoin", value);
    }

    public int GetCurrentTimeReceiveFreeCoin() => PlayerPrefs.GetInt("CurrentTimeReceiveFreeCoin", 0);

    private void Awake()
    {
        m_buttonGet.onClick.AddListener(OnClickGet);
        m_buttonDisable.onClick.AddListener(OnClickDisable);
        m_buttonOutOfTurns.onClick.AddListener(OnClickOutOfTurn);

        m_textCoinQuantity.text = coinQuantity.ToString();

        OnBuyByAds.AddListener(OnClaimCoin);
    }

    private void OnClickOutOfTurn()
    {
        //UIManager.Instance.ShowPopup<PopupMiniNoti>(null).Show("Out of turns");
    }

    private void OnClickDisable()
    {
        //UIManager.Instance.ShowPopup<PopupMiniNoti>(null).Show($"Next free in: {MyUlti.Int2TimeString((int)timeCoolDown)}");
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void OnClickGet()
    {
        CGTeamBridge.Instance.ShowRewarded("FreeCoinInShop", null, OnBuyByAds, null);
    }

    private void OnClaimCoin()
    {
        UserManager.Instance.AddCoins(coinQuantity);
        OnGotCoin?.Invoke(coinQuantity);
        LastTimeReceiveFreeCoin = DateTime.Now;
        CurrentTimeReceiveFreeCoin++;
        UpdateUI();
        
    }

    public void UpdateUI()
    {
        if (LastTimeReceiveFreeCoin.Date != DateTime.Now.Date && CurrentTimeReceiveFreeCoin >= maxTimeReceiveFreeCoin)
        {
            CurrentTimeReceiveFreeCoin = 0;
        }

        if (CurrentTimeReceiveFreeCoin >= maxTimeReceiveFreeCoin)
        {
            timeCoolDown = (LastTimeReceiveFreeCoin.AddSeconds(intervalReceiveFreeCoin) - DateTime.Now).TotalSeconds;
        }

        m_buttonGet.gameObject.SetActive(timeCoolDown <= 0 && CurrentTimeReceiveFreeCoin < maxTimeReceiveFreeCoin);
        m_buttonDisable.gameObject.SetActive(CurrentTimeReceiveFreeCoin >= maxTimeReceiveFreeCoin);

        StartCoolDownTime();
    }

    private void StartCoolDownTime()
    {
        if (timeCoolDown > 0)
        {
            StartCoroutine(IEStartCoolDown());
        }
    }

    private IEnumerator IEStartCoolDown()
    {

        while (timeCoolDown > 0)
        {
            timeCoolDown = (LastTimeReceiveFreeCoin.AddSeconds(intervalReceiveFreeCoin) - DateTime.Now).TotalSeconds;
            m_textCoolDown.text = $"{ConvertToStringDisplay((int)timeCoolDown)}";

            yield return new WaitForSeconds(1f);
        }

        UpdateUI();

    }

    private object ConvertToStringDisplay(int timeCoolDown)
    {
        string re = "";
        int h = timeCoolDown / 3600;
        int m = timeCoolDown % 3600;
        int s = m % 60;
        m = m / 60;
        re = string.Format("{0}:{1:d2}:{2:d2}", h, m, s);

        return re;
    }
}
