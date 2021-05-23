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

        public static bool DownedVortex;
        public static bool DownedNebula;
        public static bool DownedStardust;
        public static bool DownedSolar;
        public static Dictionary<int, int> Slain;

        public static bool DownedOkiku;
        public override void Initialize() {
            Slain = new Dictionary<int, int>();
        }

		public override TagCompound Save() {
			TagCompound tagCompound = new TagCompound
			{
                {"DownedVortex", DownedVortex},
                {"DownedNebula", DownedNebula},
                {"DownedStardust", DownedStardust},
                {"DownedSolar", DownedSolar},
            };
			SaveSlain(tagCompound);
			return tagCompound;
		}

		private void SaveSlain(TagCompound tag) {
            tag.Add("type", Slain.Keys.ToList());
            tag.Add("value", Slain.Values.ToList());
        }

        public override void Load(TagCompound tag) {
            LoadSlain(tag);
            tag.GetBool("DownedVortex");
            tag.GetBool("DownedNebula");
            tag.GetBool("DownedStardust");
            tag.GetBool("DownedSolar");
        }

        private void LoadSlain(TagCompound tag) {
            if (tag.ContainsKey("type")) {
                List<int> list = tag.Get<List<int>>("type");
                List<int> list2 = tag.Get<List<int>>("value");
                for (int i = 0; i < list.Count; i++) {
                    Slain.Add(list[i], list2[i]);
                }
            }
        }

        public override void PostUpdate() {
            bool charm = false;
            foreach (Player p in Main.player) {
                foreach (Item i in p.armor) {
                    if (i.type == ModContent.ItemType<Items.Accessories.CovenantOfArtorias>()) {
                        charm = true;
                        break;
                    }
                }
            }
            if (charm) {
                Main.bloodMoon = true;
                Main.moonPhase = 0;
                Main.dayTime = false;
                Main.time = 16240.0;
            }
        }
    }
}