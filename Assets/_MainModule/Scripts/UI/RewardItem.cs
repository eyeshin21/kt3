using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RewardItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_textQuanity;

    internal void SetData(RewardDataGame item)
    {
        m_textQuanity.text = item.quantity.ToString();
    }
}
