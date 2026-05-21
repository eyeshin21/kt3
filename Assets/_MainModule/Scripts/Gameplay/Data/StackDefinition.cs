using System.Collections.Generic;
using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    [System.Serializable]
    public sealed class StackDefinition
    {
        public bool useExplicitPosition;
        public GridPosition position;
        public List<ColorType> blocksBottomToTop = new List<ColorType>();
        public bool isHidden;

    }
}
