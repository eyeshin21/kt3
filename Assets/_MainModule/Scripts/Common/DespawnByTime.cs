using DG.Tweening;
using Lean.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DespawnByTime : MonoBehaviour
{
    [SerializeField] private float delay = 5f;
    private Tween tweenDelay;

    private void OnEnable()
    {
        tweenDelay = DOVirtual.DelayedCall(delay, () =>
        {
            LeanPool.Despawn(gameObject);
        }).SetLink(gameObject).SetId("DespawnByTime");
    }

    private void OnDisable()
    {
        if(tweenDelay.IsActive())
        {
            tweenDelay.Kill();
        }
    }
}
