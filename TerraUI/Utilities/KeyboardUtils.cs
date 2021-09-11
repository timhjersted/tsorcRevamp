using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;

namespace TerraUI.Utilities {
    public static class KeyboardUtils {
        /// <summary>
        /// Whether the shift key is pressed.
        /// </summary>
        public static bool Shift {
            get { return (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift)); }
        }
        /// <summary>
        /// Whether the control key is pressed.
        /// </summary>
        public static bool Control {
            get { return (Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl)); }
        }
        /// <summary>
        /// Whether the alt key is pressed.
        /// </summary>
        public static bool Alt {
            get { return (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)); }
        }

        /// <summary>
        /// Check if a key was just pressed.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns>whether key was just pressed</returns>
        public static bool JustPressed(Keys key) {
            if(Main.oldKeyState.IsKeyUp(key) && Main.keyState.IsKeyDown(key)) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a key was just released.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns>whether key was just released</returns>
        public static bool JustReleased(Keys key) {
            if(Main.oldKeyState.IsKeyDown(key) && Main.keyState.IsKeyUp(key)) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a key is held down.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns>whether key is held down</returns>
        public static bool HeldDown(Keys key) {
            if(Main.oldKeyState.IsKeyDown(key) && Main.keyState.IsKeyDown(key)) {
                return true;
            }

            return false;
        }
    }
}
