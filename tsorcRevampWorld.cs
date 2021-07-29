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
using Microsoft.Xna.Framework.Input;

namespace tsorcRevamp {
    public class tsorcRevampWorld : ModWorld {

        public static bool DownedVortex;
        public static bool DownedNebula;
        public static bool DownedStardust;
        public static bool DownedSolar;
        public static bool SuperHardMode;
        public static bool TheEnd;

        public static Dictionary<int, int> Slain;

        public override void Initialize() {
            DownedVortex = false;
            DownedNebula = false;
            DownedStardust = false;
            DownedSolar = false;
            SuperHardMode = false;
            TheEnd = false;
            Slain = new Dictionary<int, int>();
        }

		public override TagCompound Save() {

            List<string> downed = new List<string>();
            if (DownedVortex) downed.Add("DownedVortex");
            if (DownedNebula) downed.Add("DownedNebula");
            if (DownedStardust) downed.Add("DownedStardust");
            if (DownedSolar) downed.Add("DownedSolar");

            List<string> world_state= new List<string>();
            if (SuperHardMode) world_state.Add("SuperHardMode");
            if (TheEnd) world_state.Add("TheEnd");

            TagCompound tagCompound = new TagCompound
			{
                {"downed", downed},
                {"world_state", world_state}
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

            IList<string> downedList = tag.GetList<string>("downed");
            DownedVortex = downedList.Contains("DownedVortex");
            DownedNebula = downedList.Contains("DownedNebula");
            DownedStardust = downedList.Contains("DownedStardust");
            DownedSolar = downedList.Contains("DownedSolar");

            IList<string> worldStateList = tag.GetList<string>("world_state");
            SuperHardMode = worldStateList.Contains("SuperHardMode");
            TheEnd = worldStateList.Contains("TheEnd");
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

        public static bool JustPressed(Keys key) {
            return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
        }

        public static void CampfireToBonfire() {
            for (int x = 0; x < Main.maxTilesX - 2; x++) {
                for (int y = 0; y < Main.maxTilesY - 2; y++) {
                    if (Main.tile[x, y].active() && Main.tile[x, y].type == TileID.Campfire) {
                        WorldGen.KillTile(x, y, false, false, true);
                        Dust.QuickBox(new Vector2(x + 1, y + 1) * 16, new Vector2(x + 2, y + 2) * 16, 2, Color.YellowGreen, null);
                        WorldGen.Place3x4(x + 1, y + 1, (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>(), 0);
                        foreach (Item item in Main.item) {
                            item.active = false; //delete ground items (in this case campfires)
                        }
                    }
                } 
            }
        }

        public override void PostUpdate() {
            if (JustPressed(Keys.Home) && JustPressed(Keys.NumPad0)) //they have to be pressed *on the same tick*. you can't hold one and then press the other.
                CampfireToBonfire();
            bool charm = false;
            foreach (Player p in Main.player) {
                for (int i = 3; i <= 8; i++) {
                    if (p.armor[i].type == ModContent.ItemType<Items.Accessories.CovenantOfArtorias>()) {
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
            if (!Main.dedServ) {
                if (SuperHardMode) {
                    for (int i = 0; i < Main.moonTexture.Length; i++) {
                        Main.moonTexture[i] = mod.GetTexture("Textures/SHMMoon");
                    }
                    Main.sunTexture = mod.GetTexture("Textures/SHMSun1");
                    Main.sun2Texture = mod.GetTexture("Textures/SHMSun2");
                    Main.sun3Texture = mod.GetTexture("Textures/SHMSun1");
                }
                if (TheEnd) { //super hardmode and the end are mutually exclusive, so there won't be any "z-fighting", but this still feels silly
                    Main.sunTexture = ModContent.GetTexture("Terraria/Sun");
                    Main.sun2Texture = ModContent.GetTexture("Terraria/Sun2");
                    Main.sun3Texture = ModContent.GetTexture("Terraria/Sun3");
                    for (int i = 0; i < Main.moonTexture.Length; i++) {
                        Main.moonTexture[i] = ModContent.GetTexture("Terraria/Moon_" + i);
                    }
                }
            }
        }
    }
}