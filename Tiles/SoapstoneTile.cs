using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;

namespace tsorcRevamp.Tiles {

    public class SoapstoneTile : ModTile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Origin = new(0, 0);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<SoapstoneTileEntity>().Hook_AfterPlacement, -1, 0, false);
            TileObjectData.addTile(Type);
            LocalizedText name = CreateMapEntryName();
            // name.SetDefault("Soapstone Message");
            AddMapEntry(new Color(100, 0, 20), name);
            DustType = 117;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileLighted[Type] = false;
            Main.tileSpelunker[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = false;
            Main.tileSolid[Type] = false;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) {
            return false;
        }

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem) {
            fail = true; 
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
            if (TileUtils.TryGetTileEntityAs(i, j, out SoapstoneTileEntity entity))
            {
                Vector2 zero = new(Main.offScreenRange, Main.offScreenRange);
                if (Main.drawToScreen)
                {
                    zero = Vector2.Zero;
                }

                TransparentTextureHandler.TransparentTextureType type = TransparentTextureHandler.TransparentTextureType.SoapstoneMessage;
                Texture2D texture = TransparentTextureHandler.TransparentTextures[type];

                Vector2 textureSize = texture.Size();

                Vector2 position = new(i * 16 - ((int)Main.screenPosition.X + textureSize.X / 2) + 16, j * 16 - (int)Main.screenPosition.Y);
                Vector2 worldPosition = new Vector2(i + 1, j + 1) * 16;
                float distance = Vector2.Distance(Main.LocalPlayer.Center, worldPosition);
                float mouseDistance = Vector2.Distance(tsorcRevampPlayer.RealMouseWorld, worldPosition);

                bool mouseLineOfSight = (Collision.CanHitLine(Main.LocalPlayer.Center, 1, 1, worldPosition, 1, 1));
                if (entity.read)
                {
                    mouseLineOfSight = true;
                }

                bool selectedSoapstone = false;
                bool mouseInRange = mouseDistance < 150 && mouseLineOfSight;
                bool playerInRange = distance <= 128;

                //Main.NewText("soap " + tsorcRevamp.NearbySoapstoneMouse);
                //If the mouse is already nearby a soapstone
                if (tsorcRevamp.NearbySoapstoneMouse)
                {
                    //If the mouse is closer to this soapstone than to the previous one, set this as the current soapstone
                    if (mouseDistance < tsorcRevamp.NearbySoapstoneMouseDistance && mouseInRange && distance < 400)
                    {
                        tsorcRevamp.NearbySoapstone = entity;
                        if (!entity.hidden) {
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 30);
                        }

                        tsorcRevamp.NearbySoapstoneMouse = true;
                        tsorcRevamp.NearbySoapstoneMouseDistance = mouseDistance;
                        entity.timer = 25;
                        entity.read = true;
                        entity.nearPlayer = true;
                    }
                }
                else if (playerInRange || (mouseInRange && distance < 400))
                {
                    tsorcRevamp.NearbySoapstone = entity;
                    if (!entity.hidden) {
                        Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 30); 
                    }

                    if (mouseInRange)
                    {
                        tsorcRevamp.NearbySoapstoneMouse = true;
                        tsorcRevamp.NearbySoapstoneMouseDistance = mouseDistance;
                    }
                    entity.timer = 25;
                    entity.read = true;
                    entity.nearPlayer = true;
                }


                if (!selectedSoapstone)
                {
                    if (entity.timer > 0)
                    {
                        entity.timer -= 5;

                        if (entity.timer <= 0)
                        {
                            entity.timer = 0;
                            entity.nearPlayer = false;

                            if (tsorcRevamp.NearbySoapstone == entity)
                            {
                                tsorcRevamp.NearbySoapstone = null;
                            }

                            if (ModContent.GetInstance<tsorcRevampConfig>().HideSoapstones)
                            {
                                entity.hidden = true;
                            }
                        }
                    }
                }


                Color ShimmerColor = Color.PapayaWhip;

                if (entity.read)
                {
                    ShimmerColor.R /= 4;
                    ShimmerColor.G /= 4;
                    ShimmerColor.B /= 3;
                }

                spriteBatch.Draw(texture, position + zero, new Rectangle(0, 0, (int)textureSize.X, (int)textureSize.Y), ShimmerColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            }
        }
    }
    public class SoapstoneTileEntity : ModTileEntity {
        public string text;
        public int textWidth;
        public SoapstoneStyle style;
        public float timer;
        public bool nearPlayer;
        public bool read = false;
        public bool hidden = false;

        public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);

                //Sync the placement of the tile entity with other clients
                //The "type" parameter refers to the tile type which placed the tile entity, so "Type" (the type of the tile entity) needs to be used here instead
                NetMessage.SendData(MessageID.TileEntityPlacement, -1, -1, null, i, j, Type);
            }

            //ModTileEntity.Place() handles checking if the entity can be placed, then places it for you
            int placedEntity = Place(i, j);
            return placedEntity;
        }

        
        public override bool IsTileValidForEntity(int x, int y) {
            Tile tile = Framing.GetTileSafely(x, y);
            return tile.HasTile && tile.TileType == ModContent.TileType<SoapstoneTile>();
        }

        public override void OnNetPlace() {
            if (Main.netMode == NetmodeID.Server) 
                NetMessage.SendData(MessageID.TileEntitySharing, -1, -1, null, ID, Position.X, Position.Y);
        }

        public override void Update() {

            if (text == null)
            {
                foreach (SoapstoneMessage cache in SoapstoneMessage.SoapstoneList)
                {
                    if (Position == cache.location)
                    {
                        text = cache.text;
                        break;
                    }
                }
            }
        }

        public override void NetSend(BinaryWriter writer) {
            writer.Write(text);
            writer.Write(textWidth);
            writer.Write(read);
        }

        public override void NetReceive(BinaryReader reader) {
            text = reader.ReadString();
            textWidth = reader.ReadInt32();
            read = reader.ReadBoolean();
        }

        public override void SaveData(TagCompound tag) {
            tag.Add("text", text);
            tag.Add("boxWidth", textWidth);
            tag.Add("read", read);
        }

        public override void LoadData(TagCompound tag) {
            string? saved = tag.GetString("text");
            text = saved;

            int? width = tag.GetInt("boxWidth");
            textWidth = width ?? SoapstoneMessage.DEFAULT_WIDTH;

            bool? dim = tag.GetBool("read");
            read = dim ?? false;

        }
    }
}
