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
        public static Dictionary<int, int> Slain;

        public override void Initialize() {
            Slain = new Dictionary<int, int>();
        }


		public override TagCompound Save() {
			TagCompound tagCompound = new TagCompound
			{

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
    }
}