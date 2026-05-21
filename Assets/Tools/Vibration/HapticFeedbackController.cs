using System;
using UnityEngine;
using MoreMountains.NiceVibrations;

public class HapticFeedbackController : MonoBehaviour
{
    public static event Action<bool> OnHapticsEnabledChanged = delegate { };

    private static HapticFeedbackController _instance;

    private float _hapticTimer = 0f;
    private bool _hapticsPaused = false;

    private const float _hapticMinimumDelay = 0.1f;

    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        _hapticTimer -= Time.deltaTime;
    }

    private void Start()
    {
#if UNITY_IOS
            MMVibrationManager.iOSInitializeHaptics( );
#endif
    }

    public static void TriggerHaptics(HapticTypes type, bool force = false)
    {
        if (_instance._hapticsPaused)
            return;

        //if (!UserConfig.Instance.Vibrate)
        //    return;

        if (_instance._hapticTimer > 0 && !force)
            return;

        MMVibrationManager.Haptic(type);
        _instance._hapticTimer = _hapticMinimumDelay;
    }
}