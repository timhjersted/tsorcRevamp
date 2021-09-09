using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UIButton : UIObjectBordered {
        /// <summary>
        /// The font used for the text on the button.
        /// </summary>
        public DynamicSpriteFont Font { get; set; }
        /// <summary>
        /// The text displayed on the button.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The texture used for the back of the button. If null, the BackColor value will be used.
        /// </summary>
        public Texture2D BackTexture { get; set; }
        /// <summary>
        /// Whether to draw the BackTexture over the background or replace the background.
        /// </summary>
        public bool BackTextureReplaces { get; set; }
        /// <summary>
        /// The normal background color.
        /// </summary>
        public Color BackColor { get; set; }
        /// <summary>
        /// The normal text color.
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// Create a new UIButton.
        /// </summary>
        /// <param name="position">position of button in pixels</param>
        /// <param name="size">size of button in pixels</param>
        /// <param name="font">font used for button text</param>
        /// <param name="text">text displayed on button</param>
        /// <param name="borderWidth">width of button border</param>
        /// <param name="backTexture">texture used to draw back of button</param>
        /// <param name="parent">parent UIObject</param>
        public UIButton(Vector2 position, Vector2 size, DynamicSpriteFont font, string text = "", byte borderWidth = 1,
            Texture2D backTexture = null, bool backTextureReplaces = true, UIObject parent = null)
            : base(position, size, borderWidth, parent, false) {
            Font = font;
            Text = text;
            BackTexture = backTexture;
            BorderWidth = borderWidth;
            BackTextureReplaces = backTextureReplaces;

            BackColor = UIColors.DarkBackColorTransparent;
            BorderColor = UIColors.Button.BorderColor;
            TextColor = UIColors.Button.TextColor;
        }

        /// <summary>
        /// Fires when the mouse enters the UIButton.
        /// </summary>
        public override void OnMouseEnter() {
            BackColor = UIColors.LightBackColorTransparent;
        }

        /// <summary>
        /// Fires when the mouse leaves the UIButton.
        /// </summary>
        public override void OnMouseLeave() {
            BackColor = UIColors.DarkBackColorTransparent;
        }

        /// <summary>
        /// Draw the UIButton.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public override void Draw(SpriteBatch spriteBatch) {
            Rectangle = new Rectangle((int)RelativePosition.X, (int)RelativePosition.Y, (int)Size.X, (int)Size.Y);

            if(BackTexture == null || !BackTextureReplaces) {
                DrawingUtils.DrawRectangleBox(spriteBatch, BorderColor, BackColor, Rectangle, BorderWidth);
            }

            if(BackTexture != null) {
                spriteBatch.Draw(BackTexture, Rectangle, Color.White);
            }

            if(!string.IsNullOrWhiteSpace(Text)) {
                Vector2 measure = Font.MeasureString(Text);
                Vector2 origin = new Vector2(measure.X / 2, measure.Y / 2);
                Vector2 textPos = new Vector2(Rectangle.X, Rectangle.Y);

                textPos.X += (Rectangle.Width / 2);
                textPos.Y += (Rectangle.Height / 2) + (measure.Y / 8);

                spriteBatch.DrawString(Font, Text, textPos, TextColor, 0f, origin, 1f, SpriteEffects.None, 0f);
            }

            base.Draw(spriteBatch);
        }
    }
}
