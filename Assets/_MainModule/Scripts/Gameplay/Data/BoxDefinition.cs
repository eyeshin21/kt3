using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class BoxDefinition
    {
        public string boxId;
        public ColorType targetColor;
        public int capacity = 24;

        public bool isHidden;
        public int frozenDurability;
    }
}
