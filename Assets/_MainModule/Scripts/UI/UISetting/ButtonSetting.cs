using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ButtonSetting : MonoBehaviour
{
    [SerializeField] private Button m_button;
    [SerializeField] private GameObject m_activeObject;
    [SerializeField] private GameObject m_deactiveObject;

    public UnityEvent OnClick = new UnityEvent();

    private void Awake()
    {
        m_button.onClick.AddListener(OnClickButton);
    }

    private void OnClickButton()
    {
        OnClick?.Invoke();
    }

    public void SetActive(bool isActive)
    {
        m_activeObject.SetActive(isActive);
        m_deactiveObject.SetActive(!isActive);
    }
    
}
