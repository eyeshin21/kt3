using System;
using UnityEngine;
using UnityEngine.UI;

public class UILose : PanelBase
{
    [SerializeField] private Button m_buttonRetry;

    private Action retryAction;

    private void Awake()
    {
        m_buttonRetry.onClick.AddListener(OnClickRetry);
    }

    /// <summary>
    /// Sets the gameplay retry action invoked by the retry button.
    /// </summary>
    public void SetRetryAction(Action action)
    {
        retryAction = action;
    }

    private void OnClickRetry()
    {
        Hide();
        retryAction?.Invoke();
    }

    public override void Show()
    {
        base.Show();
        gameObject.SetActive(true);
    }

    override public void Hide()
    {
        base.Hide();
        gameObject.SetActive(false);
    }
}
