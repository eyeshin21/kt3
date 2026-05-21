using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textNumber;
    [SerializeField] float time = 0.5f;
    [SerializeField] Image icon;
    protected int number;

    public void SetNumberStart(int number)
    {
        this.number = number;
        textNumber.SetText(this.number.ToString());
    }

    public void SetTextNumberAnim(int number)
    {
        DOTween.Kill(this.number);
        DOTween.To(x => this.number = (int)x, this.number, number, time)
            .SetUpdate(true)
            .onUpdate += delegate
            {
                textNumber.SetText(this.number.ToString());
            };
    }
    public void SetTextNumber(int number)
    {
        DOTween.Kill(this.number);
        this.number = number;
        textNumber.SetText(number.ToString());
    }

    public Vector3 GetPosIcon()
    {
        return icon.transform.position;
    }
    public Transform GetIcon()
    {
        return icon.transform;
    }
}

