using System.Collections.Generic;
using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class TunnelDefinition
    {
        public string tunnelId;
        public GridPosition position;
        public FacingDirection direction;
        public List<BoxDefinition> contents = new List<BoxDefinition>();
    }
}
