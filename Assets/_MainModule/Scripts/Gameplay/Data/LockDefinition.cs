using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class LockDefinition
    {
        public string lockId;
        public GridPosition position;
        public ColorType color;
    }
}
