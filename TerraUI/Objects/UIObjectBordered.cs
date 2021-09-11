using Microsoft.Xna.Framework;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UIObjectBordered : UIObject {
        /// <summary>
        /// The color of the border.
        /// </summary>
        public Color BorderColor { get; set; }
        /// <summary>
        /// The width of the border.
        /// </summary>
        public byte BorderWidth { get; set; }

        public UIObjectBordered(Vector2 position, Vector2 size, byte borderWidth = 1, UIObject parent = null,
            bool allowFocus = false, bool acceptsKeyboardInput = false)
            : base(position, size, parent, allowFocus, acceptsKeyboardInput) {
            BorderWidth = borderWidth;
            BorderColor = UIColors.Button.BorderColor;
        }
    }
}
