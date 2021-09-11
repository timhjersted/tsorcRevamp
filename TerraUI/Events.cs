using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TerraUI.Objects;

namespace TerraUI {
    public delegate void UIEventHandler(UIObject sender);
    public delegate bool MouseClickEventHandler(UIObject sender, MouseButtonEventArgs e);
    public delegate void MouseButtonEventHandler(UIObject sender, MouseButtonEventArgs e);
    public delegate void MouseEventHandler(UIObject sender, MouseEventArgs e);
    public delegate void DrawHandler(UIObject sender, SpriteBatch spriteBatch);
    public delegate bool ConditionHandler(Item item);
    public delegate void ValueChangedEventHandler<T>(UIObject sender, ValueChangedEventArgs<T> e);

    public class ValueChangedEventArgs<T> {
        public T PreviousValue { get; private set; }
        public T Value { get; private set; }

        public ValueChangedEventArgs(T previousValue, T value) {
            PreviousValue = previousValue;
            Value = value;
        }
    }

    public class MouseButtonEventArgs {
        public MouseButtons Button { get; private set; }
        public Vector2 Position { get; private set; }

        public MouseButtonEventArgs(MouseButtons button, Vector2 position) {
            Button = button;
            Position = position;
        }
    }

    public class MouseEventArgs {
        public Vector2 Position { get; private set; }

        public MouseEventArgs(Vector2 position) {
            Position = position;
        }
    }
}
