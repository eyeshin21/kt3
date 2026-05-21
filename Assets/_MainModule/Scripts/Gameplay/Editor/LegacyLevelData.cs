using System;

namespace HexaFall.Gameplay.Editor
{
    [System.Serializable]
    public class LegacyLevelData
    {
        public int levelNumber;
        public int levelVersionCode;
        public LegacyCollectorArea collectorArea;
        public LegacyHexStackArea hexStackArea;
    }

    [System.Serializable]
    public class LegacyCollectorArea
    {
        public int gridWidth;
        public int gridHeight;
        public LegacySingleBlock[] singleBlockCollectors;
        public LegacyMystery[] mysteryCollectors;
        public LegacyIce[] iceCollectors;
        public LegacyWoodBox[] woodBoxCollectors;
        public LegacyDeadCell[] deadCells;
        public LegacyTunnel[] tunnels;
        public LegacyPinBlocker[] pinBlockers;
        public LegacyKeyLock[] keyLocks;
    }

    [System.Serializable]
    public class LegacySingleBlock { public int x; public int y; public string color; }
    [System.Serializable]
    public class LegacyMystery { public int x; public int y; public string hiddenColor; }
    [System.Serializable]
    public class LegacyIce { public int x; public int y; public string hiddenColor; public int iceCapacity; }
    [System.Serializable]
    public class LegacyWoodBox { public int x; public int y; public string hiddenColor; }
    [System.Serializable]
    public class LegacyDeadCell { public int x; public int y; }
    [System.Serializable]
    public class LegacyTunnel { public int x; public int y; public string direction; public LegacyTunnelBlock[] collectorQueue; }
    [System.Serializable]
    public class LegacyTunnelBlock { public string color; }
    [System.Serializable]
    public class LegacyPinBlocker { public int x; public int y; public string direction; public int blockCount; }
    [System.Serializable]
    public class LegacyKeyLock { public int x; public int y; public string lockColor; }

    [System.Serializable]
    public class LegacyHexStackArea
    {
        public int gridWidth;
        public int gridHeight;
        public LegacyStack[] stacks;
    }

    [System.Serializable]
    public class LegacyStack
    {
        public int x;
        public int y;
        public string[] colors;
    }
}
