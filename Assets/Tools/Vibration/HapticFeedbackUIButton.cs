using System;
using MoreMountains.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace CrazyLabsExtension
{
    [RequireComponent( typeof( Button ) )]
    public class HapticFeedbackUIButton : MonoBehaviour
    {
        public static event Action<HapticTypes> OnHapticButtonPressed = delegate{ };

        private Button _button;

        [SerializeField]
        private HapticTypes _hapticType = HapticTypes.Selection;

        private void Awake()
        {
            _button = GetComponent<Button>( );

            _button.onClick.AddListener( OnButtonClicked );
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener( OnButtonClicked );
        }

        private void OnButtonClicked()
        {
            OnHapticButtonPressed.Invoke( _hapticType );
        }
    }
}