using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class PinDefinition
    {
        public string pinId;
        public GridPosition headPosition;
        public PinDirection direction;
        public int length = 1;
    }
}
