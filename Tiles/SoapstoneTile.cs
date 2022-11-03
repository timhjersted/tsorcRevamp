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
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI.Chat;

namespace tsorcRevamp.Tiles {
    public class SoapstonePlaceable : ModItem {

        public override string Texture => "tsorcRevamp/Tiles/BonfirePlaceable";

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("You probably shouldn't have this.");
        }

        public override void SetDefaults() {
            Item.CloneDefaults(ItemID.Bookcase);
            Item.createTile = ModContent.TileType<SoapstoneTile>();
            Item.placeStyle = 0;
        }
    }

    public class SoapstoneTile : ModTile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Origin = new(0, 0);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(ModContent.GetInstance<SoapstoneTileEntity>().Hook_AfterPlacement, -1, 0, false);
            TileObjectData.addTile(Type);
            ModTranslation name = CreateMapEntryName();
            name.SetDefault("Soapstone Message");
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
            if (TileUtils.TryGetTileEntityAs(i, j, out SoapstoneTileEntity entity)) {

                Vector2 zero = new(Main.offScreenRange, Main.offScreenRange);
                if (Main.drawToScreen) {
                    zero = Vector2.Zero;
                }

                TransparentTextureHandler.TransparentTextureType type;
                switch (entity.style) {
                    case SoapstoneStyle.Runes: {
                        type = TransparentTextureHandler.TransparentTextureType.SoapstoneMessage0;
                        break;
                    }
                    case SoapstoneStyle.Dialogue: {
                        type = TransparentTextureHandler.TransparentTextureType.SoapstoneMessage1;
                        break;
                    }
                    default: {
                        type = TransparentTextureHandler.TransparentTextureType.SoapstoneMessage0;
                        break;
                    }
                }
                Texture2D texture = TransparentTextureHandler.TransparentTextures[type];

                Vector2 textureSize = texture.Size();

                Vector2 position = new(i * 16 - ((int)Main.screenPosition.X + textureSize.X / 2) + 16, j * 16 - (int)Main.screenPosition.Y - (textureSize.Y / 2));

                Color ShimmerColor;
                switch (entity.style) {
                    case SoapstoneStyle.Runes: {
                        ShimmerColor = new(192 + (Main.DiscoR / 2), 128 + (Main.DiscoG / 4), 16);
                        break;
                    }
                    default: {
                        ShimmerColor = Color.PapayaWhip;
                        break;
                    }
                }

                if (entity.read) {
                    ShimmerColor.R /= 2;
                    ShimmerColor.G /= 2;
                    ShimmerColor.B /= 2;
                }

                spriteBatch.Draw(texture, position + zero, new Rectangle(0, 0, (int)textureSize.X, (int)textureSize.Y), ShimmerColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

                if (entity.nearPlayer) {
                    tsorcRevamp.NearbySoapstone = entity;
                }
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
        public bool show;

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
            show = !read || !ModContent.GetInstance<tsorcRevampConfig>().HideSoapstones;
            nearPlayer = false;
            for (int i = 0; i < Main.maxPlayers; i++) {
                Player p = Main.player[i];
                if (Vector2.Distance(p.Center, Position.ToVector2() * 16) <= 64) {
                    nearPlayer = true;
                    read = true;
                    if (text == null) {
                        foreach (SoapstoneMessage cache in SoapstoneMessage.SoapstoneList) {
                            if (Position == cache.location) {
                                text = cache.text;
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            if (timer > 0) {
                show = true;
            }
            if (nearPlayer && show) {
                if (timer < 20) timer += 1;
            }
            else {
                if (timer >= 1) {
                    timer -= 2;
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
