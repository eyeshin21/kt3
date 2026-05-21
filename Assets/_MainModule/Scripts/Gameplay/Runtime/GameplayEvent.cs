using HexaFall.Gameplay.Core;
using HexaFall.Gameplay.Data;

namespace HexaFall.Gameplay.Runtime
{
    public enum GameplayEventType
    {
        None = 0,
        BoxSelected = 1,
        BoxEnteredWaitingArea = 2,
        BlockCollected = 3,
        BoxCleared = 4,
        PuzzleWon = 5,
        PuzzleFailedOutOfSpace = 6,
        PinRemoved = 7,
        BoxThawed = 8,
        BoxRevealed = 9,
        KeyActivated = 10,
        LockDestroyed = 11,
        TunnelReleased = 12,
        StackRevealed = 13,
    }

    public sealed class GameplayEvent
    {
        public GameplayEvent(GameplayEventType eventType, string boxId = null, GridPosition? sourcePosition = null, ColorType color = ColorType.None, int fillCount = 0)
        {
            EventType = eventType;
            BoxId = boxId;
            SourcePosition = sourcePosition;
            Color = color;
            FillCount = fillCount;
        }

        public GameplayEventType EventType { get; }
        public string BoxId { get; }
        public GridPosition? SourcePosition { get; }
        public ColorType Color { get; }
        public int FillCount { get; }
    }
}
