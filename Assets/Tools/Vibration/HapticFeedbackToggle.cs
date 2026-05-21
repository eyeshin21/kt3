using MoreMountains.NiceVibrations;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabsExtension
{
    [RequireComponent( typeof( Button ) )]
    public class HapticFeedbackToggle : MonoBehaviour
    {
        public static event Action<bool> OnHapticsToggled = delegate{ };

        private Button _button;
        public  Image  image;

        public Sprite activeSprite;
        public Sprite inactiveSprite;

        private bool _currentlyActive = true;

        private void Awake()
        {
            _button = GetComponent<Button>( );

            _currentlyActive = PlayerPrefs.GetInt( "HapticsEnabled", MMVibrationManager.HapticsSupported( ) ? 1 : 0 ) == 1;
            RefreshSprite( );

            _button.onClick.AddListener( OnButtonClicked );
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener( OnButtonClicked );
        }

        private void OnButtonClicked()
        {
            _currentlyActive = !_currentlyActive;
            RefreshSprite( );

            PlayerPrefs.SetInt( "HapticsEnabled", _currentlyActive ? 1 : 0 );
            OnHapticsToggled.Invoke( _currentlyActive );
        }

        private void RefreshSprite()
        {
            image.sprite = ( _currentlyActive ? activeSprite : inactiveSprite );
        }
    }
}