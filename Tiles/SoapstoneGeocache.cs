using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Tiles {
    public class SoapstoneMessage {
        public static List<SoapstoneMessage> SoapstoneList;

        public string text;
        public Point16 location;

        public SoapstoneMessage(string text, Point16 location) {
            this.text = text;
            this.location = location;
        }   

        public static void InitSoapstones() {
            SoapstoneList ??= new();

            SoapstoneList.Add(new SoapstoneMessage("Testing soapstone system. hello! i just need this to run out to multiple lines", new Point16(4950, 865)));


        }
    }
}
