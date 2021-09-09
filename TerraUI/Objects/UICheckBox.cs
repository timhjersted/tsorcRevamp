using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UICheckBox : UIObject {
        public const int MINIMUM_BOX_SIZE = 30;

        /// <summary>
        /// Fires when the checked value is changed.
        /// </summary>
        public event ValueChangedEventHandler<bool> ValueChanged;
        /// <summary>
        /// The font used to draw the text.
        /// </summary>
        public DynamicSpriteFont Font { get; set; }
        /// <summary>
        /// Whether the UICheckBox is checked or not.
        /// </summary>
        public bool Checked { get; set; }
        /// <summary>
        /// The text displayed next to the UICheckBox.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The size of the check box.
        /// </summary>
        public int BoxSize { get; set; }
        /// <summary>
        /// The background color of the check box.
        /// </summary>
        public Color BoxColor { get; set; }
        /// <summary>
        /// The border color of the check box.
        /// </summary>
        public Color BoxBorderColor { get; set; }
        /// <summary>
        /// The color of the text drawn next to the UICheckBox.
        /// </summary>
        public Color TextColor { get; set; }
        /// <summary>
        /// The color of the tick inside the UICheckBox.
        /// </summary>
        public Color TickColor { get; set; }
        /// <summary>
        /// The width of the border around the check box.
        /// </summary>
        public byte BoxBorderWidth { get; set; }

        /// <summary>
        /// Create a new UICheckBox.
        /// </summary>
        /// <param name="position">position of object in pixels</param>
        /// <param name="width">width of object in pixels</param>
        /// <param name="font">font used to draw text</param>
        /// <param name="text">text displayed next to check box</param>
        /// <param name="defaultValue">default value of the check box</param>
        /// <param name="parent">parent UIObject</param>
        public UICheckBox(Vector2 position, int width, DynamicSpriteFont font, string text = "", bool defaultValue = false, UIObject parent = null)
            : base(position, new Vector2(width, MINIMUM_BOX_SIZE), parent, true) {
            Checked = defaultValue;
            Text = text;
            BoxSize = MINIMUM_BOX_SIZE;
            Font = font;

            BoxColor = UIColors.CheckBox.BoxColor;
            BoxBorderColor = UIColors.CheckBox.BoxBorderColor;
            TextColor = UIColors.CheckBox.TextColor;
            TickColor = UIColors.CheckBox.TickColor;
            BoxBorderWidth = 1;
        }

        /// <summary>
        /// Create a new UICheckBox.
        /// </summary>
        /// <param name="position">position of object in pixels</param>
        /// <param name="width">width of object in pixels</param>
        /// <param name="boxSize">height of check box</param>
        /// <param name="font">font used to draw text</param>
        /// <param name="text">text displayed next to check box</param>
        /// <param name="defaultValue">default value of the check box</param>
        /// <param name="parent">parent UIObject</param>
        public UICheckBox(Vector2 position, int width, int boxHeight, DynamicSpriteFont font, string text = "", bool defaultValue = false,
            UIObject parent = null) : base(position, new Vector2(width, boxHeight), parent, false, true) {
            Checked = defaultValue;
            Text = text;
            Font = font;
            BoxSize = boxHeight;

            BoxColor = UIColors.CheckBox.BoxColor;
            BoxBorderColor = UIColors.CheckBox.BoxBorderColor;
            TextColor = UIColors.CheckBox.TextColor;
            TickColor = UIColors.CheckBox.TickColor;
            BoxBorderWidth = 1;
        }

        /// <summary>
        /// Update the UICheckBox.
        /// </summary>
        public override void Update() {
            if(Focused) {
                if(KeyboardUtils.JustPressed(Keys.Enter)) {
                    Toggle();
                }
            }

            base.Update();
        }

        /// <summary>
        /// The default left click event.
        /// </summary>
        public override void OnLeftClick() {
            Toggle();
        }

        /// <summary>
        /// Toggle the value of the UICheckBox.
        /// </summary>
        /// <returns></returns>
        public bool Toggle() {
            Checked = !Checked;

            if(ValueChanged != null) {
                ValueChanged(this, new ValueChangedEventArgs<bool>(!Checked, Checked));
            }

            return Checked;
        }

        /// <summary>
        /// Draw the UICheckBox.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public override void Draw(SpriteBatch spriteBatch) {
            Rectangle = new Rectangle((int)RelativePosition.X, (int)RelativePosition.Y, (int)Size.X, (int)Size.Y);

            DrawingUtils.DrawRectangleBox(spriteBatch, BoxBorderColor, BoxColor, new Rectangle(Rectangle.X, Rectangle.Y,
                BoxSize, BoxSize), BoxBorderWidth);

            if(Checked) {
                Texture2D tick = UIUtils.GetTexture("Tick");
                float scaleW = (float)BoxSize / tick.Width * 0.60f;
                float scaleH = (float)BoxSize / tick.Height * 0.60f;
                Vector2 origin = new Vector2(tick.Width / 2, tick.Height / 2);

                spriteBatch.Draw(
                    tick,
                    RelativePosition + new Vector2(BoxSize / 2, BoxSize / 2),
                    null,
                    TickColor,
                    0f,
                    origin,
                    new Vector2(scaleW, scaleH),
                    SpriteEffects.None,
                    0f);
            }

            if(!string.IsNullOrWhiteSpace(Text)) {
                Vector2 size = Font.MeasureString(Text);
                Vector2 origin = new Vector2(0, size.Y * 0.5f);
                Vector2 pos = new Vector2(BoxSize + 5, Rectangle.Height * 0.5f);
                float width = Font.MeasureString(Text).X;
                string text = Text;

                while(width >= (Size.X - BoxSize)) {
                    text = text.Remove(text.Length - 1);
                    width = Font.MeasureString(text).X;
                }

                if(Rectangle.Height < size.Y) {
                    origin.Y = 0;
                    pos.Y = 0;
                }

                spriteBatch.DrawString(
                                Font,
                                text,
                                RelativePosition + pos,
                                TextColor,
                                0f,
                                origin,
                                1f,
                                SpriteEffects.None,
                                0f);
            }

            base.Draw(spriteBatch);
        }
    }
}
