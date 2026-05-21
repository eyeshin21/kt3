using System.Collections.Generic;
using UnityEngine;

namespace HexaFall.Gameplay.Data
{
    public enum LevelType
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Expert = 3
    }

    [CreateAssetMenu(fileName = "LevelData", menuName = "Hexa Fall/Gameplay/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        public int level;
        public LevelType levelType;
        public int waitingSlots = 5;
        public GridCellBoardData gridCellBoardData;
        public StackBoardData stackBoard;
        public List<KeyDefinition>    keys    = new List<KeyDefinition>();
        public List<LockDefinition>   locks   = new List<LockDefinition>();
        public List<TunnelDefinition> tunnels = new List<TunnelDefinition>();
        public List<PinDefinition>    pins    = new List<PinDefinition>();

    }
}
