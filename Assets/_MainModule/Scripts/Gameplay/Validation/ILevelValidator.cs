using HexaFall.Gameplay.Config;
using HexaFall.Gameplay.Data;

namespace HexaFall.Gameplay.Validation
{
    /// <summary>
    /// Validates authored level data before runtime puzzle state is created.
    /// </summary>
    public interface ILevelValidator
    {
        /// <summary>
        /// Validates a level and returns all practical authoring errors.
        /// </summary>
        LevelValidationResult Validate(LevelData level, GameplayTuningConfig tuning);
    }
}
