using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UIObject {
        protected bool acceptsKeyboardInput = false;
        protected UIObject parent = null;
        protected bool mouseEnter = false;
        protected bool allowFocus = false;

        /// <summary>
        /// Fires when the object is clicked.
        /// </summary>
        public event MouseClickEventHandler Click;
        /// <summary>
        /// Fires when a mouse button is pressed while the cursor is over the UIObject.
        /// </summary>
        public event MouseButtonEventHandler MouseDown;
        /// <summary>
        /// Fires when a mouse button is released while the cursor is over the UIObject.
        /// </summary>
        public event MouseButtonEventHandler MouseUp;
        /// <summary>
        /// Fires when the mouse cursor enters the UIObject.
        /// </summary>
        public event MouseEventHandler MouseEnter;
        /// <summary>
        /// Fires when the mouse cursor leaves the UIObject.
        /// </summary>
        public event MouseEventHandler MouseLeave;
        /// <summary>
        /// Fires each frame the mouse cursor is hovering over the UIObject.
        /// </summary>
        public event MouseEventHandler MouseHover;
        /// <summary>
        /// Fires when the object loses focus.
        /// </summary>
        public event UIEventHandler LostFocus;
        /// <summary>
        /// Fires when the object gains focus.
        /// </summary>
        public event UIEventHandler GotFocus;
        /// <summary>
        /// The X and Y position of the object.
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// The X and Y position of the object relative to its parent.
        /// </summary>
        public Vector2 RelativePosition {
            get {
                if(Parent != null) {
                    return Position + Parent.RelativePosition;
                }
                else {
                    return Position;
                }
            }
        }
        /// <summary>
        /// The X and Y position of the object on the screen, accounting for all its parent objects.
        /// </summary>
        public Vector2 ScreenPosition {
            get {
                UIObject obj = Parent;
                Vector2 position = Position;

                while(obj != null) {
                    position += obj.Position;
                    obj = obj.Parent;
                }

                return position;
            }
        }
        /// <summary>
        /// The width and height of the object on the screen.
        /// </summary>
        public Vector2 Size { get; set; }
        /// <summary>
        /// The rectangle used to draw the object.
        /// </summary>
        public Rectangle Rectangle { get; protected set; }
        /// <summary>
        /// Whether the object has focus.
        /// </summary>
        public bool Focused { get; protected set; }
        /// <summary>
        /// The children of the object.
        /// </summary>
        public List<UIObject> Children { get; protected set; }
        /// <summary>
        /// The parent of the object.
        /// </summary>
        public UIObject Parent {
            get { return parent; }
            set {
                if(parent != null) {
                    parent.Children.Remove(this);
                }

                parent = value;

                if(parent != null) {
                    parent.Children.Add(this);
                }
            }
        }

        /// <summary>
        /// Create a new UIObject.
        /// </summary>
        /// <param name="position">position of the object in pixels</param>
        /// <param name="size">size of the object in pixels</param>
        /// <param name="parent">parent UIObject</param>
        /// <param name="acceptsKeyboardInput">whether the object should capture keyboard input</param>
        public UIObject(Vector2 position, Vector2 size, UIObject parent = null, bool allowFocus = false,
            bool acceptsKeyboardInput = false) {
            Position = position;
            Size = size;
            Children = new List<UIObject>();
            Parent = parent;
            this.allowFocus = allowFocus;
            this.acceptsKeyboardInput = acceptsKeyboardInput;
        }

        /// <summary>
        /// Update the object. Call during any PreUpdate() function.
        /// </summary>
        public virtual void Update() {
            if(!PlayerInput.IgnoreMouseInterface) {
                if(MouseUtils.Rectangle.Intersects(Rectangle)) {
                    Main.player[Main.myPlayer].mouseInterface = true;

                    if(MouseEnter != null && !mouseEnter) {
                        mouseEnter = true;
                        MouseEnter(this, new MouseEventArgs(MouseUtils.Position));
                    }
                    else {
                        OnMouseEnter();
                    }

                    if(MouseHover != null) {
                        MouseHover(this, new MouseEventArgs(MouseUtils.Position));
                    }
                    else {
                        OnMouseHover();
                    }

                    Handle();
                }
                else {
                    if(mouseEnter && MouseLeave != null) {
                        mouseEnter = false;
                        MouseLeave(this, new MouseEventArgs(MouseUtils.Position));
                    }
                    else {
                        OnMouseLeave();
                    }

                    if(MouseUtils.AnyButtonPressed()) {
                        Unfocus();
                    }
                }
            }

            foreach(UIObject obj in Children) {
                obj.Update();
            }
        }

        /// <summary>
        /// Handle the mouse click events.
        /// </summary>
        public virtual void Handle() {
            MouseButtons button = MouseButtons.None;

            if(MouseUtils.AnyButtonPressed(out button)) {
                if(MouseDown != null) {
                    MouseDown(this, new MouseButtonEventArgs(button, MouseUtils.Position));
                }
                else {
                    OnMouseDown();
                }

                if(Click == null || !Click(this, new MouseButtonEventArgs(button, MouseUtils.Position))) {
                    Focus();

                    switch(button) {
                        case MouseButtons.Left:
                            OnLeftClick();
                            break;
                        case MouseButtons.Middle:
                            OnMiddleClick();
                            break;
                        case MouseButtons.Right:
                            OnRightClick();
                            break;
                        case MouseButtons.XButton1:
                            OnXButton1Click();
                            break;
                        case MouseButtons.XButton2:
                            OnXButton2Click();
                            break;
                    }
                }
            }

            if(MouseUtils.AnyButtonReleased(out button)) {
                if(MouseUp != null) {
                    MouseUp(this, new MouseButtonEventArgs(button, MouseUtils.Position));
                }
                else {
                    OnMouseUp();
                }
            }
        }

        /// <summary>
        /// The default left click event.
        /// </summary>
        public virtual void OnLeftClick() { }
        /// <summary>
        /// The default middle click event.
        /// </summary>
        public virtual void OnMiddleClick() { }
        /// <summary>
        /// The default right click event.
        /// </summary>
        public virtual void OnRightClick() { }
        /// <summary>
        /// The default XButton1 click event.
        /// </summary>
        public virtual void OnXButton1Click() { }
        /// <summary>
        /// The default XButton2 click event.
        /// </summary>
        public virtual void OnXButton2Click() { }
        /// <summary>
        /// The default MouseDown event.
        /// </summary>
        public virtual void OnMouseDown() { }
        /// <summary>
        /// The default MouseUp event.
        /// </summary>
        public virtual void OnMouseUp() { }
        /// <summary>
        /// The default MouseEnter event.
        /// </summary>
        public virtual void OnMouseEnter() { }
        /// <summary>
        /// The default MouseLeave event.
        /// </summary>
        public virtual void OnMouseLeave() { }
        /// <summary>
        /// The default MouseHover event.
        /// </summary>
        public virtual void OnMouseHover() { }
        /// <summary>
        /// The default LostFocus event.
        /// </summary>
        public virtual void OnLostFocus() { }
        /// <summary>
        /// The default GotFocus event.
        /// </summary>
        public virtual void OnGotFocus() { }

        /// <summary>
        /// Draw the object. Call during any Draw() function.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public virtual void Draw(SpriteBatch spriteBatch) {
            foreach(UIObject obj in Children) {
                obj.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Give the object focus.
        /// </summary>
        public virtual void Focus() {
            if(!Focused && allowFocus) {
                Focused = true;
                if(acceptsKeyboardInput) {
                    Main.blockInput = true;
                }

                if(GotFocus != null) {
                    GotFocus(this);
                }
                else {
                    OnGotFocus();
                }
            }
        }

        /// <summary>
        /// Take focus from the object.
        /// </summary>
        protected virtual void Unfocus() {
            if(Focused && allowFocus) {
                Focused = false;
                if(acceptsKeyboardInput) {
                    Main.blockInput = false;
                }

                if(LostFocus != null) {
                    LostFocus(this);
                }
                else {
                    OnLostFocus();
                }
            }
        }
    }
}
