using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraUI.Objects;
using TerraUI.Utilities;

namespace TerraUI.Panels {
    public class UIPanel : UIObject {
        /// <summary>
        /// Background color of the panel.
        /// </summary>
        public Color BackColor { get; set; }
        /// <summary>
        /// Background texture of the panel.
        /// </summary>
        public Texture2D BackTexture { get; set; }
        /// <summary>
        /// Whether to draw as a Terraria-styled box.
        /// </summary>
        public bool DrawStyledBox { get; set; }
        /// <summary>
        /// Border color of the UIPanel if DrawStyledBox is false.
        /// </summary>
        public Color BorderColor { get; set; }
        /// <summary>
        /// The border width if DrawStyledBox is false.
        /// </summary>
        public byte BorderWidth { get; set; }

        /// <summary>
        /// Create a new UIPanel.
        /// </summary>
        /// <param name="position">position of object in pixels</param>
        /// <param name="size">size of object in pixels</param>
        /// <param name="drawStyledBox">whether to draw as Terraria-styled box</param>
        /// <param name="backColor">background color of panel</param>
        /// <param name="backTexture">background texture of panel</param>
        /// <param name="parent">parent UIObject</param>
        public UIPanel(Vector2 position, Vector2 size, bool drawStyledBox = true, Texture2D backTexture = null, UIObject parent = null) : base(position, size, parent, false) {
            BackTexture = backTexture;
            DrawStyledBox = drawStyledBox;

            BackColor = UIColors.BackColorTransparent;
            BorderColor = UIColors.Panel.BorderColor;
            BorderWidth = 1;
        }

        /// <summary>
        /// Draw the UIPanel.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public override void Draw(SpriteBatch spriteBatch) {
            Rectangle = new Rectangle((int)RelativePosition.X, (int)RelativePosition.Y, (int)Size.X, (int)Size.Y);

            if(BackTexture != null) {
                spriteBatch.Draw(BackTexture, Rectangle, Color.White);
            }
            else {
                if(DrawStyledBox) {
                    DrawingUtils.DrawTerrariaStyledBox(spriteBatch, BackColor, Rectangle);
                }
                else {
                    DrawingUtils.DrawRectangleBox(spriteBatch, BorderColor, BackColor, Rectangle, BorderWidth);
                }
            }

            base.Draw(spriteBatch);
        }
    }
}
