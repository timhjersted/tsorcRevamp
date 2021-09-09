using Microsoft.Xna.Framework;

namespace TerraUI.Utilities {
    public static class UIColors {
        public static class TextBox {
            public static readonly Color BorderColor = Color.DarkGray;
            public static readonly Color BackColor = Color.White;
            public static readonly Color TextColor = Color.Black;
        }

        public static class Label {
            public static readonly Color TextColor = Color.White;
            public static readonly Color BorderColor = Color.Black;
        }

        public static class Button {
            public static readonly Color BorderColor = Color.Black;
            public static readonly Color TextColor = Color.White;
        }

        public static class Scrollbar {
            public static readonly Color BorderColor = Color.White;
        }

        public static class Panel {
            public static readonly Color BorderColor = Color.Black;
        }

        public static class CheckBox {
            public static readonly Color BoxColor = Color.White;
            public static readonly Color TextColor = Color.White;
            public static readonly Color TickColor = Color.Black;
            public static readonly Color BoxBorderColor = Color.Black;
        }

        public static class ProgressBar {
            public static readonly Color BackColor = UIColors.BackColor;
            public static readonly Color BarColor = Color.White;
            public static readonly Color BorderColor = Color.White;
        }

        public static readonly Color LightBackColor = new Color(100, 102, 190);
        public static readonly Color LightBackColorTransparent = new Color(100, 102, 190) * 0.7f;
        public static readonly Color DarkBackColor = new Color(63, 65, 151);
        public static readonly Color DarkBackColorTransparent = new Color(63, 65, 151) * 0.7f;
        public static readonly Color BackColor = new Color(63, 82, 151);
        public static readonly Color BackColorTransparent = new Color(63, 82, 151) * 0.7f;
    }
}
