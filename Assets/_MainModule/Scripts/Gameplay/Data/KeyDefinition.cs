using System.Collections.Generic;
using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class KeyDefinition
    {
        public string keyId;
        public GridPosition position;
        public ColorType color;
    }
}
