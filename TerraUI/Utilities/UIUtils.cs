using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using TerraUI.Objects;

namespace TerraUI.Utilities {
    public static class UIUtils {
        /// <summary>
        /// The mod that uses TerraUI.
        /// </summary>
        public static Mod Mod { get; set; }
        /// <summary>
        /// The subdirectory that TerraUI is stored in, if it is not in the mod's base folder. Used to determine where to load UI
        /// textures from.
        /// Example: Addons/TerraUI
        /// </summary>
        public static string Subdirectory { get; set; }

        /// <summary>
        /// Returns a Texture2D with the specified name from the Textures directory.
        /// </summary>
        /// <param name="texture">texture name without extension</param>
        /// <returns>Texture2D</returns>
        public static Texture2D GetTexture(string texture) {
            string tex = "";
            string subdir = (string.IsNullOrEmpty(Subdirectory) ? "" : Subdirectory.Replace(@"\", "/"));

            if(!string.IsNullOrWhiteSpace(subdir)) {
                tex += subdir;

                if(!subdir.Substring(subdir.Length - 1, 1).Equals("/")) {
                    tex += "/";
                }
            }

            tex += "Textures/" + texture;

            return Mod.GetTexture(tex);
        }

        /// <summary>
        /// Check if any children of the object intersect a chosen rectangle.
        /// </summary>
        /// <param name="obj">parent object</param>
        /// <param name="rect">intersection rectangle</param>
        /// <returns>true if no children intersect the rectangle</returns>
        public static bool NoChildrenIntersect(UIObject obj, Rectangle rect) {
            bool flag = true;

            foreach(UIObject ob in obj.Children) {
                if(ob.GetType() != typeof(UILabel)) {
                    if(ob.Rectangle.Intersects(rect)) {
                        flag = false;
                    }
                }
            }

            return flag;
        }

        /// <summary>
        /// Get the texture of a slot based on its context.
        /// </summary>
        /// <param name="context">slot context</param>
        /// <returns>texture of the slot</returns>
        public static Texture2D GetContextTexture(int context) {
            switch(context) {
                case ItemSlot.Context.EquipAccessory:
                case ItemSlot.Context.EquipArmor:
                case ItemSlot.Context.EquipGrapple:
                case ItemSlot.Context.EquipMount:
                case ItemSlot.Context.EquipMinecart:
                case ItemSlot.Context.EquipPet:
                case ItemSlot.Context.EquipLight:
                    return Main.inventoryBack3Texture;
                case ItemSlot.Context.EquipArmorVanity:
                case ItemSlot.Context.EquipAccessoryVanity:
                    return Main.inventoryBack8Texture;
                case ItemSlot.Context.EquipDye:
                    return Main.inventoryBack12Texture;
                case ItemSlot.Context.ChestItem:
                    return Main.inventoryBack5Texture;
                case ItemSlot.Context.BankItem:
                    return Main.inventoryBack2Texture;
                case ItemSlot.Context.GuideItem:
                case ItemSlot.Context.PrefixItem:
                case ItemSlot.Context.CraftingMaterial:
                    return Main.inventoryBack4Texture;
                case ItemSlot.Context.TrashItem:
                    return Main.inventoryBack7Texture;
                case ItemSlot.Context.ShopItem:
                    return Main.inventoryBack6Texture;
                default:
                    return Main.inventoryBackTexture;
            }
        }

        /// <summary>
        /// Get the hover text of a slot based on its context and the current language.
        /// </summary>
        /// <param name="context">context of the slot</param>
        /// <returns>text in current language</returns>
        public static string GetHoverText(int context) {
            switch(context) {
                case ItemSlot.Context.EquipAccessory:
                    return Language.GetTextValue("LegacyInterface.9");
                case ItemSlot.Context.EquipAccessoryVanity:
                    return Language.GetTextValue("LegacyInterface.11") + " " + Language.GetTextValue("LegacyInterface.9");
                case ItemSlot.Context.EquipDye:
                    return Language.GetTextValue("LegacyInterface.57");
                case ItemSlot.Context.EquipGrapple:
                    return Language.GetTextValue("LegacyInterface.90");
                case ItemSlot.Context.EquipLight:
                    return Language.GetTextValue("LegacyInterface.94");
                case ItemSlot.Context.EquipMinecart:
                    return Language.GetTextValue("LegacyInterface.93");
                case ItemSlot.Context.EquipMount:
                    return Language.GetTextValue("LegacyInterface.91");
                case ItemSlot.Context.EquipPet:
                    return Language.GetTextValue("LegacyInterface.92");
                case ItemSlot.Context.InventoryAmmo:
                    return Language.GetTextValue("LegacyInterface.27");
                case ItemSlot.Context.InventoryCoin:
                    return Language.GetTextValue("LegacyInterface.26");
            }

            return string.Empty;
        }

        /// <summary>
        /// Switch two items.
        /// </summary>
        /// <param name="item1">first item</param>
        /// <param name="item2">second item</param>
        public static void SwitchItems(ref Item item1, ref Item item2) {
            if((item1.type == 0 || item1.stack < 1) && (item2.type != 0 || item2.stack > 0)) //if item2 is mouseitem, then if item slot is empty and item is picked up
            {
                item1 = item2;
                item2 = new Item();
                item2.SetDefaults();
            }
            else if((item1.type != 0 || item1.stack > 0) && (item2.type == 0 || item2.stack < 1)) //if item2 is mouseitem, then if item slot is empty and item is picked up
            {
                item2 = item1;
                item1 = new Item();
                item1.SetDefaults();
            }
            else if((item1.type != 0 || item1.stack > 0) && (item2.type != 0 || item2.stack > 0)) //if item2 is mouseitem, then if item slot is empty and item is picked up
            {
                Item item3 = item2;
                item2 = item1;
                item1 = item3;
            }
        }

        /// <summary>
        /// Translate a keypress into a character.
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="shift">whether shift is pressed</param>
        /// <param name="capsLock">whether capslock is enabled</param>
        /// <param name="numLock">whether numlock is enabled</param>
        /// <returns>translated character</returns>
        public static string TranslateChar(Keys key, bool shift, bool capsLock, bool numLock) {
            switch(key) {
                case Keys.A:
                    return TranslateAlphabetic('a', shift, capsLock);
                case Keys.B:
                    return TranslateAlphabetic('b', shift, capsLock);
                case Keys.C:
                    return TranslateAlphabetic('c', shift, capsLock);
                case Keys.D:
                    return TranslateAlphabetic('d', shift, capsLock);
                case Keys.E:
                    return TranslateAlphabetic('e', shift, capsLock);
                case Keys.F:
                    return TranslateAlphabetic('f', shift, capsLock);
                case Keys.G:
                    return TranslateAlphabetic('g', shift, capsLock);
                case Keys.H:
                    return TranslateAlphabetic('h', shift, capsLock);
                case Keys.I:
                    return TranslateAlphabetic('i', shift, capsLock);
                case Keys.J:
                    return TranslateAlphabetic('j', shift, capsLock);
                case Keys.K:
                    return TranslateAlphabetic('k', shift, capsLock);
                case Keys.L:
                    return TranslateAlphabetic('l', shift, capsLock);
                case Keys.M:
                    return TranslateAlphabetic('m', shift, capsLock);
                case Keys.N:
                    return TranslateAlphabetic('n', shift, capsLock);
                case Keys.O:
                    return TranslateAlphabetic('o', shift, capsLock);
                case Keys.P:
                    return TranslateAlphabetic('p', shift, capsLock);
                case Keys.Q:
                    return TranslateAlphabetic('q', shift, capsLock);
                case Keys.R:
                    return TranslateAlphabetic('r', shift, capsLock);
                case Keys.S:
                    return TranslateAlphabetic('s', shift, capsLock);
                case Keys.T:
                    return TranslateAlphabetic('t', shift, capsLock);
                case Keys.U:
                    return TranslateAlphabetic('u', shift, capsLock);
                case Keys.V:
                    return TranslateAlphabetic('v', shift, capsLock);
                case Keys.W:
                    return TranslateAlphabetic('w', shift, capsLock);
                case Keys.X:
                    return TranslateAlphabetic('x', shift, capsLock);
                case Keys.Y:
                    return TranslateAlphabetic('y', shift, capsLock);
                case Keys.Z:
                    return TranslateAlphabetic('z', shift, capsLock);

                case Keys.D0:
                    return (shift) ? ")" : "0";
                case Keys.D1:
                    return (shift) ? "!" : "1";
                case Keys.D2:
                    return (shift) ? "@" : "2";
                case Keys.D3:
                    return (shift) ? "#" : "3";
                case Keys.D4:
                    return (shift) ? "$" : "4";
                case Keys.D5:
                    return (shift) ? "%" : "5";
                case Keys.D6:
                    return (shift) ? "^" : "6";
                case Keys.D7:
                    return (shift) ? "&" : "7";
                case Keys.D8:
                    return (shift) ? "*" : "8";
                case Keys.D9:
                    return (shift) ? "(" : "9";

                case Keys.Add:
                    return "+";
                case Keys.Divide:
                    return "/";
                case Keys.Multiply:
                    return "*";
                case Keys.Subtract:
                    return "-";

                case Keys.Space:
                    return " ";
                case Keys.Tab:
                    return "    ";

                case Keys.Decimal:
                    if(numLock && !shift)
                        return ".";
                    break;
                case Keys.NumPad0:
                    if(numLock && !shift)
                        return "0";
                    break;
                case Keys.NumPad1:
                    if(numLock && !shift)
                        return "1";
                    break;
                case Keys.NumPad2:
                    if(numLock && !shift)
                        return "2";
                    break;
                case Keys.NumPad3:
                    if(numLock && !shift)
                        return "3";
                    break;
                case Keys.NumPad4:
                    if(numLock && !shift)
                        return "4";
                    break;
                case Keys.NumPad5:
                    if(numLock && !shift)
                        return "5";
                    break;
                case Keys.NumPad6:
                    if(numLock && !shift)
                        return "6";
                    break;
                case Keys.NumPad7:
                    if(numLock && !shift)
                        return "7";
                    break;
                case Keys.NumPad8:
                    if(numLock && !shift)
                        return "8";
                    break;
                case Keys.NumPad9:
                    if(numLock && !shift)
                        return "9";
                    break;

                case Keys.OemBackslash:
                    return shift ? "|" : "\\";
                case Keys.OemCloseBrackets:
                    return shift ? "}" : "]";
                case Keys.OemComma:
                    return shift ? "<" : ",";
                case Keys.OemMinus:
                    return shift ? "_" : "-";
                case Keys.OemOpenBrackets:
                    return shift ? "{" : "[";
                case Keys.OemPeriod:
                    return shift ? ">" : ".";
                case Keys.OemPipe:
                    return shift ? "|" : "\\";
                case Keys.OemPlus:
                    return shift ? "+" : "=";
                case Keys.OemQuestion:
                    return shift ? "?" : "/";
                case Keys.OemQuotes:
                    return shift ? "\"" : "\'";
                case Keys.OemSemicolon:
                    return shift ? ":" : ";";
                case Keys.OemTilde:
                    return shift ? "~" : "`";
            }
            return "";
        }

        /// <summary>
        /// Helper function for TranslateChar().
        /// </summary>
        /// <param name="baseChar">original character</param>
        /// <param name="shift">whether shift is pressed</param>
        /// <param name="capsLock">whether capslock is enabled</param>
        /// <returns>translated character</returns>
        public static string TranslateAlphabetic(char baseChar, bool shift, bool capsLock) {
            return (capsLock ^ shift) ? string.Concat(char.ToUpper(baseChar)) : string.Concat(baseChar);
        }
    }
}
