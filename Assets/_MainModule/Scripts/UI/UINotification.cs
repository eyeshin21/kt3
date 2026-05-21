using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UINotification : PanelBase
{
    [SerializeField] private Button m_buttonClose;
    [SerializeField] private TextMeshProUGUI m_textContent;

    private void Awake()
    {
        m_buttonClose.onClick.AddListener(OnClickClose);
    }

    private void OnClickClose()
    {
        Hide();
    }

    public void Show(string content)
    {
        base.Show();
        m_textContent.text = content;
    }    
}
