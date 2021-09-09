using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;
using TerraUI.Utilities;

namespace TerraUI.Objects {
    public class UIItemSlot : UIObject {
        protected Item item;
        protected string hoverText = "";
        protected float backOpacity = 1f;
        protected float itemOpacity = 1f;
        protected const int defaultSize = 52;
        protected Rectangle tickRect;

        /// <summary>
        /// Method that checks whether an item can go in the slot.
        /// </summary>
        public ConditionHandler Conditions { get; set; }
        /// <summary>
        /// Method that draws the background of the item slot.
        /// </summary>
        public DrawHandler DrawBackground { get; set; }
        /// <summary>
        /// Method that draws the item in the slot.
        /// </summary>
        public DrawHandler DrawItem { get; set; }
        /// <summary>
        /// Method called after the item in the slot is drawn.
        /// </summary>
        public DrawHandler PostDrawItem { get; set; }
        /// <summary>
        /// Whether the item in the slot is visible on the player character.
        /// </summary>
        public bool ItemVisible { get; set; }
        /// <summary>
        /// Whether to draw the slot as a normal item slot.
        /// </summary>
        public bool DrawAsNormalSlot { get; set; }
        /// <summary>
        /// The context for the slot.
        /// </summary>
        public int Context { get; set; }
        /// <summary>
        /// The text to show when the mouse hovers over the slot. If blank, text is determined by slot context.
        /// </summary>
        public string HoverText {
            get { return hoverText; }
            set {
                if(string.IsNullOrEmpty(value)) {
                    hoverText = UIUtils.GetHoverText(Context);
                }
                else {
                    hoverText = value;
                }
            }
        }
        /// <summary>
        /// Whether to scale the slot with the inventory's scale.
        /// </summary>
        public bool ScaleToInventory { get; set; }
        /// <summary>
        /// The item shown in the slot.
        /// </summary>
        public Item Item {
            get { return item; }
            set { item = value; }
        }
        /// <summary>
        /// The slot to swap items with if this slot is right-clicked.
        /// </summary>
        public UIItemSlot Partner { get; set; }
        /// <summary>
        /// The opacity of the item slot background (between 0 and 1).
        /// </summary>
        public float BackOpacity {
            get { return backOpacity; }
            set { backOpacity = MathHelper.Clamp(value, 0f, 1f); }
        }
        /// <summary>
        /// The opacity of the item texture in the slot (between 0 and 1).
        /// </summary>
        public float ItemOpacity {
            get { return itemOpacity; }
            set { itemOpacity = MathHelper.Clamp(value, 0f, 1f); }
        }

        /// <summary>
        /// Create a new UIItemSlot.
        /// </summary>
        /// <param name="position">position of slot in pixels</param>
        /// <param name="size">size of slot in pixels</param>
        /// <param name="context">context for slot</param>
        /// <param name="hoverText">hover text to show instead of context</param>
        /// <param name="parent">parent UIObject</param>
        /// <param name="conditions">checked before item is placed in slot; if null, all items are permitted</param>
        /// <param name="drawBackground">run when slot background is drawn; if null, slot is drawn with background texture</param>
        /// <param name="drawItem">run when item in slot is drawn; if null, item is drawn in center of slot</param>
        /// <param name="postDrawItem">run after item in slot is drawn; use to draw elements over the item</param>
        /// <param name="drawAsNormalSlot">draw as a normal inventory ItemSlot</param>
        /// <param name="scaleToInventory">whether to scale with the inventory</param>
        public UIItemSlot(Vector2 position, int size = 52, int context = ItemSlot.Context.InventoryItem,
            string hoverText = "", UIObject parent = null, ConditionHandler conditions = null,
            DrawHandler drawBackground = null, DrawHandler drawItem = null, DrawHandler postDrawItem = null,
            bool drawAsNormalSlot = false, bool scaleToInventory = false)
            : base(position, new Vector2(size), parent, false) {
            Item = new Item();
            Context = context;
            HoverText = hoverText;
            Conditions = conditions;
            DrawBackground = drawBackground;
            DrawItem = drawItem;
            PostDrawItem = postDrawItem;
            DrawAsNormalSlot = drawAsNormalSlot;
            ScaleToInventory = scaleToInventory;
        }

        /// <summary>
        /// The default left click event.
        /// </summary>
        public override void OnLeftClick() {
            if(Main.mouseItem.stack < 1 && item.type < 1) {
                return;
            }

            if(Main.mouseItem.stack < 1 || Conditions == null || Conditions(Main.mouseItem)) {
                Swap(ref item, ref Main.mouseItem);
            }
        }

        /// <summary>
        /// The default right click event.
        /// </summary>
        public override void OnRightClick() {
            if(Conditions(Main.mouseItem)) {
                Swap(ref item, ref Main.mouseItem);
            }
            else if(Partner != null && (Item.stack > 0 || Partner.Item.stack > 0)) {
                Swap(ref item, ref Partner.item);
            }
        }

        /// <summary>
        /// Swap two items between slots or the slot and the mouse cursor.
        /// </summary>
        /// <param name="item1">first item</param>
        /// <param name="item2">second item</param>
        public void Swap(ref Item item1, ref Item item2) {
            UIUtils.SwitchItems(ref item1, ref item2);
            Main.PlaySound(SoundID.Grab);
            Recipe.FindRecipes();
        }

        /// <summary>
        /// Toggle the visibility of the item in the slot.
        /// </summary>
        public void ToggleVisibility() {
            ItemVisible = !ItemVisible;
            Main.PlaySound(SoundID.MenuTick);
        }

        /// <summary>
        /// Update the item slot.
        /// </summary>
        public override void Update() {
            if(!PlayerInput.IgnoreMouseInterface) {
                bool intersectsTick = MouseUtils.Rectangle.Intersects(tickRect) && HasTick();
                bool intersectsRect = MouseUtils.Rectangle.Intersects(Rectangle);

                if(intersectsRect || intersectsTick) {
                    Main.player[Main.myPlayer].mouseInterface = true;
                }

                if(intersectsTick) {
                    Main.hoverItemName =
                        (ItemVisible ? Language.GetTextValue("LegacyInterface.59") : Language.GetTextValue("GameUI.Hidden"));

                    if(MouseUtils.JustPressed(MouseButtons.Left)) {
                        ToggleVisibility();
                    }
                }
                else if(intersectsRect) {
                    Main.HoverItem = Item;

                    if(Item.stack < 1) {
                        Main.hoverItemName = HoverText;
                    }
                    else if(Item.stack > 0) {
                        Main.hoverItemName = Item.Name;
                    }
                    else if(Item.stack > 1) {
                        Main.hoverItemName = Item.Name + " (" + Item.stack + ")";
                    }
                }

                if(!intersectsTick) {
                    base.Update();
                }
            }
        }

        /// <summary>
        /// Draw the object. Call during any Draw() function.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public override void Draw(SpriteBatch spriteBatch) {
            Rectangle = new Rectangle((int)RelativePosition.X, (int)RelativePosition.Y, (int)Size.X, (int)Size.Y);

            if(DrawAsNormalSlot) {
                ItemSlot.Draw(spriteBatch, ref item, (int)Context, RelativePosition);
            }
            else {
                if(DrawBackground != null) {
                    DrawBackground(this, spriteBatch);
                }
                else {
                    OnDrawBackground(spriteBatch);
                }

                if(Item.type > 0) {
                    if(DrawItem != null) {
                        DrawItem(this, spriteBatch);
                    }
                    else {
                        OnDrawItem(spriteBatch);
                    }
                }
            }

            if(PostDrawItem != null) {
                PostDrawItem(this, spriteBatch);
            }
            else {
                if(HasTick() && !DrawAsNormalSlot) {
                    DrawTick(spriteBatch);
                }
            }

            base.Draw(spriteBatch);
        }

        /// <summary>
        /// The default DrawBackground function.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public void OnDrawBackground(SpriteBatch spriteBatch) {
            Texture2D backTex = UIUtils.GetContextTexture(Context);
            spriteBatch.Draw(
                backTex,
                Rectangle.TopLeft(),
                null,
                Color.White * BackOpacity,
                0f,
                Vector2.Zero,
                Scale(true),
                SpriteEffects.None,
                1f);
        }

        /// <summary>
        /// The default DrawItem function.
        /// </summary>
        /// <param name="spriteBatch">drawing SpriteBatch</param>
        public void OnDrawItem(SpriteBatch spriteBatch) {
            Texture2D texture2D = Main.itemTexture[Item.type];
            Rectangle rectangle;

            if(Main.itemAnimations[Item.type] != null) {
                rectangle = Main.itemAnimations[Item.type].GetFrame(texture2D);
            }
            else {
                rectangle = texture2D.Frame(1, 1, 0, 0);
            }

            Vector2 origin = rectangle.Size() / 2f;
            Vector2 position = new Rectangle(Rectangle.X, Rectangle.Y, (int)(Rectangle.Width * Scale(false)),
                (int)(Rectangle.Height * Scale(false))).Center.ToVector2();

            spriteBatch.Draw(
                texture2D,
                position,
                new Rectangle?(rectangle),
                Color.White * ItemOpacity,
                0f,
                origin,
                Scale(true),
                SpriteEffects.None,
                0f);

            if(Item.stack > 1) {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    Main.fontItemStack,
                    Item.stack.ToString(),
                    RelativePosition + new Vector2(9f, 22f) * Scale(false),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    new Vector2(Scale(true)),
                    -1f,
                    Scale(false));
            }
        }

        /// <summary>
        /// Draws the visibility tick in the slot.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawTick(SpriteBatch spriteBatch) {
            Texture2D tickTexture = Main.inventoryTickOnTexture;

            if(!ItemVisible) {
                tickTexture = Main.inventoryTickOffTexture;
            }

            tickRect = new Rectangle(Rectangle.Right - 18, Rectangle.Top - 2, tickTexture.Width, tickTexture.Height);
            spriteBatch.Draw(tickTexture, tickRect, Color.White * 0.7f);
        }

        /// <summary>
        /// Checks if the slot has a context that needs a tick.
        /// </summary>
        /// <returns>whether the slot has a tick</returns>
        public bool HasTick() {
            if(Context == ItemSlot.Context.EquipAccessory ||
               Context == ItemSlot.Context.EquipLight ||
               Context == ItemSlot.Context.EquipPet) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the scale of the UIItemSlot.
        /// </summary>
        /// <param name="scaleForSlot">whether to also scale for the slot size</param>
        /// <returns>scale</returns>
        public float Scale(bool scaleForSlot) {
            float scale = (ScaleToInventory ? Main.inventoryScale : 1f);

            if(scaleForSlot) {
                scale *= (Size.X / defaultSize);
            }

            return scale;
        }
    }
}
