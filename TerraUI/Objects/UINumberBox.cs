using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UINumberBox : UIObject {
        private const int frameDelay = 9;
        private int up = 0;
        private int down = 0;

        private UITextBox textBox;
        private UIButton upButton;
        private UIButton downButton;
        private int value = 0;
        private int maximum = 100;
        private int minimum = 0;
        private uint stepAmount = 1;

        /// <summary>
        /// Fires when the value of the UINumberBox is changed.
        /// </summary>
        public event ValueChangedEventHandler<int> ValueChanged;
        /// <summary>
        /// The maximum value of the UINumberBox.
        /// </summary>
        public int Maximum {
            get { return maximum; }
            set {
                if(value <= Minimum) {
                    maximum = Minimum + 1;
                }
                else {
                    maximum = value;
                }
            }
        }
        /// <summary>
        /// The minimum value of the UINumberBox.
        /// </summary>
        public int Minimum {
            get { return minimum; }
            set {
                if(value >= Maximum) {
                    minimum = Maximum - 1;
                }
                else {
                    minimum = value;
                }
            }
        }
        /// <summary>
        /// The current value of the UINumberBox.
        /// </summary>
        public int Value {
            get { return Convert.ToInt32(textBox.Text); }
            set {
                int oldValue = this.value;
                this.value = (int)MathHelper.Clamp(value, Minimum, Maximum);

                if(textBox != null) {
                    textBox.TextChanged -= TextBox_TextChanged;
                    textBox.Text = this.value.ToString();
                    textBox.TextChanged += TextBox_TextChanged;
                }

                if(ValueChanged != null && oldValue != this.value) {
                    ValueChanged(this, new ValueChangedEventArgs<int>(oldValue, this.value));
                }
            }
        }
        /// <summary>
        /// The amount that the value of the UINumberBox changes with each Increase() or Decrease() call.
        /// </summary>
        public uint StepAmount {
            get { return stepAmount; }
            set {
                if(value < 1) {
                    stepAmount = 1;
                }
                else {
                    stepAmount = value;
                }
            }
        }
        /// <summary>
        /// The background color of the UINumberBox text.
        /// </summary>
        public Color TextBackColor {
            get { return textBox.BackColor; }
            set { textBox.BackColor = value; }
        }
        /// <summary>
        /// The background color of the UINumberBox up button.
        /// </summary>
        public Color UpBackColor {
            get { return upButton.BackColor; }
            set { upButton.BackColor = value; }
        }
        /// <summary>
        /// The background color of the UINumberBox down button.
        /// </summary>
        public Color DownBackColor {
            get { return downButton.BackColor; }
            set { downButton.BackColor = value; }
        }
        /// <summary>
        /// The text color of the UINumberBox.
        /// </summary>
        public Color TextColor {
            get { return textBox.TextColor; }
            set { textBox.TextColor = value; }
        }
        /// <summary>
        /// Font used to draw the text.
        /// </summary>
        public DynamicSpriteFont Font { get; set; }

        /// <summary>
        /// Create a new UINumberBox.
        /// </summary>
        /// <param name="position">position of the object in pixels</param>
        /// <param name="size">size of the object</param>
        /// <param name="value">starting value</param>
        /// <param name="minimum">minimum value</param>
        /// <param name="maximum">maximum value</param>
        /// <param name="stepAmount">amount value changes with each step</param>
        /// <param name="parent">parent UIObject</param>
        public UINumberBox(Vector2 position, Vector2 size, DynamicSpriteFont font, int value = 0, int minimum = 0,
            int maximum = 100, uint stepAmount = 1, UIObject parent = null)
            : base(position, size, parent, true, true) {
            Font = font;
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            StepAmount = stepAmount;

            textBox = new UITextBox(Vector2.Zero, size, font, value.ToString(), parent: this);
            upButton = new UIButton(
                new Vector2(size.X - (size.Y / 2) - 2, 2),
                new Vector2((size.Y / 2) - 3, (size.Y / 2) - 2),
                font, "", 1, UIUtils.GetTexture("UpArrow"), parent: this);
            downButton = new UIButton(
                new Vector2(size.X - (size.Y / 2) - 2, (size.Y / 2)),
                new Vector2((size.Y / 2) - 3, (size.Y / 2) - 2),
                font, "", 1, UIUtils.GetTexture("DownArrow"), parent: this);

            textBox.Strip = "\\D";

            textBox.TextChanged += TextBox_TextChanged;

            upButton.Click += UpButton_Click;
            downButton.Click += DownButton_Click;
        }

        private void TextBox_TextChanged(UIObject sender, ValueChangedEventArgs<string> e) {
            // ensure that the textbox text doesn't contain a leading zero, stays within bounds, etc.
            Value = (string.IsNullOrWhiteSpace(textBox.Text) ? Value = Minimum : Convert.ToInt32(textBox.Text));
        }

        private bool UpButton_Click(UIObject sender, MouseButtonEventArgs e) {
            Increase();
            return true;
        }

        private bool DownButton_Click(UIObject sender, MouseButtonEventArgs e) {
            Decrease();
            return true;
        }

        /// <summary>
        /// Increase by the StepAmount.
        /// </summary>
        private void Increase() {
            Value += (int)StepAmount;
        }

        /// <summary>
        /// Decrease by the StepAmount.
        /// </summary>
        private void Decrease() {
            Value -= (int)StepAmount;
        }

        /// <summary>
        /// Update the UINumberBox.
        /// </summary>
        public override void Update() {
            if(textBox.Focused) {
                if(KeyboardUtils.JustPressed(Keys.Up) || KeyboardUtils.HeldDown(Keys.Up)) {
                    if(up == 0) {
                        Increase();
                        up = frameDelay;
                    }
                    up--;
                }
                else if(KeyboardUtils.JustPressed(Keys.Down) || KeyboardUtils.HeldDown(Keys.Down)) {
                    if(down == 0) {
                        Decrease();
                        down = frameDelay;
                    }
                    down--;
                }

                textBox.SelectionStart = textBox.Text.Length;
            }

            base.Update();
        }
    }
}
