using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Tiles {
    public enum SoapstoneStyle {
        Dialogue = 1,
    }
    public class SoapstoneMessage {
        public static List<SoapstoneMessage> SoapstoneList;

        public string text;
        public Point16 location;
        public int textWidth;
        public SoapstoneStyle style;

        public const int DEFAULT_WIDTH = 320;
        public const SoapstoneStyle DEFAULT_STYLE = SoapstoneStyle.Dialogue;

        public SoapstoneMessage(string text, Point16 location, int textWidth = DEFAULT_WIDTH, SoapstoneStyle style = DEFAULT_STYLE) {
            this.text = text;
            this.location = location;
            this.textWidth = textWidth;
            this.style = style;
        }   

        public static void InitSoapstones() {
            SoapstoneList = new();
            

            //SoapstoneList.Add(new SoapstoneMessage("Glowing green skulls and hanging skeletons hold important secrets. Right-click to discover.", new Point16(4278, 951)));
            SoapstoneList.Add(new SoapstoneMessage(LaUtils.GetTextValue("UI.Soapstone"), new Point16(4960, 878)));

            //Use the Recipe Browser mod to see what can be crafted with Dark Souls. Anything that says it's a DS crafting material should be investigated.

            /*
                         "The dodgeroll makes you immune to attacks. Mastering it is necessary for survival!",
                         
                         "The dodgeroll keybind can be changed in Controls. Try Left Shift, R or whatever works for you.",
                         "You can dodge mid-air to avoid attacks and reverse your momentum instantly...",
                         "Crystal shards often hint at secrets, chests or hidden paths nearby...",
                         "Using potions during normal combat is often necessary to survive",
                         "Losing souls on death can be disabled in the config",
                         "Enemies in certain dark areas drop Humanity, which can restore HP lost to curses",
                         "Item tooltips often have information vital to your survival...",                        
                         "Progression clues can be found on unique items dropped by bosses",                        
                         "Signs offer many important hints",
                         "Teal Pressure Pads can only be activated with ranged attacks",
                         "Some vanilla recipes have been removed. You must find these items in the world by exploring",
                         "Get a bad modifier on a weapon? Talk to Jade, the Emerald Herald. She can remove it with her Blessing",
                         //"Grey Stone Gates cannot be broken and must be opened by a lever or switch",
                         "You may encounter bosses before you can defeat them. If in doubt, come back when you're stronger",
                         "Play with the Recipe Browser mod to easily see what your loot can be transformed into",
                         "Welcome to the Dark Souls of Terraria",
                         "The Chloranthy Ring massively improves your dodgeroll, allowing superior evasion...",
                         "Teal pressure plates must be hit with a ranged weapon to activate",
            */
        }
    }

    //exists only so vanilla signs can be de/serialized from/to json
    public class SignJSONSerializable {
        public string text { get; set; }
        public int tileX { get; set; }
        public int tileY { get; set; }
        public int textWidth { get; set; }
        public SoapstoneStyle style { get; set; }
    }
}
