using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)]
public class CGTeamBridge : SingletonDontDestroyMono<CGTeamBridge>
{
    public int RetendDay
    {
        get
        {
            return PlayerPrefs.GetInt("retendDay", 0);
        }
        set
        {
            PlayerPrefs.SetInt("retendDay", value);
        }
    }

    public int DayPlayed
    {
        get
        {
            return PlayerPrefs.GetInt("dayPlayed", 0);
        }
        set
        {
            PlayerPrefs.SetInt("dayPlayed", value);
        }
    }

    public int DailyLogin
    {
        get
        {
            return dailyLogin;
        }
        set
        {
            dailyLogin = value;
            PlayerPrefs.SetInt("dailyLogin", value);
        }
    }

    private int dailyLogin;
    private bool canShowDailyBonus;

    private void Awake()
    {

    }

    private void Start()
    {
        CheckDailyLogin();
    }

    public bool HasInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }
        return true;
    }

    #region ADS
    public void ShowMaxDebugger()
    {

    }

    public void ShowBanner()
    {

    }

    public void HideBanner()
    {

    }

    public void ShowInterstitial(string placement, UnityEvent onClose)
    {
        onClose?.Invoke();
    }

    public void ShowRewarded(string placement, UnityEvent onStart, UnityEvent onCompleted, UnityEvent onFailed)
    {
        onCompleted?.Invoke();
    }

    public bool IsRewardReady()
    {
        return true;
    }
    #endregion

    #region Analytics
    void LogEvent(string eventName, Dictionary<string, object> parameters)
    {
        Debug.Log("Firebase Analytics: " + eventName);
    }

    public void TrackCustomEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (parameters != null)
        {
            foreach (KeyValuePair<string, object> kv in parameters)
            {
                Debug.Log(string.Format("FirebaseAnalytics: {0} - Parameters: {1} -- {2}", eventName, kv.Key, kv.Value));
            }
        }
        LogEvent(eventName, parameters);
    }

    public void TrackTutAction(string action_name)
    {
        var parameters = new Dictionary<string, object>
            {
                {"action_name", action_name}
            };
        TrackCustomEvent("tut_action", parameters);
    }

    /// <summary>
    /// Log khi vừa bắt đầu level
    /// </summary>
    /// <param name="level"></param> level đang chơi
    public void OnGameStarted(int level, string difficult)
    {

    }

    /// <summary>
    /// Log khi kết thúc game. Bao gồm: win, lose
    /// </summary>
    /// <param name="levelComplete"></param> trạng thái game thắng hay thua
    /// <param name="mission_progress"></param> điểm của level. Số khay còn lại trong level. nếu win thì là 1, nếu thua thì thua khi giải được bao nhiêu phần của level
    /// <param name="level"></param> level đang chơi
    /// <param name="coinEarn"></param> số coin kiếm được
    public void OnGameFinished(bool levelComplete, float mission_progress, int level, int coinEarn = 0)
    {

    }

    /// <summary>
    /// Log khi user thoát ra home hoặc ấn replay lại lúc đang chơi
    /// </summary>
    /// <param name="level"></param> level đang chơi
    /// <param name="mission_progress"></param> điểm của level
    public void OnGameAbandoned(int level, float mission_progress)
    {

    }

    /// <summary>
    /// Log khi xảy ra các sự kiện trong game. Hiện tại chỉ cần log khi hiện revive và sau khi revive là được
    /// </summary>
    /// <param name="level"></param> level đang chơi
    /// <param name="mission_progress"></param> điểm của level
    /// <param name="stepName"></param> Nếu hiện revive thì stepName = "soft_fail", còn nếu sau khi revive thì stepName = "revive"
    public void OnGameStep(int level, float mission_progress, string stepName)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param> level đang chơi
    /// <param name="item_type"></param> Coin = soft, Gem = hard, Booster = power_up
    /// <param name="name"></param> coin, gem, tên các booster,...
    /// <param name="amount"></param> Số lượng tiêu
    /// <param name="reason"></param> Lý do tiêu
    /// <param name="screen"></param> màn hình thực hiện hành động
    /// <param name="balance"></param> Số lượng sau khi thực hiện hành động
    public void LogSpendResource(int level, string item_type, string name, int amount, string reason, string screen, int balance)
    {

    }

    /// <summary>
    /// Log khi user ấn mua booster
    /// </summary>
    /// <param name="nameBooster"></param> tên của loại booster
    /// <param name="level"></param> level đang chơi
    public void TrackBuyBooster(string nameBooster, int level)
    {

    }

    /// <summary>
    /// Log khi user ấn dùng booster
    /// </summary>
    /// <param name="nameBooster"></param> tên của loại booster
    /// <param name="level"></param> level đang chơi
    public void TrackUseBooster(string nameBooster, int level)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param> level đang chơi
    /// <param name="item_type"></param> Coin = soft, Gem = hard, Booster = power_up
    /// <param name="name"></param> coin, gem, tên các booster,...
    /// <param name="amount"></param> Số lượng kiếm được
    /// <param name="reason"></param> Lý do kiếm được
    /// <param name="screen"></param> Màn hình thực hiện hành động
    /// <param name="balance"></param> Số lượng sau khi thực hiện hành động
    public void LogEarnCurrency(int level, string item_type, string name, int amount, string reason, string screen, int balance)
    {
       
    }

    void SetUserProperty(string name, string value)
    {
        Debug.Log(name + ": " + value);
    }

    public void SetLevelProperty(int level)
    {
        SetUserProperty("current_level", level.ToString());
    }
    #endregion

    #region Remote Config
    public int GetScoreRate()
    {
        return 5;
    }

    public int GetShowRateFrequency()
    {
        return 10;
    }
    #endregion

    #region Social
    [Header("Social")]
    private const string AndroidRatingURI = "market://details?id={0}";
    private const string IOSRatingURI = "https://apps.apple.com/app/id{0}";
    string url = string.Empty;
    [SerializeField]
    string STORE_APP_ID = string.Empty;
    public void RateGame()
    {
        Debug.Log("Rate Game");
    }
    #endregion

    #region IAP
    public void Purchase(eIAPKey eIAPKey)
    {
        Debug.Log("Purchase: " + eIAPKey);
        switch (eIAPKey)
        {
            case eIAPKey.remove_ads_bundle:
                IAPHandlePurchase.Instance.BuyRemoveAdsBundle();
                break;
            case eIAPKey.coin_pack_1:
                IAPHandlePurchase.Instance.BuyCoin1();
                break;
            case eIAPKey.coin_pack_2:
                IAPHandlePurchase.Instance.BuyCoin2();
                break;
            case eIAPKey.coin_pack_3:
                IAPHandlePurchase.Instance.BuyCoin3();
                break;
            case eIAPKey.coin_pack_4:
                IAPHandlePurchase.Instance.BuyCoin4();
                break;
            case eIAPKey.coin_pack_5:
                IAPHandlePurchase.Instance.BuyCoin5();
                break;
            case eIAPKey.coin_pack_6:
                IAPHandlePurchase.Instance.BuyCoin6();
                break;
        }
        
    }

    public void RestorePurchase()
    {
        Debug.Log("Restore Purchase");
    }

    public string GetProductCurrencyFromStore(eIAPKey eIAPKey)
    {
        return "$";
    }

    public string GetProductPriceStringFromStore(eIAPKey eIAPKey)
    {
        return "0";
    }

    public decimal GetProductPriceFromStore(eIAPKey eIAPKey)
    {
        return 0;
    }

    public bool IsNonConsumablePurchased(eIAPKey eIAPKey)
    {
        return PlayerPrefs.HasKey(eIAPKey.ToString());
    }
    #endregion

    #region Daily Login
    void CheckDailyLogin()
    {
        if (DailyLogin == 0)
        {
            PlayerPrefs.SetInt("retent_type", 0);

            canShowDailyBonus = true;
            return;
        }

        System.DateTime currentTimeDate = System.DateTime.Now.Date;
        System.DateTime lastTimeOpen = System.DateTime.Parse(PlayerPrefs.GetString("lastTimeOpen", System.DateTime.Now.ToString()));
        Debug.Log(PlayerPrefs.GetString("lastTimeOpen", System.DateTime.Now.ToString()));

        int dayOpen = (currentTimeDate - lastTimeOpen).Days;
        if (dayOpen > 0)
        {
            int retend_type = PlayerPrefs.GetInt("retent_type");
            retend_type += dayOpen;
            PlayerPrefs.SetInt("retent_type", retend_type);
            canShowDailyBonus = true;
        }
    }

    public bool CanShowDailyBonus()
    {
        return canShowDailyBonus;
    }

    public void DailyLoginSuccessful()
    {
        DailyLogin++;
        PlayerPrefs.SetString("lastTimeOpen", System.DateTime.Now.Date.ToString());
    }

    internal void Purchase(string v, object value1, object onPurchaseSuccess, object value2)
    {
        throw new NotImplementedException();
    }
    #endregion
}