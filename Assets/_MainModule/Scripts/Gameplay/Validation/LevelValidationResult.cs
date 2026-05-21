using System.Collections.Generic;

namespace HexaFall.Gameplay.Validation
{
    /// <summary>
    /// Immutable result returned by level validation.
    /// </summary>
    public sealed class LevelValidationResult
    {
        /// <summary>
        /// Creates a validation result from collected errors.
        /// </summary>
        public LevelValidationResult(IReadOnlyList<string> errors)
        {
            Errors = errors ?? new List<string>();
        }

        /// <summary>
        /// True when validation found no errors.
        /// </summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>
        /// All practical validation errors detected in one pass.
        /// </summary>
        public IReadOnlyList<string> Errors { get; }
    }
}
