using DG.Tweening;
using HexaFall.Gameplay.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIWin : PanelBase
{
    [SerializeField] private Image m_previewImage;
    [SerializeField] private TextMeshProUGUI m_textCoinReward;
    [SerializeField] private Button m_buttonNext;
    [SerializeField] private Button m_buttonClaimByAds;
    [SerializeField] private Transform m_coinRewardObject;
    [SerializeField] private Transform m_previewPicture;
    [SerializeField] private Transform m_rewardObject;
    [SerializeField] private TextMeshProUGUI m_textBonusCoin;
    [SerializeField] private ParticleSystem m_vfxConfetiiFire;
    [SerializeField] private ParticleSystem m_vfxConfetiiFlow;

    private GameController GameController => GameController.Instance;

    private UnityEvent OnBuyByAds = new();

    private UnityEvent OnRewardFailed = new();

    private void Awake()
    {
        m_buttonNext.onClick.AddListener(OnClickNext);
        m_buttonClaimByAds.onClick.AddListener(OnClickClaimByAds);
        OnBuyByAds.AddListener(RewardSuccess);
        OnRewardFailed.AddListener(OnClickNext);

        m_vfxConfetiiFlow.gameObject.SetActive(false);
        m_vfxConfetiiFire.gameObject.SetActive(false);
    }

    private void RewardSuccess()
    {
        UserManager.Instance.AddCoins(GameController.GetCurrentLevelCoinReward() * 2);
        CGTeamBridge.Instance.LogEarnCurrency(GameController.CurrentLevel, UserResourceType.Coin.ToString(), "earn_coin", GameController.GetCurrentLevelCoinReward() * 2, "win_level", "ui_win", UserManager.Instance.Coins);

        UIManager.Instance.GetPanel<UIReceiveCoin>().PlayCoinFX(default, GameController.GetCurrentLevelCoinReward() * 2, m_coinRewardObject.position, () =>
        {

            GameController.Instance.NextLevel();
            Hide();
            GameController.Instance.StartCurrentLevel();
            //UIManager.Instance.GetPanel<UIHome>().Show();
        });
    }    

    private void OnClickClaimByAds()
    {
        m_buttonClaimByAds.interactable = false;
        m_buttonNext.interactable = false;
        CGTeamBridge.Instance.ShowRewarded("UIWin", null,OnBuyByAds, OnRewardFailed);
    }

    private void OnClickNext()
    {
        m_buttonClaimByAds.interactable = false;
        m_buttonNext.interactable = false;
        UserManager.Instance.AddCoins(GameController.GetCurrentLevelCoinReward());
        CGTeamBridge.Instance.LogEarnCurrency(GameController.CurrentLevel, UserResourceType.Coin.ToString(), "earn_coin", GameController.GetCurrentLevelCoinReward(), "win_level", "ui_win", UserManager.Instance.Coins);
        CGTeamBridge.Instance.ShowInterstitial("Win", null);

        UIManager.Instance.GetPanel<UIReceiveCoin>().PlayCoinFX(default, GameController.GetCurrentLevelCoinReward(), m_coinRewardObject.position, () =>
        {
            GameController.Instance.NextLevel();
            Hide();
            GameController.Instance.StartCurrentLevel();
            //UIManager.Instance.GetPanel<UIHome>().Show();
        });
    }

    public override void Show()
    {
        base.Show();
        gameObject.SetActive(true);

        m_textCoinReward.text = $"{GameController.GetCurrentLevelCoinReward()}";
        m_textBonusCoin.text = $"+{GameController.GetCurrentLevelCoinReward() * 2}";

        StartCoroutine(IEAnimateShow());

        m_buttonClaimByAds.interactable = true;
        m_buttonNext.interactable = true;

        m_previewImage.sprite = Resources.Load<Sprite>($"Data/PreviewLevels/Level_{GameController.Instance.CurrentLevel}");
    }

    private IEnumerator IEAnimateShow()
    {
        AudioManager.Instance.PlayAudioFX(AudioType.Confetti);
        m_previewPicture.localScale = Vector3.zero;
        m_rewardObject.localScale = Vector3.zero;
        m_buttonNext.transform.localScale = Vector3.zero;
        m_buttonClaimByAds.transform.localScale = Vector3.zero;

        m_vfxConfetiiFlow.gameObject.SetActive(false);
        m_vfxConfetiiFire.gameObject.SetActive(true);
        m_vfxConfetiiFire.Stop();
        m_vfxConfetiiFire.Play();
        yield return new WaitForSeconds(0.5f);
        m_vfxConfetiiFlow.gameObject.SetActive(true);
        m_vfxConfetiiFlow.Play();

        yield return null;
        m_previewPicture.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.2f);

        AudioManager.Instance.PlayAudioFX(AudioType.Win);

        m_rewardObject.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.25f);
        m_buttonClaimByAds.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);
        m_buttonNext.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

    }
    override public void Hide()
    {
        base.Hide();
        gameObject.SetActive(false);
    }
}
