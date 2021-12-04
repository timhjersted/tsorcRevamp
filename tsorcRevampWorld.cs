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
using Terraria.GameContent.NetModules;
using Terraria.Localization;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

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

            tsorcScriptedEvents.InitializeScriptedEvents();
        }

		public override TagCompound Save() {

            List<string> downed = new List<string>();

            if (DownedVortex) downed.Add("DownedVortex");
            if (DownedNebula) downed.Add("DownedNebula");
            if (DownedStardust) downed.Add("DownedStardust");
            if (DownedSolar) downed.Add("DownedSolar");

            List<string> world_state= new List<string>();
            if (SuperHardMode) world_state.Add("SuperHardMode");
            //This saves the fact that SuperHardMode has been disabled
            if(world_state.Contains("SuperHardMode") && !SuperHardMode)
            {
                world_state.Remove("SuperHardMode");
            }
            if (TheEnd) world_state.Add("TheEnd");

            TagCompound tagCompound = new TagCompound
			{
                {"downed", downed},
                {"world_state", world_state},
            };
			SaveSlain(tagCompound);
            tsorcScriptedEvents.SaveScriptedEvents(tagCompound);
            return tagCompound;
		}

		private void SaveSlain(TagCompound tag) {
            tag.Add("type", Slain.Keys.ToList());
            tag.Add("value", Slain.Values.ToList());
        }

        public override void Load(TagCompound tag)
        {
            LoadSlain(tag);
            tsorcScriptedEvents.LoadScriptedEvents(tag);

            IList<string> downedList = tag.GetList<string>("downed");
            DownedVortex = downedList.Contains("DownedVortex");
            DownedNebula = downedList.Contains("DownedNebula");
            DownedStardust = downedList.Contains("DownedStardust");
            DownedSolar = downedList.Contains("DownedSolar");

            IList<string> worldStateList = tag.GetList<string>("world_state");
            SuperHardMode = worldStateList.Contains("SuperHardMode");
            TheEnd = worldStateList.Contains("TheEnd");

            //If the player leaves the world or turns off their computer in the middle of the fight or whatever, this will de-actuate the pyramid for them next time they load
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                if (Main.tile[5810, 1670] != null)
                {
                    if (Main.tile[5810, 1670].active() && Main.tile[5810, 1670].inActive())
                    {
                        NPCs.Bosses.SuperHardMode.DarkCloud.ActuatePyramid();
                    }
                }
            }
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

        public override void NetSend(BinaryWriter writer)
        {
            if(Main.netMode == NetmodeID.Server)
            {
                //Storing it in an int32 just so its exact type is guranteed, since that does matter
                int slainSize = Slain.Count;
                writer.Write(slainSize);
                foreach (KeyValuePair<int, int> pair in Slain)
                {
                    //Fuck it, i'm encoding each entry of slain as a Vector2. It's probably more sane than doing it byte by byte.
                    writer.WriteVector2(new Vector2(pair.Key, pair.Value));
                }
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            int slainSize = reader.ReadInt32();
            for (int i = 0; i < slainSize; i++)
            {
                Vector2 readData = reader.ReadVector2();
                if (Slain.ContainsKey((int)readData.X))
                {
                    Slain[(int)readData.X] = (int)readData.Y;
                }
                else
                {
                    Slain.Add((int)readData.X, (int)readData.Y);
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
                            if (item.type == ItemID.Campfire)
                            {
                                item.active = false; //delete ground items (in this case campfires)
                            }
                        }
                    }
                } 
            }
        }
        Texture2D SHMSun1 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun1");
        Texture2D SHMSun2 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun2");
        Texture2D SHMSun3 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun1");
        Texture2D SHMMoon = ModContent.GetTexture("tsorcRevamp/Textures/SHMMoon");
        Texture2D VanillaSun1 = ModContent.GetTexture("Terraria/Sun");
        Texture2D VanillaSun2 = ModContent.GetTexture("Terraria/Sun2");
        Texture2D VanillaSun3 = ModContent.GetTexture("Terraria/Sun3");
        List<Texture2D> VanillaMoonTextures;

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
                if (Main.GlobalTime % 120 == 0 && Main.netMode != NetmodeID.SinglePlayer) {
                    //globaltime always ticks up unless the player is in camera mode, and lets be honest: who uses camera mode? 
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
            if (!Main.dedServ) {
                if (SuperHardMode) {
                    for (int i = 0; i < Main.moonTexture.Length; i++) {
                        Main.moonTexture[i] = SHMMoon;
                    }
                    Main.sunTexture = SHMSun1;
                    Main.sun2Texture = SHMSun2;
                    Main.sun3Texture = SHMSun3;
                }
                if (TheEnd) { //super hardmode and the end are mutually exclusive, so there won't be any "z-fighting", but this still feels silly
                    Main.sunTexture = VanillaSun1;
                    Main.sun2Texture = VanillaSun2;
                    Main.sun3Texture = VanillaSun3;
                    if (VanillaMoonTextures == null)
                    {
                        VanillaMoonTextures = new List<Texture2D>();
                        for (int i = 0; i < Main.moonTexture.Length; i++)
                        {
                            VanillaMoonTextures.Add(ModContent.GetTexture("Terraria/Moon_" + i));
                        }
                    }
                    for (int i = 0; i < Main.moonTexture.Length; i++) {
                        Main.moonTexture[i] = VanillaMoonTextures[i];
                    }
                }
            }
        }

        public static void AdventureModeCheck()
        {

        }

        //Called upon the death of Gwyn, Lord of Cinder. Disables both hardmode and superhardmode, and sets the world state to "The End".
        public static void InitiateTheEnd()
        {
            Color c = new Color(255f, 255f, 60f);
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (Main.netMode == 0)
                {
                    Main.NewText("The portal from The Abyss has closed!", c);
                    Main.NewText("The world has been healed. Attraidies' sway over the world has finally ended!", c);
                }
                else if (Main.netMode == 2)
                {
                    NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("The portal from The Abyss has closed!"), c);
                    NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("The world has been healed. Attraidies' sway over the world has finally ended!"), c);
                }
            }
            else
            {
                if (Main.netMode == 0)
                {
                    Main.NewText("You have vanquished the final guardian...", c);
                    Main.NewText("The portal from The Abyss remains closed. All is at peace...", c);
                }
                else if (Main.netMode == 2)
                {
                    NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("You have vanquished the final guardian..."), c);
                    NetTextModule.SerializeServerMessage(NetworkText.FromLiteral("The portal from The Abyss remains closed. All is at peace..."), c);
                }
            }
            
            //These are outside of the if statements just so players can still disable hardmode or superhardmode if they happen to activate them again.
            Main.hardMode = false;
            tsorcRevampWorld.SuperHardMode = false;
            tsorcRevampWorld.TheEnd = true;

            //		Main.NewText("You have vanquished the final guardian of the Abyss...");
            //		Main.NewText("The kiln of the First Flame has been ignited!");
            //		//Main.NewText("Congratulations, you have inherited the fire of this world. You will forever be known as the hero of the age.");  
        }
    }
}