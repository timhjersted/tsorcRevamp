using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UIProgressBar : UIObjectBordered {
        private uint value = 0;
        private uint maximum = 100;
        private uint minimum = 0;
        private uint stepAmount = 1;
        private bool callFinished = true;

        /// <summary>
        /// Fires when the value of the UIProgressBar is changed.
        /// </summary>
        public event ValueChangedEventHandler<uint> ProgressChanged;
        /// <summary>
        /// Fires when the value of the UIProgressBar is equal to the maximum value.
        /// </summary>
        public event UIEventHandler ProgressFinished;
        /// <summary>
        /// The maximum value of the UIProgressBar.
        /// </summary>
        public uint Maximum {
            get { return maximum; }
            set {
                if(value < 0) {
                    maximum = 1;
                }
                else if(value <= Minimum) {
                    maximum = Minimum + 1;
                }
                else {
                    maximum = value;
                }
            }
        }
        /// <summary>
        /// The minimum value of the UIProgressBar.
        /// </summary>
        public uint Minimum {
            get { return minimum; }
            set {
                if(value < 0) {
                    minimum = 0;
                }
                else if(value >= Maximum) {
                    minimum = Maximum - 1;
                }
                else {
                    minimum = value;
                }
            }
        }
        /// <summary>
        /// The current filled percent of the UIProgressBar (between 0 and 1).
        /// </summary>
        public float Percent {
            get { return (float)value / Maximum; }
        }
        /// <summary>
        /// The current value of the UIProgressBar.
        /// </summary>
        public uint Value {
            get { return value; }
            set {
                if(value < 0) {
                    this.value = value;
                }
                else if(value > Maximum) {
                    this.value = Maximum;
                }
                else {
                    this.value = value;
                }
            }
        }
        /// <summary>
        /// The amount that the progress of the UIProgressBar changes with each Step() call.
        /// </summary>
        public uint StepAmount {
            get { return stepAmount; }
            set {
                if(value < 1) {
                    stepAmount = 1;
                }
                else if(value > Maximum) {
                    stepAmount = Maximum;
                }
                else {
                    stepAmount = value;
                }
            }
        }
        /// <summary>
        /// The background color of the UIProgressBar.
        /// </summary>
        public Color BackColor { get; set; }
        /// <summary>
        /// The color of the progress bar.
        /// </summary>
        public Color BarColor { get; set; }
        /// <summary>
        /// The margin around the progress bar inside the UIProgressBar.
        /// </summary>
        public Vector2 BarMargin { get; set; }

        /// <summary>
        /// Create a new UIProgressBar.
        /// </summary>
        /// <param name="position">position of the object in pixels</param>
        /// <param name="size">size of the object</param>
        /// <param name="minimum">minimum progress value</param>
        /// <param name="maximum">maximum progress value</param>
        /// <param name="stepAmount">amount progress changes with each step</param>
        /// <param name="parent">parent UIObject</param>
        public UIProgressBar(Vector2 position, Vector2 size, uint minimum = 0, uint maximum = 100, uint stepAmount = 1,
            byte borderWidth = 1, UIObject parent = null) : base(position, size, borderWidth, parent, false) {
            Minimum = minimum;
            Maximum = maximum;
            StepAmount = stepAmount;
            BackColor = UIColors.ProgressBar.BackColor;
            BarColor = UIColors.ProgressBar.BarColor;
            BorderColor = UIColors.ProgressBar.BorderColor;
            BorderWidth = 1;
            BarMargin = Vector2.Zero;
        }

        /// <summary>
        /// Perform a step.
        /// </summary>
        public void Step() {
            if(Value < Maximum) {
                Value += StepAmount;

                if(ProgressChanged != null) {
                    ProgressChanged(this, new ValueChangedEventArgs<uint>(Value - StepAmount, Value));
                }
            }
            else {
                if(ProgressFinished != null && callFinished) {
                    ProgressFinished(this);
                    callFinished = false;
                }
            }
        }

        /// <summary>
        /// Reset the UIProgressBar's progress.
        /// </summary>
        public void Reset() {
            Value = Minimum;
            callFinished = true;
        }

        /// <summary>
        /// Finish the UIProgressBar's progress.
        /// </summary>
        public void Finish() {
            Value = Maximum;

            if(ProgressFinished != null && callFinished) {
                ProgressFinished(this);
                callFinished = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            Rectangle = new Rectangle((int)RelativePosition.X, (int)RelativePosition.Y, (int)Size.X, (int)Size.Y);
            Rectangle barRectangle = new Rectangle((int)(RelativePosition.X + BarMargin.X), (int)(RelativePosition.Y + BarMargin.Y),
                                                   (int)(Size.X - BarMargin.X * 2), (int)(Size.Y - BarMargin.Y * 2));

            barRectangle.Width = (int)((Size.X * Percent) - BarMargin.X * 2);

            DrawingUtils.DrawRectangleBox(spriteBatch, BorderColor, BackColor, Rectangle, BorderWidth);
            DrawingUtils.DrawRectangleBox(spriteBatch, Color.Transparent, BarColor, barRectangle, 0);

            base.Draw(spriteBatch);
        }
    }
}
