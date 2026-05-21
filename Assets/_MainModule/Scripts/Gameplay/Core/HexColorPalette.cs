using HexaFall.Gameplay.Core;
using UnityEngine;

namespace HexaFall.Gameplay.CoreController
{
    /// <summary>
    /// Maps gameplay color IDs to simple Unity colors for MVP presentation.
    /// </summary>
    public static class HexColorPalette
    {
        /// <summary>
        /// Converts a gameplay color ID to a Unity color.
        /// </summary>
        public static Color ToColor(ColorType colorId)
        {
            return colorId switch
            {
                ColorType.Red => Color.red,
                ColorType.Blue => Color.blue,
                ColorType.Green => Color.green,
                ColorType.Yellow => Color.yellow,
                ColorType.Purple => new Color(0.5f, 0f, 0.8f),
                ColorType.Orange => new Color(1f, 0.5f, 0f),
                ColorType.Pink => new Color(1f, 0.4f, 0.7f),
                ColorType.Cyan => Color.cyan,
                ColorType.White => Color.white,
                ColorType.Black => Color.black,
                _ => Color.gray
            };
        }
    }
}
