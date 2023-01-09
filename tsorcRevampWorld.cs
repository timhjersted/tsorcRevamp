using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Textures;
using tsorcRevamp.Tiles;

namespace tsorcRevamp
{
    public class tsorcRevampWorld : ModSystem
    {

        public static bool DownedVortex;
        public static bool DownedNebula;
        public static bool DownedStardust;
        public static bool DownedSolar;
        public static bool SuperHardMode;
        public static bool TheEnd;
        public static bool CustomMap;

        public static Dictionary<int, int> Slain; 
        
        public static List<int> PairedBosses;

        public static List<Vector2> LitBonfireList;

        public static Dictionary<Vector2, int> MapMarkers;

        public override void OnWorldLoad()
        {
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
            MapMarkers = new();

            PopulatePairedBosses();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            List<string> downed = new List<string>();

            if (DownedVortex)
                downed.Add("DownedVortex");
            if (DownedNebula)
                downed.Add("DownedNebula");
            if (DownedStardust)
                downed.Add("DownedStardust");
            if (DownedSolar)
                downed.Add("DownedSolar");

            List<string> world_state = new List<string>();
            if (SuperHardMode)
                world_state.Add("SuperHardMode");
            //This saves the fact that SuperHardMode has been disabled
            if (world_state.Contains("SuperHardMode") && !SuperHardMode)
            {
                world_state.Remove("SuperHardMode");
            }
            if (TheEnd)
                world_state.Add("TheEnd");
            if (CustomMap)
                world_state.Add("CustomMap");

            tag.Add("downed", downed);
            tag.Add("world_state", world_state);
            SaveSlain(tag);
            tsorcScriptedEvents.SaveScriptedEvents(tag);

            MapMarkers ??= new();
            tag.Add("MapMarkerKeys", MapMarkers.Keys.ToList());
            tag.Add("MapMarkerValues", MapMarkers.Values.ToList());
        }

        public override void LoadWorldData(TagCompound tag)
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
            if (Framing.GetTileSafely(7102, 137).TileType == 54 && Framing.GetTileSafely(7103, 137).TileType == 357 && Framing.GetTileSafely(7104, 136).TileType == 357 && Framing.GetTileSafely(7105, 136).TileType == 197)
            {
                CustomMap = true;
            }

            LitBonfireList = GetActiveBonfires();


            //If the player leaves the world or turns off their computer in the middle of the fight or whatever, this will de-actuate the pyramid for them next time they load
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (Main.tile[5810, 1670] != null)
                {
                    if (Main.tile[5810, 1670].HasTile && Main.tile[5810, 1670].IsActuated)
                    {
                        NPCs.Bosses.SuperHardMode.DarkCloud.ActuatePyramid();
                    }
                }
            }

            if (tag.ContainsKey("MapMarkerKeys")) {
                List<Vector2> markerKeys = (List<Vector2>)tag.GetList<Vector2>("MapMarkerKeys");
                List<int> markerValues = (List<int>)tag.GetList<int>("MapMarkerValues");
                for (int i = 0; i < markerKeys.Count; i++) {
                    MapMarkers.Add(markerKeys[i], markerValues[i]);
                }

            }
        }

        private void SaveSlain(TagCompound tag)
        {
            if(Slain == null)
            {
                Slain = new Dictionary<int, int>();
            }
            tag.Add("type", Slain.Keys.ToList());
            tag.Add("value", Slain.Values.ToList());
        }

        private void LoadSlain(TagCompound tag)
        {
            if (tag.ContainsKey("type"))
            {
                List<int> list = tag.Get<List<int>>("type");
                List<int> list2 = tag.Get<List<int>>("value");
                for (int i = 0; i < list.Count; i++)
                {
                    Slain.Add(list[i], list2[i]);
                }
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            if (Main.netMode == NetmodeID.Server)
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
                int markerSize = MapMarkers.Count;
                writer.Write(markerSize);
                foreach (KeyValuePair<Vector2, int> marker in MapMarkers) {
                    writer.WriteVector2(marker.Key);
                    writer.Write(marker.Value);
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
            if (LitBonfireList == null)
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

            int markerSize = reader.ReadInt32();

            MapMarkers ??= new();
            for (int i = 0; i < markerSize; i++) {
                Vector2 markerKey = reader.ReadVector2();
                int markerValue = reader.ReadInt32();
                if (!MapMarkers.ContainsKey(markerKey)) {
                    MapMarkers.Add(markerKey, markerValue);
                }
            }
        }

        public static bool JustPressed(Keys key)
        {
            return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
        }


        #region CampfireToBonfire (Is also Skelly Loot Cache replacement code)

        public static void PlaceModdedTiles()
        {
            Mod mod = ModContent.GetInstance<tsorcRevamp>();
            for (int x = 0; x < Main.maxTilesX - 2; x++)
            {
                for (int y = 0; y < Main.maxTilesY - 2; y++)
                {

                    Tile worldTile = Framing.GetTileSafely(x, y);

                    //Campfire to Bonfire
                    if (worldTile.HasTile && worldTile.TileType == TileID.Campfire)
                    {

                        //kill the space above the campfire, to remove vines and such
                        for (int q = 0; q < 3; q++)
                        {
                            for (int w = -2; w < 2; w++)
                            {
                                WorldGen.KillTile(x + q, y + w, false, false, true);
                            }
                        }
                        Dust.QuickBox(new Vector2(x + 1, y + 1) * 16, new Vector2(x + 2, y + 2) * 16, 2, Color.YellowGreen, null);
                        //WorldGen.Place3x4(x + 1, y + 1, (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>(), 0);

                        int style = 0;
                        ushort type = (ushort)ModContent.TileType<Tiles.BonfireCheckpoint>();
                        //reimplement WorldGen.Place3x4 minus SolidTile2 checking because this game is fucked 
                        {
                            if (x + 1 < 5 || x + 1 > Main.maxTilesX - 5 || y + 1 < 5 || y + 1 > Main.maxTilesY - 5)
                            {
                                return;
                            }
                            bool flag = true;
                            for (int i = x + 1 - 1; i < x + 1 + 2; i++)
                            {
                                for (int j = y + 1 - 3; j < y + 1 + 1; j++)
                                {
                                    if (Main.tile[i, j] == null)
                                    {
                                        Main.tile[i, j].ClearTile();
                                    }
                                    if (Main.tile[i, j].HasTile)
                                    {
                                        flag = false;
                                    }
                                }
                                if (Main.tile[i, y + 1 + 1] == null)
                                {
                                    Main.tile[i, y + 1 + 1].ClearTile();
                                }
                            }
                            if (flag)
                            {
                                int num = style * 54;
                                for (int k = -3; k <= 0; k++)
                                {
                                    short frameY = (short)((3 + k) * 18);
                                    Tile tile = Main.tile[x + 1 - 1, y + 1 + k];
                                    tile.HasTile = true;
                                    tile.TileFrameY = frameY;
                                    tile.TileFrameX = (short)num;
                                    tile.TileType = type;
                                    tile = Main.tile[x + 1, y + 1 + k];
                                    tile.HasTile = true;
                                    tile.TileFrameY = frameY;
                                    tile.TileFrameX = (short)(num + 18);
                                    tile.TileType = type;
                                    tile = Main.tile[x + 1 + 1, y + 1 + k];
                                    tile.HasTile = true;
                                    tile.TileFrameY = frameY;
                                    tile.TileFrameX = (short)(num + 36);
                                    tile.TileType = type;
                                }
                            }

                        }
                    }

                    //Slime blocks to SkullLeft - SlimeBlock-PinkSlimeBlock (I tried to stick right and lefts together but the code refuses to work for both, I swear I'm not just being dumb) 
                    if (worldTile.HasTile && worldTile.TileType == TileID.PinkSlimeBlock && Main.tile[x - 1, y].TileType == TileID.SlimeBlock)
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
                                    Main.tile[i, j].ClearTile();
                                }
                                if (Main.tile[i, j].HasTile)
                                {
                                    flag = false;
                                }
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1].ClearTile();
                            }
                        }
                        if (flag)
                        {
                            short num2 = (short)(36 * style);
                            Tile tile = Main.tile[x - 1, y - 1];
                            tile.HasTile = true;
                            tile.TileFrameY = num;
                            tile.TileFrameX = num2;
                            tile.TileType = type;
                            tile = Main.tile[x, y - 1];
                            tile.HasTile = true;
                            tile.TileFrameY = num;
                            tile.TileFrameX = (short)(num2 + 18);
                            tile.TileType = type;
                            tile = Main.tile[x - 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = (short)(num + 18);
                            tile.TileFrameX = num2;
                            tile.TileType = type;
                            tile = worldTile;
                            tile.HasTile = true;
                            tile.TileFrameY = (short)(num + 18);
                            tile.TileFrameX = (short)(num2 + 18);
                            tile.TileType = type;
                        }
                    }

                    //Slime block to SkullRight - PinkSlimeBlock-SlimeBlock
                    if (worldTile.HasTile && worldTile.TileType == TileID.SlimeBlock && Main.tile[x - 1, y].TileType == TileID.PinkSlimeBlock)
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
                                    Main.tile[i, j].ClearTile();
                                }
                                if (Main.tile[i, j].HasTile)
                                {
                                    flag = false;
                                }
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1].ClearTile();
                            }
                        }
                        if (flag)
                        {
                            short num2 = (short)(36 * style);
                            Tile tile = Main.tile[x - 1, y - 1];
                            tile.HasTile = true;
                            tile.TileFrameY = num;
                            tile.TileFrameX = num2;
                            tile.TileType = type;
                            tile = Main.tile[x, y - 1];
                            tile.HasTile = true;
                            tile.TileFrameY = num;
                            tile.TileFrameX = (short)(num2 + 18);
                            tile.TileType = type;
                            tile = Main.tile[x - 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = (short)(num + 18);
                            tile.TileFrameX = num2;
                            tile.TileType = type;
                            tile = worldTile;
                            tile.HasTile = true;
                            tile.TileFrameY = (short)(num + 18);
                            tile.TileFrameX = (short)(num2 + 18);
                            tile.TileType = type;
                        }
                    }

                    //Stucco blocks to SkellyLeft - GreyStucco-GreenStuccoBlock-GreyStuccoBlock
                    if (worldTile.HasTile && worldTile.TileType == TileID.GreenStucco && Main.tile[x + 1, y].TileType == TileID.GrayStucco && Main.tile[x - 1, y].TileType == TileID.GrayStucco)
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
                                Main.tile[i, y].ClearTile();
                            }
                            if (Main.tile[i, y].HasTile)
                            {
                                flag = false;
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1].ClearTile();
                            }
                        }
                        if (flag)
                        {
                            short num = (short)(54 * style);
                            Tile tile = Main.tile[x - 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = num;
                            tile.TileType = type;
                            tile = worldTile;
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = (short)(num + 18);
                            tile.TileType = type;
                            tile = Main.tile[x + 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = (short)(num + 36);
                            tile.TileType = type;
                        }
                    }

                    //Stucco blocks to SkellyRight - GreenStucco-GreyStuccoBlock-GreenStuccoBlock
                    if (worldTile.HasTile && worldTile.TileType == TileID.GrayStucco && Main.tile[x + 1, y].TileType == TileID.GreenStucco && Main.tile[x - 1, y].TileType == TileID.GreenStucco)
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
                                Main.tile[i, y].ClearTile();
                            }
                            if (Main.tile[i, y].HasTile)
                            {
                                flag = false;
                            }
                            if (Main.tile[i, y + 1] == null)
                            {
                                Main.tile[i, y + 1].ClearTile();
                            }
                        }
                        if (flag)
                        {
                            short num = (short)(54 * style);
                            Tile tile = Main.tile[x - 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = num;
                            tile.TileType = type;
                            tile = worldTile;
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = (short)(num + 18);
                            tile.TileType = type;
                            tile = Main.tile[x + 1, y];
                            tile.HasTile = true;
                            tile.TileFrameY = 0;
                            tile.TileFrameX = (short)(num + 36);
                            tile.TileType = type;
                        }
                    }

                    //Confetti blocks to SkellyHangingUp (wrists chained) - Confetti Block
                    if (worldTile.HasTile && worldTile.TileType == TileID.Confetti)
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
                                Tile tile = Main.tile[k, l];
                                tile.HasTile = true;
                                tile.TileType = type;
                                tile.TileFrameX = (short)(num4 + 18 * (k - num));
                                tile.TileFrameY = (short)(num5 + 18 * (l - num2));
                            }
                        }
                    }

                    //Confetti blocks to SkellyHangingDown (ankles chained) - Confetti Black Block (aka Midnight Confetti Block)
                    if (worldTile.HasTile && worldTile.TileType == TileID.ConfettiBlack)
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
                                Tile tile = Main.tile[k, l];
                                tile.HasTile = true;
                                tile.TileType = type;
                                tile.TileFrameX = (short)(num4 + 18 * (k - num));
                                tile.TileFrameY = (short)(num5 + 18 * (l - num2));
                            }
                        }
                    }

                }
            }
            for (int i = 0; i < 400; i++)
            {
                if (Main.item[i].type == ItemID.Campfire && Main.item[i].active)
                {
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
            for (int i = 1; i < (Main.tile.Width - 1); i += 3)
            {
                for (int j = 1; j < (Main.tile.Height - 1); j += 4)
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

        Asset<Texture2D> SHMSun1 = ModContent.Request<Texture2D>("tsorcRevamp/Textures/SHMSun1");
        Asset<Texture2D> SHMSun2 = ModContent.Request<Texture2D>("tsorcRevamp/Textures/SHMSun2");
        Asset<Texture2D> SHMSun3 = ModContent.Request<Texture2D>("tsorcRevamp/Textures/SHMSun1");
        Asset<Texture2D> SHMMoon = ModContent.Request<Texture2D>("tsorcRevamp/Textures/SHMMoon");
        Asset<Texture2D> VanillaSun1 = ModContent.Request<Texture2D>("Terraria/Images/Sun");
        Asset<Texture2D> VanillaSun2 = ModContent.Request<Texture2D>("Terraria/Images/Sun2");
        Asset<Texture2D> VanillaSun3 = ModContent.Request<Texture2D>("Terraria/Images/Sun3");
        List<Asset<Texture2D>> VanillaMoonTextures;

        //MAKE CATACOMBS DUNGEON BIOME - This code was blocking spawns in the catacombs, but catacombs now works as dungeon without it likely
        //because of other code improving dungeon spawn detection

        //public override void TileCountsAvailable(int[] tileCounts) {
        //Main.dungeonTiles += tileCounts[TileID.BoneBlock];
        //Main.dungeonTiles += tileCounts[TileID.MeteoriteBrick];

        //}

        public override void PreUpdateWorld()
        {
            Terraria.GameContent.Creative.CreativePowerManager.Instance.GetPower<Terraria.GameContent.Creative.CreativePowers.StopBiomeSpreadPower>().SetPowerInfo(true);
        }

        public override void PostUpdateWorld()
        {
            Terraria.GameContent.Creative.CreativePowerManager.Instance.GetPower<Terraria.GameContent.Creative.CreativePowers.StopBiomeSpreadPower>().SetPowerInfo(false);            
        }

        bool initialized = false;
        public override void PreUpdatePlayers()
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
                    if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                    {
                        Tiles.SoulSkellyGeocache.InitializeSkellys();                   
                    }

                    //Stuff that should only be done by either a solo player or the server
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        
                        if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID)
                        {
                            Main.worldID = Main.rand.Next(9999999);
                            PlaceModdedTiles();
                        }

                        //Spawn in NPCs
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.EmeraldHerald>()))
                        {
                            NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4510 * 16, 737 * 16, ModContent.NPCType<NPCs.Friendly.EmeraldHerald>());
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.Dwarf>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4301 * 16, 697 * 16, ModContent.NPCType<NPCs.Friendly.Dwarf>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4301;
                            Main.npc[npc].homeTileY = 697;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.ShamanElder>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4124 * 16, 690 * 16, ModContent.NPCType<NPCs.Friendly.ShamanElder>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4124;
                            Main.npc[npc].homeTileY = 690;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.TibianArcher>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4145 * 16, 682 * 16, ModContent.NPCType<NPCs.Friendly.TibianArcher>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4145;
                            Main.npc[npc].homeTileY = 682;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.SolaireOfAstora>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4370 * 16, 667 * 16, ModContent.NPCType<NPCs.Friendly.SolaireOfAstora>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4370;
                            Main.npc[npc].homeTileY = 667;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.TibianMage>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4176 * 16, 690 * 16, ModContent.NPCType<NPCs.Friendly.TibianMage>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4176;
                            Main.npc[npc].homeTileY = 690;
                        }
                        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Friendly.TibianMage>()))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4176 * 16, 690 * 16, ModContent.NPCType<NPCs.Friendly.TibianMage>());
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 4176;
                            Main.npc[npc].homeTileY = 690;
                        }

                        NPC.savedGolfer = true;
                        NPC.savedStylist = true;

                        if (!NPC.AnyNPCs(NPCID.Golfer))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4176 * 16, 690 * 16, NPCID.Golfer);
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 5881;
                            Main.npc[npc].homeTileY = 866;
                        }
                        if (!NPC.AnyNPCs(NPCID.Stylist))
                        {
                            int npc = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), 4176 * 16, 690 * 16, NPCID.Stylist);
                            Main.npc[npc].homeless = false;
                            Main.npc[npc].homeTileX = 5863;
                            Main.npc[npc].homeTileY = 835;
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
                }
            }
            if (tsorcRevamp.NearbySoapstone != null) {
                if (tsorcRevamp.NearbySoapstone.timer <= 0 && !tsorcRevamp.NearbySoapstone.nearPlayer)
                    tsorcRevamp.NearbySoapstone = null;
            }
        }

        internal static void HandleDevKeys() {
            char separator = Path.DirectorySeparatorChar;

            string jsonPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcSoapstones.json"; //Where the music mod is downloaded to


            //kill signs and rebuild json. if youre making manual edits to the json you probably dont need this any more
            if (JustPressed(Keys.NumPad7) && JustPressed(Keys.Home)) {

                for (int x = 0; x < Main.maxTilesX - 2; x++) {
                    for (int y = 0; y < Main.maxTilesY - 2; y++) {
                        Tile worldTile = Framing.GetTileSafely(x, y);
                        if (worldTile.HasTile && worldTile.TileType == TileID.Signs) {
                            string h = Main.sign[Sign.ReadSign(x, y, false)].text;

                            SignJSONSerializable signJson = new();
                            signJson.text = h;
                            signJson.tileX = x;
                            signJson.tileY = y;
                            signJson.textWidth = SoapstoneMessage.DEFAULT_WIDTH;
                            signJson.style = SoapstoneMessage.DEFAULT_STYLE;

                            JsonSerializerOptions opt = new JsonSerializerOptions { WriteIndented = true };
                            string rawJson = JsonSerializer.Serialize(signJson, opt);
                            List<string> asList = new() { rawJson };
                            File.AppendAllLines(jsonPath, asList);
                            for (int q = x; q < x + 2; q++) {
                                for (int w = y; w < y + 2; w++) {
                                    WorldGen.KillTile(q, w);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < 400; i++) {
                    if (Main.item[i].type == ItemID.Sign && Main.item[i].active) {
                        Main.item[i].active = false; //delete ground items (in this case campfires)
                    }
                }
                Main.NewText("wrote json!");
            }


            //kill signs and place soapstones from the json. for testing manual json edits
            //eventually can be done on world load, or something
            if (JustPressed(Keys.NumPad5)) {

                for (int x = 0; x < Main.maxTilesX - 2; x++) {
                    for (int y = 0; y < Main.maxTilesY - 2; y++) {
                        Tile worldTile = Framing.GetTileSafely(x, y);
                        if (worldTile.HasTile && worldTile.TileType == TileID.Signs) {
                            for (int q = x; q < x + 2; q++) {
                                for (int w = y; w < y + 2; w++) {
                                    WorldGen.KillTile(q, w);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < 400; i++) {
                    if (Main.item[i].type == ItemID.Sign && Main.item[i].active) {
                        Main.item[i].active = false;
                    }
                }
                Main.NewText("killed signs!");

                string bigJson = File.ReadAllText(jsonPath);
                List<SignJSONSerializable> texts = UsefulFunctions.DeserializeMultiple<SignJSONSerializable>(bigJson).ToList();
                foreach (SignJSONSerializable sign in texts) {
                    int locX = sign.tileX;
                    int locY = sign.tileY;
                    Dust.QuickBox(new Vector2(locX, locY) * 16, new Vector2(locX + 1, locY + 1) * 16, 2, Color.YellowGreen, null);

                    
                    Tile tile = Framing.GetTileSafely(locX, locY);
                    if (tile.TileType != ModContent.TileType<SoapstoneTile>()) {

                        //tip: i am so fucking mad
                        if (!tile.HasTile) tile.HasTile = true;
                        tile.TileType = (ushort)ModContent.TileType<SoapstoneTile>();
                        tile.TileFrameX = 0;
                        tile.TileFrameY = 0;
                        tile.TileFrameNumber = 0;

                        ModContent.GetInstance<SoapstoneTileEntity>().Place(locX, locY);
                        if (TileUtils.TryGetTileEntityAs(locX, locY, out SoapstoneTileEntity entity)) {
                            entity.text = sign.text;
                            entity.textWidth = sign.textWidth;
                            entity.style = sign.style;
                        } 
                    }
                }
                Main.NewText("attempted to read json!");
            }
        }
        public override void PostUpdateEverything()
        {
            if (JustPressed(Keys.Home) && JustPressed(Keys.NumPad0)) //they have to be pressed *on the same tick*. you can't hold one and then press the other.
                PlaceModdedTiles();

            //HandleDevKeys();

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                tsorcScriptedEvents.ScriptedEventCheck();
            }


            bool charm = false;
            foreach (Player p in Main.player)
            {
                for (int i = 3; i <= 8; i++)
                {
                    if (p.armor[i].type == ModContent.ItemType<Items.Accessories.Defensive.CovenantOfArtorias>())
                    {
                        charm = true;
                        break;
                    }
                }
            }
            if (charm)
            {
                Main.bloodMoon = true;
                Main.moonPhase = 0;
                Main.dayTime = false;
                Main.time = 16240.0;
                if (Main.GlobalTimeWrappedHourly % 120 == 0 && Main.netMode != NetmodeID.SinglePlayer)
                {
                    //globaltime always ticks up unless the player is in camera mode, and lets be honest: who uses camera mode? 
                    NetMessage.SendData(MessageID.WorldData);
                }

            }
            if (!Main.dedServ)
            {
                if (SuperHardMode)
                {
                    for (int i = 0; i < TextureAssets.Moon.Length; i++)
                    {
                        TextureAssets.Moon[i] = SHMMoon;
                    }
                    TextureAssets.Sun = SHMSun1;
                    TextureAssets.Sun2 = SHMSun2;
                    TextureAssets.Sun3 = SHMSun3;
                }
                if (TheEnd)
                { //super hardmode and the end are mutually exclusive, so there won't be any "z-fighting", but this still feels silly
                    TextureAssets.Sun = VanillaSun1;
                    TextureAssets.Sun2 = VanillaSun2;
                    TextureAssets.Sun3 = VanillaSun3;
                    if (VanillaMoonTextures == null)
                    {
                        VanillaMoonTextures = new List<Asset<Texture2D>>();
                        for (int i = 0; i < TextureAssets.Moon.Length; i++)
                        {
                            VanillaMoonTextures.Add(ModContent.Request<Texture2D>("Terraria/Images/Moon_" + i));
                        }
                    }
                    for (int i = 0; i < TextureAssets.Moon.Length; i++)
                    {
                        TextureAssets.Moon[i] = VanillaMoonTextures[i];
                    }
                }
            }
        }

        public override void OnWorldUnload() {
            tsorcRevamp.NearbySoapstone = null;
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

        public static bool BossAlive
        {
            get
            {
                for(int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].boss)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        //13 SHM bosses:
        //8 Mandatory:
        //Kraken
        //Marilith
        //Lich
        //Seath
        //Blight
        //Wyvern Mage Shadow
        //Artorias
        //Gwyn

        //5 optional:
        //Hellkite Dragon
        //Witchking
        //Oolicale Sorcerer
        //Chaos
        //Dark Cloud

        public static Recipe.Condition AdventureModeDisabled
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("Only craftable outside of Adventure Mode"), r => ModContent.GetInstance<tsorcRevampConfig>().AdventureMode);
            }
        }
        public static Recipe.Condition AdventureModeEnabled
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("Only craftable outside of Adventure Mode"), r => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode);
            }
        }


        public static Recipe.Condition SHM1Downed
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("One Guardian of the Abyss Slain"), r => SHMDowned > 0);
            }
        }
        public static Recipe.Condition SHM3Downed
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("Three Guardians of the Abyss Slain"), r => SHMDowned >= 3);
            }
        }
        public static Recipe.Condition SHM6Downed
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("Six Guardians of the Abyss Slain"), r => SHMDowned >= 6);
            }
        }
        public static Recipe.Condition SHM9Downed
        {
            get
            {
                return new Recipe.Condition(Terraria.Localization.NetworkText.FromKey("Nine Guardians of the Abyss Slain"), r => SHMDowned >= 9);
            }
        }

        //Returns 0-12 for normal bosses, 13 including Gwyn
        public static int SHMDowned
        {
            get
            {
                if (Slain == null)
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

        public static void PopulatePairedBosses()
        {
            PairedBosses = new List<int>();
            //Wyvern Mage and his wyvern
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>());
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());

            //Slogra and Gaibon
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.Slogra>());
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.Gaibon>());

            //Wyvern Mage Shadow and its disciple
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>());
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>());

            //Serris and Serris X
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            PairedBosses.Add(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>());
        }
    }
}