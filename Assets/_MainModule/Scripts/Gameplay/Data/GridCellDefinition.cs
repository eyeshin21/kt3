using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class GridCellDefinition
    {
        public GridPosition position;
        public GridCellType cellType;

        public BoxDefinition box;

        public string pinId;
        public string tunnelId;
        public string keyId;
        public string lockId;
    }
}
