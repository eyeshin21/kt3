using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.NiceVibrations;


public class HapticFeedbackManager : SingletonMono<HapticFeedbackManager>
{
    public static event Action<bool> OnHapticsEnabledChanged = delegate { };

    private float _hapticTimer = 0f;
    private bool _hapticsPaused = false;

    private const float _hapticMinimumDelay = 0.1f;

    private bool vibrate;

    public bool Vibrate
    {
        get
        {
            if(vibrate != (PlayerPrefs.GetInt("Vibrate", 1) > 0))
            {
                vibrate = PlayerPrefs.GetInt("Vibrate", 1) > 0;
                OnHapticsEnabledChanged.Invoke(vibrate);
            }
            return vibrate;
        }
        set
        {
            vibrate = value;
            PlayerPrefs.SetInt("Vibrate", vibrate?1:0);
        }
    }

    private void Awake()
    {
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
        if (Instance._hapticsPaused)
            return;

        if (!Instance.Vibrate)
            return;

        if (Instance._hapticTimer > 0 && !force)
            return;

        MMVibrationManager.Haptic(type);
        Instance._hapticTimer = _hapticMinimumDelay;
    }
}
