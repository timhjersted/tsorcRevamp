using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions.PermanentPotions;
using tsorcRevamp.Buffs;
using System;

namespace tsorcRevamp {
    public class tsorcRevampWorld : ModWorld {

        public static bool DownedSorrow;
        public static bool DownedHunter;
        public static bool DownedRage;

        public override TagCompound Save() {
            var downed = new List<string>();
            if (DownedSorrow) {
                downed.Add("Sorrow");
            }
            if (DownedHunter) {
                downed.Add("Hunter");
            }
            if (DownedSorrow) {
                downed.Add("Rage");
            }

            return new TagCompound {
                {"downed", downed }
            };
        }

    }
}