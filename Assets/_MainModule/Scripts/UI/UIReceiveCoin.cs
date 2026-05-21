using DG.Tweening;
using MoreMountains.NiceVibrations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UIReceiveCoin : PanelBase
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI m_txtQuantity;
    [SerializeField] private Transform m_coinContainer;
    [SerializeField] private Transform m_startPosTextBonus;
    [SerializeField] private CoinBar m_coinbar;
    [SerializeField] private ParticleSystem m_targetPointFx;

    [Header("Timings")]
    [SerializeField] private float _moveOutDuration = 0.3f;
    [SerializeField] private float _moveOutDelay = 0.2f;
    [SerializeField] private float _moveToTargetDuration = 0.5f;
    [SerializeField] private float _coinStaggerDelay = 0.15f;

    private Vector3[] _initialPos;
    private Quaternion[] _initialRot;
    private float _originScale = 1f;

    private void Awake()
    {
        int count = m_coinContainer.childCount;
        _initialPos = new Vector3[count];
        _initialRot = new Quaternion[count];
        m_coinbar.enableAutoSync = false;

        for (int i = 0; i < count; i++)
        {
            var rect = m_coinContainer.GetChild(i).GetComponent<RectTransform>();
            _initialPos[i] = rect.localPosition;
            _initialRot[i] = rect.localRotation;
        }

        _originScale = m_coinbar.transform.localScale.x;
    }

    public void PlayCoinFX(Vector3 coinBarPosition, int coinCount, Vector3 coinStartPos = default, UnityAction onFinish = null)
    {
        gameObject.SetActive(true);
        transform.parent.SetAsLastSibling();

        // Reset before play
        ResetCoins();

        StartCoroutine(CoinFXRoutine(coinBarPosition, coinCount, coinStartPos, onFinish));
    }

    private IEnumerator CoinFXRoutine(Vector3 coinBarPosition, int coinCount, Vector3 coinStartPos = default, UnityAction onFinish = null)
    {
        if (coinBarPosition != default)
        {
            m_coinbar.transform.position = coinBarPosition;
        }
        m_coinbar.enableAutoSync = false;
        m_coinbar.SetTextNumber(UserManager.Instance.Coins - coinCount);

        PlayTextFx(coinCount);

        AudioManager.Instance.PlayAudioFX(AudioType.CoinBlast);

        float delayCount = 0f;
        for (int i = 0; i < m_coinContainer.childCount; i++)
        {
            m_coinbar.SetTextNumber(UserManager.Instance.Coins - coinCount);
            Transform coin = m_coinContainer.GetChild(i);

            if (coinStartPos != default)
            {
                coin.transform.position = coinStartPos;
                //coin.transform.localScale = Vector3.one * 0.5f;
            }

            int index = i;

            Sequence coinSeq = DOTween.Sequence();
            coinSeq.Append(coin.DOScale(1f, _moveOutDuration).SetEase(Ease.OutBack))
                   .Join(coin.DOLocalMove(_initialPos[i], _moveOutDuration).SetEase(Ease.OutBack))
                   .Join(coin.DOLocalRotate(coin.localRotation.eulerAngles + Vector3.forward * 360, _moveOutDuration * 5, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        /*.SetLoops(-1, LoopType.Restart)*/)
                   .AppendInterval(_moveOutDelay)
                   .Append(coin.DOMove(m_coinbar.GetIcon().transform.position + Vector3.forward * 5, _moveToTargetDuration).SetEase(Ease.InBack))
                   .Join(coin.DOScale(0.8f, _moveToTargetDuration * 0.8f).OnComplete(() =>
                   {
                       coin.DOScale(0f, _moveToTargetDuration * 0.2f).SetEase(Ease.InBack);
                   }))
                   .Join(coin.DORotate(coin.localRotation.eulerAngles + Vector3.forward * 360, _moveToTargetDuration * 2, RotateMode.FastBeyond360).SetEase(Ease.Linear))
                   //.Append(coin.DOScale(0f, 0.25f).SetEase(Ease.InBack))
                   .Join(m_coinbar.transform.DOScale(_originScale * 1.1f, 0.05f).SetEase(Ease.InOutSine)
                        .SetDelay(_moveToTargetDuration)
                        .OnComplete(() =>
                        {
                            m_coinbar.SetTextNumber(UserManager.Instance.Coins - coinCount + coinCount / m_coinContainer.childCount * (index + 1));
                            m_coinbar.transform.DOScale(_originScale, 0.05f);
                        }))
                   .SetDelay(delayCount)
                   .OnComplete(() =>
                   {
                       //AudioManager.Instance.PlayCoinDingFX();
                       //VibrationManager.VibrateWeak();
                       //AudioManager.Instance.PlayOneShot(AudioClipNames.COLLECT_COIN.ToString(), 1f);
                       HapticFeedbackManager.TriggerHaptics(HapticTypes.LightImpact);
                       AudioManager.Instance.PlayAudioFxOverLap(AudioType.CoinCollect);
                       //Destroy(coinObject.gameObject);

                       if (index == m_coinContainer.childCount - 1)
                       {
                           // Final sync
                           m_coinbar.SetTextNumber(UserManager.Instance.Coins);
                           //_goldCounter.Sync = true;
                           m_coinbar.gameObject.SetActive(false);

                           //m_targetPointFx?.Play();
                           onFinish?.Invoke();


                           Hide();
                       }
                   });
            delayCount += _coinStaggerDelay;
        }

        yield return null;
    }

    Vector3 ConvertUICoinPos(Vector3 pos)
    {
        var screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, pos);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(GetComponent<RectTransform>(), screenPoint, Camera.main, out var newPos);
        return new Vector3(newPos.x, newPos.y, -6);
    }

    private void PlayTextFx(int coinCount)
    {
        m_txtQuantity.gameObject.SetActive(true);
        m_txtQuantity.text = $"+{coinCount}";
        m_txtQuantity.DOFade(0, 0);
        m_txtQuantity.transform.localPosition = Vector3.zero;

        Sequence txtSeq = DOTween.Sequence();
        txtSeq.Append(m_txtQuantity.DOFade(1, 0.3f))
              .Join(m_txtQuantity.transform.DOLocalMoveY(50, 0.5f))
              .AppendInterval(m_coinContainer.childCount * _coinStaggerDelay)
              .Append(m_txtQuantity.DOFade(0, 0.4f))
              .Join(m_txtQuantity.transform.DOLocalMoveY(100, 0.4f));
    }

    private void ResetCoins()
    {
        m_coinbar.gameObject.SetActive(true);
        for (int i = 0; i < m_coinContainer.childCount; i++)
        {
            Transform coin = m_coinContainer.GetChild(i);
            coin.localPosition = m_startPosTextBonus.localPosition;
            coin.localRotation = Quaternion.identity;
            coin.localScale = Vector3.zero;
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(m_txtQuantity.transform);
        DOTween.Kill(m_coinbar.transform);
        foreach (Transform coin in m_coinContainer)
            DOTween.Kill(coin);
    }

#if UNITY_EDITOR
    [ContextMenu("TestReceiveCoinFx")]
    public void TestReceiveCoinFx()
    {
        PlayCoinFX(m_coinbar.transform.position, 182);
    }
#endif
}
