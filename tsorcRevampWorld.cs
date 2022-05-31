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
using tsorcRevamp.UI;

namespace tsorcRevamp {
    public class tsorcRevampWorld : ModWorld {

        public static bool DownedVortex;
        public static bool DownedNebula;
        public static bool DownedStardust;
        public static bool DownedSolar;
        public static bool SuperHardMode;
        public static bool TheEnd;
        public static bool CustomMap;

        public static Dictionary<int, int> Slain;

        public static List<Vector2> LitBonfireList;

       

        public override void Initialize() {
            DownedVortex = false;
            DownedNebula = false;
            DownedStardust = false;
            DownedSolar = false;
            SuperHardMode = false;
            TheEnd = false;
            CustomMap = false;
            Slain = new Dictionary<int, int>();
            LitBonfireList = new List<Vector2>();
            initialized = false;

            tsorcScriptedEvents.InitializeScriptedEvents();
            Tiles.SoulSkellyGeocache.InitializeSkellys();
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
            if (CustomMap) world_state.Add("CustomMap");

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
            CustomMap = worldStateList.Contains("CustomMap");

            //Faisafe. Checks some blocks near the top of one of the Wyvern Mage's tower that are unlikely to change. Even if they do, this shouldn't be necessary though. It's purely to be safe.
            if (Framing.GetTileSafely(7102, 137).TileType == 54 && Framing.GetTileSafely(7103, 137).TileType == 357 && Framing.GetTileSafely(7104, 136).TileType == 357 && Framing.GetTileSafely(7105, 136).TileType == 197) {
                CustomMap = true;
            }

            LitBonfireList = GetActiveBonfires();
            

            //If the player leaves the world or turns off their computer in the middle of the fight or whatever, this will de-actuate the pyramid for them next time they load
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                if (Main.tile[5810, 1670] != null)
                {
                    if (Main.tile[5810, 1670].HasTile && Main.tile[5810, 1670].IsActuated)
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
                writer.Write(CustomMap);
                writer.Write(SuperHardMode);

                //Storing it in an int32 just so its exact type is guranteed, since that does matter
                int slainSize = Slain.Count;
                writer.Write(slainSize);
                foreach (KeyValuePair<int, int> pair in Slain)
                {
                    //Fuck it, i'm encoding each entry of slain as a Vector2. It's probably more sane than doing it byte by byte.
                    writer.WriteVector2(new Vector2(pair.Key, pair.Value));
                }

                int bonfireSize = LitBonfireList.Count;
                writer.Write(bonfireSize);
                foreach (Vector2 bonfire in LitBonfireList)
                {
                    //Fuck it, i'm encoding each entry of slain as a Vector2. It's probably more sane than doing it byte by byte.
                    writer.WriteVector2(bonfire);
                }
            }
        }

        public override void NetReceive(BinaryReader reader)
        {
            CustomMap = reader.ReadBoolean();
            SuperHardMode = reader.ReadBoolean();

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

            int bonfireSize = reader.ReadInt32();
            if(LitBonfireList == null)
            {
                LitBonfireList = new List<Vector2>();
            }

            for (int i = 0; i < bonfireSize; i++)
            {
                Vector2 bonfire = reader.ReadVector2();
                if (!LitBonfireList.Contains(bonfire))
                {
                    LitBonfireList.Add(bonfire);
                }
            }
        }

        public static bool JustPressed(Keys key) {
            return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
        }


        #region CampfireToBonfire (Is also Skelly Loot Cache replacement code)

        public static void CampfireToBonfire() {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            for (int x = 0; x < Main.maxTilesX - 2; x++) {
                for (int y = 0; y < Main.maxTilesY - 2; y++) {

                    //Campfire to Bonfire
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Campfire) {

                        //kill the space above the campfire, to remove vines and such
                        for (int q = 0; q < 3; q++) {
                            for (int w = -2; w < 2; w++) {
                                WorldGen.KillTile(x + q, y + w, false, false, true);  
                            }
                        }
                        Dust.QuickBox(new Vector2(x + 1, y + 1) * 16, new Vector2(x + 2, y + 2) * 16, 2, Color.YellowGreen, null);
                        //WorldGen.Place3x4(x + 1, y + 1, (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>(), 0);

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>();
                        //reimplement WorldGen.Place3x4 minus SolidTile2 checking because this game is fucked 
                        {
                            if (x+1 < 5 || x + 1 > Main.maxTilesX - 5 || y + 1 < 5 || y + 1 > Main.maxTilesY - 5) {
                                return;
                            }
                            bool flag = true;
                            for (int i = x + 1 - 1; i < x + 1 + 2; i++) {
                                for (int j = y + 1 - 3; j < y + 1 + 1; j++) {
                                    if (Main.tile[i, j] == null) {
                                        Main.tile[i, j] = new Tile();
                                    }
                                    if (Main.tile[i, j].HasTile) {
                                        flag = false;
                                    }
                                }
                                if (Main.tile[i, y + 1 + 1] == null) {
                                    Main.tile[i, y + 1 + 1] = new Tile();
                                }
                            }
                            if (flag) {
                                int num = style * 54;
                                for (int k = -3; k <= 0; k++) {
                                    short frameY = (short)((3 + k) * 18);
                                    Main.tile[x + 1 - 1, y + 1 + k].HasTile = true;
                                    Main.tile[x + 1 - 1, y + 1 + k].TileFrameY = frameY;
                                    Main.tile[x + 1 - 1, y + 1 + k].TileFrameX = (short)num;
                                    Main.tile[x + 1 - 1, y + 1 + k].TileType = type;
                                    Main.tile[x + 1, y + 1 + k].HasTile = true;
                                    Main.tile[x + 1, y + 1 + k].TileFrameY = frameY;
                                    Main.tile[x + 1, y + 1 + k].TileFrameX = (short)(num + 18);
                                    Main.tile[x + 1, y + 1 + k].TileType = type;
                                    Main.tile[x + 1 + 1, y + 1 + k].HasTile = true;
                                    Main.tile[x + 1 + 1, y + 1 + k].TileFrameY = frameY;
                                    Main.tile[x + 1 + 1, y + 1 + k].TileFrameX = (short)(num + 36);
                                    Main.tile[x + 1 + 1, y + 1 + k].TileType = type;
                                }
                            }

                        }
                    }

                    //Slime blocks to SkullLeft - SlimeBlock-PinkSlimeBlock (I tried to stick right and lefts together but the code refuses to work for both, I swear I'm not just being dumb) 
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.PinkSlimeBlock && Main.tile[x - 1, y].TileType == TileID.SlimeBlock) 
                    {

                        //kill the space the skull occupies, to remove vines and such
                        for (int q = -1; q < 1; q++)
                        {
                            for (int w = -1; w < 1; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }
                        //WorldGen.Place3x4(x + 1, y + 1, (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>(), 0);

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkullL>();
                        //reimplement WorldGen.Place2x2 minus SolidTile2 checking
                        if (x < 5 || x > Main.maxTilesX - 5 || y < 5 || y > Main.maxTilesY - 5) {
                            return;
                        }
                        short num = 0;
                        bool flag = true;
                        for (int i = x - 1; i < x + 1; i++) {
                            for (int j = y - 1; j < y + 1; j++) {
                                if (Main.tile[i, j] == null) {
                                    Main.tile[i, j] = new Tile();
                                }
                                if (Main.tile[i, j].HasTile) {
                                    flag = false;
                                }
                            }
                            if (Main.tile[i, y + 1] == null) {
                                Main.tile[i, y + 1] = new Tile();
                            }
                        }
                        if (flag) {
                            short num2 = (short)(36 * style);
                            Main.tile[x - 1, y - 1].HasTile = true;
                            Main.tile[x - 1, y - 1].TileFrameY = num;
                            Main.tile[x - 1, y - 1].TileFrameX = num2;
                            Main.tile[x - 1, y - 1].TileType = type;
                            Main.tile[x, y - 1].HasTile = true;
                            Main.tile[x, y - 1].TileFrameY = num;
                            Main.tile[x, y - 1].TileFrameX = (short)(num2 + 18);
                            Main.tile[x, y - 1].TileType = type;
                            Main.tile[x - 1, y].HasTile = true;
                            Main.tile[x - 1, y].TileFrameY = (short)(num + 18);
                            Main.tile[x - 1, y].TileFrameX = num2;
                            Main.tile[x - 1, y].TileType = type;
                            Main.tile[x, y].HasTile = true;
                            Main.tile[x, y].TileFrameY = (short)(num + 18);
                            Main.tile[x, y].TileFrameX = (short)(num2 + 18);
                            Main.tile[x, y].TileType = type;
                        }
                    }

                    //Slime block to SkullRight - PinkSlimeBlock-SlimeBlock
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.SlimeBlock && Main.tile[x - 1, y].TileType == TileID.PinkSlimeBlock)
                    {

                        //kill the space the skull occupies, to remove vines and such
                        for (int q = -1; q < 1; q++)
                        {
                            for (int w = -1; w < 1; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }
                        //WorldGen.Place3x4(x + 1, y + 1, (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>(), 0);

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkullR>();
                        //reimplement WorldGen.Place2x2 minus SolidTile2 checking
                        if (x < 5 || x > Main.maxTilesX - 5 || y < 5 || y > Main.maxTilesY - 5)
                        {
                            return;
                        }
                        short num = 0;
                        bool flag = true;
                        for (int i = x - 1; i < x + 1; i++)
                        {
                            for (int j = y - 1; j < y + 1; j++)
                            {
                                if (Main.tile[i, j] == null)
                                {
                                    Main.tile[i, j] = new Tile();
                                }
                                if (Main.tile[i, j].HasTile)
                                {
                                    flag = false;
                                }
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1] = new Tile();
                            }
                        }
                        if (flag)
                        {
                            short num2 = (short)(36 * style);
                            Main.tile[x - 1, y - 1].HasTile = true;
                            Main.tile[x - 1, y - 1].TileFrameY = num;
                            Main.tile[x - 1, y - 1].TileFrameX = num2;
                            Main.tile[x - 1, y - 1].TileType = type;
                            Main.tile[x, y - 1].HasTile = true;
                            Main.tile[x, y - 1].TileFrameY = num;
                            Main.tile[x, y - 1].TileFrameX = (short)(num2 + 18);
                            Main.tile[x, y - 1].TileType = type;
                            Main.tile[x - 1, y].HasTile = true;
                            Main.tile[x - 1, y].TileFrameY = (short)(num + 18);
                            Main.tile[x - 1, y].TileFrameX = num2;
                            Main.tile[x - 1, y].TileType = type;
                            Main.tile[x, y].HasTile = true;
                            Main.tile[x, y].TileFrameY = (short)(num + 18);
                            Main.tile[x, y].TileFrameX = (short)(num2 + 18);
                            Main.tile[x, y].TileType = type;
                        }
                    }

                    //Stucco blocks to SkellyLeft - GreyStucco-GreenStuccoBlock-GreyStuccoBlock
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.GreenStucco && Main.tile[x + 1, y].TileType == TileID.GrayStucco && Main.tile[x - 1, y].TileType == TileID.GrayStucco)
                    {

                        //kill the space the skelly occupies, to remove vines and such
                        for (int q = -1; q < 2; q++)
                        {
                            for (int w = 0; w < 1; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkellyL>();
                        //reimplement WorldGen.Place3x1 minus SolidTile2
                        if (x < 5 || x > Main.maxTilesX - 5 || y < 5 || y > Main.maxTilesY - 5)
                        {
                            return;
                        }
                        bool flag = true;
                        for (int i = x - 1; i < x + 2; i++)
                        {
                            if (Main.tile[i, y] == null)
                            {
                                Main.tile[i, y] = new Tile();
                            }
                            if (Main.tile[i, y].HasTile)
                            {
                                flag = false;
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1] = new Tile();
                            }
                        }
                        if (flag)
                        {
                            short num = (short)(54 * style);
                            Main.tile[x - 1, y].HasTile = true;
                            Main.tile[x - 1, y].TileFrameY = 0;
                            Main.tile[x - 1, y].TileFrameX = num;
                            Main.tile[x - 1, y].TileType = type;
                            Main.tile[x, y].HasTile = true;
                            Main.tile[x, y].TileFrameY = 0;
                            Main.tile[x, y].TileFrameX = (short)(num + 18);
                            Main.tile[x, y].TileType = type;
                            Main.tile[x + 1, y].HasTile = true;
                            Main.tile[x + 1, y].TileFrameY = 0;
                            Main.tile[x + 1, y].TileFrameX = (short)(num + 36);
                            Main.tile[x + 1, y].TileType = type;
                        }
                    }

                    //Stucco blocks to SkellyRight - GreenStucco-GreyStuccoBlock-GreenStuccoBlock
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.GrayStucco && Main.tile[x + 1, y].TileType == TileID.GreenStucco && Main.tile[x - 1, y].TileType == TileID.GreenStucco)
                    {

                        //kill the space the skelly occupies, to remove vines and such
                        for (int q = -1; q < 2; q++)
                        {
                            for (int w = 0; w < 1; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkellyR>();
                        //reimplement WorldGen.Place3x1 minus SolidTile2
                        if (x < 5 || x > Main.maxTilesX - 5 || y < 5 || y > Main.maxTilesY - 5)
                        {
                            return;
                        }
                        bool flag = true;
                        for (int i = x - 1; i < x + 2; i++)
                        {
                            if (Main.tile[i, y] == null)
                            {
                                Main.tile[i, y] = new Tile();
                            }
                            if (Main.tile[i, y].HasTile)
                            {
                                flag = false;
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1] = new Tile();
                            }
                        }
                        if (flag)
                        {
                            short num = (short)(54 * style);
                            Main.tile[x - 1, y].HasTile = true;
                            Main.tile[x - 1, y].TileFrameY = 0;
                            Main.tile[x - 1, y].TileFrameX = num;
                            Main.tile[x - 1, y].TileType = type;
                            Main.tile[x, y].HasTile = true;
                            Main.tile[x, y].TileFrameY = 0;
                            Main.tile[x, y].TileFrameX = (short)(num + 18);
                            Main.tile[x, y].TileType = type;
                            Main.tile[x + 1, y].HasTile = true;
                            Main.tile[x + 1, y].TileFrameY = 0;
                            Main.tile[x + 1, y].TileFrameX = (short)(num + 36);
                            Main.tile[x + 1, y].TileType = type;
                        }
                    }

                    //Confetti blocks to SkellyHangingUp (wrists chained) - Confetti Block
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Confetti)
                    {

                        //kill the space the skelly occupies, to remove vines and such
                        for (int q = -1; q < 2; q++)
                        {
                            for (int w = -1; w < 2; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkellyWall1>();
                        //reimplement WorldGen.Place3x3Wall
                        int num = x - 1;
                        int num2 = y - 1;
                        bool flag = true;
                        for (int i = num; i < num + 3; i++)
                        {
                            for (int j = num2; j < num2 + 3; j++)
                            {
                                if (Main.tile[i, j].HasTile || Main.tile[i, j].WallType == 0)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (!flag)
                        {
                            return;
                        }
                        int num3 = 0;
                        while (style > 35)
                        {
                            num3++;
                            style -= 36;
                        }
                        int num4 = style * 54;
                        int num5 = num3 * 54;
                        for (int k = num; k < num + 3; k++)
                        {
                            for (int l = num2; l < num2 + 3; l++)
                            {
                                Main.tile[k, l].HasTile = true;
                                Main.tile[k, l].TileType = type;
                                Main.tile[k, l].TileFrameX = (short)(num4 + 18 * (k - num));
                                Main.tile[k, l].TileFrameY = (short)(num5 + 18 * (l - num2));
                            }
                        }
                    }

                    //Confetti blocks to SkellyHangingDown (ankles chained) - Confetti Black Block (aka Midnight Confetti Block)
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.ConfettiBlack)
                    {

                        //kill the space the skelly occupies, to remove vines and such
                        for (int q = -1; q < 2; q++)
                        {
                            for (int w = -1; w < 2; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.SoulSkellyWall2>();
                        //reimplement WorldGen.Place3x3Wall
                        int num = x - 1;
                        int num2 = y - 1;
                        bool flag = true;
                        for (int i = num; i < num + 3; i++)
                        {
                            for (int j = num2; j < num2 + 3; j++)
                            {
                                if (Main.tile[i, j].HasTile || Main.tile[i, j].WallType == 0)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (!flag)
                        {
                            return;
                        }
                        int num3 = 0;
                        while (style > 35)
                        {
                            num3++;
                            style -= 36;
                        }
                        int num4 = style * 54;
                        int num5 = num3 * 54;
                        for (int k = num; k < num + 3; k++)
                        {
                            for (int l = num2; l < num2 + 3; l++)
                            {
                                Main.tile[k, l].HasTile = true;
                                Main.tile[k, l].TileType = type;
                                Main.tile[k, l].TileFrameX = (short)(num4 + 18 * (k - num));
                                Main.tile[k, l].TileFrameY = (short)(num5 + 18 * (l - num2));
                            }
                        }
                    }

                }
            }
            for (int i = 0; i < 400; i++) {
                if (Main.item[i].type == ItemID.Campfire && Main.item[i].active) {
                    Main.item[i].active = false; //delete ground items (in this case campfires)
                }
            }
        }

        #endregion
        public static List<Vector2> GetActiveBonfires()
        {
            List<Vector2> BonfireList = new List<Vector2>();
            int bonfireType = ModContent.TileType<Tiles.BonfireCheckpoint>();

            //Only check every 3 tiles horizontally and every 4 vertically, since bonfires are 3x4 and we only want to add each of them once
            for (int i = 1; i < (Main.tile.GetUpperBound(0) - 1); i += 3)
            {
                for (int j = 1; j < (Main.tile.GetUpperBound(1) - 1); j += 4)
                {
                    //Check if each tile is a bonfire, and is the left frame, and does not have a bonfire above it (aka just the top left tile of a bonfire)
                    if (Main.tile[i, j] != null && Main.tile[i, j].HasTile && Main.tile[i, j].TileType == bonfireType)
                    {
                        if (Main.tile[i, j].TileFrameY / 74 != 0)
                        {
                            BonfireList.Add(new Vector2(i, j));
                        }
                    }
                }
            }

            return BonfireList;
        }

        Texture2D SHMSun1 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun1");
        Texture2D SHMSun2 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun2");
        Texture2D SHMSun3 = ModContent.GetTexture("tsorcRevamp/Textures/SHMSun1");
        Texture2D SHMMoon = ModContent.GetTexture("tsorcRevamp/Textures/SHMMoon");
        Texture2D VanillaSun1 = ModContent.GetTexture("Terraria/Sun");
        Texture2D VanillaSun2 = ModContent.GetTexture("Terraria/Sun2");
        Texture2D VanillaSun3 = ModContent.GetTexture("Terraria/Sun3");
        List<Texture2D> VanillaMoonTextures;

        //MAKE CATACOMBS DUNGEON BIOME - This code was blocking spawns in the catacombs, but catacombs now works as dungeon without it likely
        //because of other code improving dungeon spawn detection

        //public override void TileCountsAvailable(int[] tileCounts) {
        //Main.dungeonTiles += tileCounts[TileID.BoneBlock];
        //Main.dungeonTiles += tileCounts[TileID.MeteoriteBrick];

        //}

        bool initialized = false;
        public override void PreUpdate()
        {
            //Only do this on the first tick after loading
            if (!initialized)
            {
                initialized = true;
                if (CheckForCustomMap())
                {
                    //This is done like this so that it can never set CustomMap to false, since that isn't what that function returning false means.
                    CustomMap = true;
                }

                //Stuff that should only be done on the custom map
                if (CustomMap)
                {
                    //Stuff that should be done by every client that joins
                    if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                    {
                        UsefulFunctions.BroadcastText("Custom map detected. Adventure Mode auto-enabled.", Color.GreenYellow);
                        ModContent.GetInstance<tsorcRevampConfig>().AdventureMode = true;
                    }
                    if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                    {
                        UsefulFunctions.BroadcastText("Warning!! The setting 'Adventure Mode: Recipes and Items' is disabled!!", Color.OrangeRed);
                        UsefulFunctions.BroadcastText("Having this off can break progression and parts of the map, please enable this setting and reload mods!", Color.OrangeRed);
                    }

                    //Stuff that should only be done by either a solo player or the server
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                        {
                            CampfireToBonfire();
                        }
                        if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID)
                        {
                            Main.worldID = Main.rand.Next(9999999);
                        }                        

                        //Spawn in NPCs
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.EmeraldHerald>()))
                        {
                            NPC.NewNPC(4510 * 16, 737 * 16, ModContent.NPCType<NPCs.Friendly.EmeraldHerald>());
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.Dwarf>()))
                        {
                            int npc = NPC.NewNPC(4301 * 16, 697 * 16, ModContent.NPCType<NPCs.Friendly.Dwarf>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4301;
                            Main.npc[npc].homeTileY = 697;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.ShamanElder>()))
                        {
                            int npc = NPC.NewNPC(4124 * 16, 690 * 16, ModContent.NPCType<NPCs.Friendly.ShamanElder>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4124;
                            Main.npc[npc].homeTileY = 690;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.TibianArcher>()))
                        {
                            int npc = NPC.NewNPC(4145 * 16, 682 * 16, ModContent.NPCType<NPCs.Friendly.TibianArcher>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4145;
                            Main.npc[npc].homeTileY = 682;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.SolaireOfAstora>()))
                        {
                            int npc = NPC.NewNPC(4370 * 16, 667 * 16, ModContent.NPCType<NPCs.Friendly.SolaireOfAstora>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4370;
                            Main.npc[npc].homeTileY = 667;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.TibianMage>()))
                        {
                            int npc = NPC.NewNPC(4176 * 16, 690 * 16, ModContent.NPCType<NPCs.Friendly.TibianMage>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4176;
                            Main.npc[npc].homeTileY = 690;
                        }                        
                    }
                }
                else
                {
                    //Stuff that should only be done if they're *not* on the custom map
                    if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                    {
                        UsefulFunctions.BroadcastText("Randomly-generated map detected. Adventure Mode auto-disabled.", Color.GreenYellow);
                        ModContent.GetInstance<tsorcRevampConfig>().AdventureMode = false;
                    }
                    if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                    {
                        UsefulFunctions.BroadcastText("Warning!! The setting 'Adventure Mode: Recipes and Items' is enabled!!", Color.OrangeRed);
                        UsefulFunctions.BroadcastText("This is intended for the custom map and can break randomly generated worlds! To prevent issues, please disable this setting and reload mods!", Color.OrangeRed);
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

        //Called upon the death of Gwyn, Lord of Cinder. Disables both hardmode and superhardmode, and sets the world state to "The End".
        public static void InitiateTheEnd()
        {
            Color c = new Color(255f, 255f, 60f);
            if (tsorcRevampWorld.SuperHardMode)
            {
                UsefulFunctions.BroadcastText("The portal from The Abyss has closed!", c);
                UsefulFunctions.BroadcastText("The world has been healed. Attraidies' sway over the world has finally ended!", c);
            }
            else
            {
                UsefulFunctions.BroadcastText("You have vanquished the final guardian...", c);
                UsefulFunctions.BroadcastText("The portal from The Abyss remains closed. All is at peace...", c);
            }
            
            //These are outside of the if statements just so players can still disable hardmode or superhardmode if they happen to activate them again.
            Main.hardMode = false;
            tsorcRevampWorld.SuperHardMode = false;
            tsorcRevampWorld.TheEnd = true;

            //		Main.NewText("You have vanquished the final guardian of the Abyss...");
            //		Main.NewText("The kiln of the First Flame has been ignited!");
            //		//Main.NewText("Congratulations, you have inherited the fire of this world. You will forever be known as the hero of the age.");  
        }

        //Runs a double check on whether or not the world is the custom map.
        public static bool CheckForCustomMap()
        {
            if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID)
            {
                return true;
            }

            //Faisafe. Checks some blocks near the top of one of the Wyvern Mage's towers that are unlikely to change. Even if they do, this shouldn't be necessary though
            //This simply ensures even if something deeply silly happens it'll still likely register as the custom map
            if (Main.tile[7102, 137] != null && Main.tile[7103, 137] != null && Main.tile[7104, 136] != null && Main.tile[7105, 136] != null)
            {
                if (Main.tile[7102, 137].TileType == 54 && Main.tile[7103, 137].TileType == 357 && Main.tile[7104, 136].TileType == 357 && Main.tile[7105, 136].TileType == 197)
                {
                    return true;
                }
            }

            return false;
        }

        //Returns 0-12 for normal bosses, 13 including Gwyn
        public static int SHMDowned
        {
            get
            {
                if(Slain == null)
                {
                    return 0;
                }

                int count = 0;
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.WaterFiendKraken>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()))
                {
                    count++;
                }
                if (Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
                {
                    count++;
                }

                return count;
            }
        }

        //Scaling formula
        //Starts at 0.7 with no bosses dead, ramps up logarithmically to 1.5x with all but Gwyn dead
        public static float SHMScale
        {
            get
            {
                return ((float)Math.Log(SHMDowned + 1, 5) * 0.63f) + 1;
            }
        }

        //Less steep scaling formula, goes from 1 to 1.2
        //For things that need more subtle tuning, like enemy movement and projectile speeds
        public static float SubtleSHMScale
        {
            get
            {
                return ((float)Math.Log(SHMDowned + 1, 5) * 0.13f) + 1;
            }
        }
    }
}