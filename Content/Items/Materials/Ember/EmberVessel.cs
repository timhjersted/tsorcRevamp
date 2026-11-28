using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Materials.Ember
{
    /// <summary>
    /// Floating 2x2 animated ember pickup converted on world load from 2x2 Yellow Team Blocks (Pale Ember)
    /// and 2x2 Pink Team Blocks (Radiant Ember).
    /// TileFrameX encodes variant and claimed state:
    ///   0 / 18  = Pale Ember (unclaimed, white glow)
    ///   36 / 54 = Radiant Ember (unclaimed, pink glow)
    ///   >= 72   = Claimed (invisible and non-interactable)
    /// Remains invisible and non-interactable whenever WeaponReinforcement.Enabled is false.
    /// </summary>
    public class EmberVessel : ModTile
    {
        public const short PaleFrameOffset = 0;
        public const short RadiantFrameOffset = 36;
        public const short ClaimedFrameOffset = 72;

        private const int FrameWidth = 48;
        private const int FrameHeight = 64;
        private const int FrameCount = 6;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = false;
            Main.tileNoFail[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(220, 215, 230));
            DustType = DustID.WhiteTorch;
        }

        private static bool IsClaimed(Tile tile)
        {
            return tile.TileFrameX >= ClaimedFrameOffset;
        }

        private static bool IsRadiant(Tile tile)
        {
            return ((tile.TileFrameX / 36) % 2) == 1;
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            return false;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile tile = Main.tile[i, j];
            if (!WeaponReinforcement.Enabled || IsClaimed(tile))
            {
                r = 0f;
                g = 0f;
                b = 0f;
                return;
            }

            if (IsRadiant(tile))
            {
                r = 0.56f;
                g = 0.22f;
                b = 0.44f;
            }
            else
            {
                r = 0.40f;
                g = 0.42f;
                b = 0.46f;
            }
        }

        public override void MouseOver(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (!WeaponReinforcement.Enabled || IsClaimed(tile))
            {
                return;
            }

            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = IsRadiant(tile)
                ? ModContent.ItemType<RadiantEmber>()
                : ModContent.ItemType<PaleEmber>();
        }

        public override void MouseOverFar(int i, int j)
        {
            MouseOver(i, j);
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (!WeaponReinforcement.Enabled || IsClaimed(tile))
            {
                return false;
            }

            bool radiant = IsRadiant(tile);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.OpenEmberVessel);
                packet.Write((short)i);
                packet.Write((short)j);
                packet.Send();
            }
            else if (!TryOpen(i, j, Main.myPlayer))
            {
                return false;
            }

            if (!Main.dedServ)
            {
                Vector2 worldCenter = new Vector2(i * 16 + 8, j * 16 + 8);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.65f, Pitch = radiant ? 0.35f : 0.1f }, worldCenter);

                int dustType = radiant ? DustID.PinkTorch : DustID.WhiteTorch;
                for (int k = 0; k < 14; k++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.2f, 3.2f);
                    Dust mote = Dust.NewDustPerfect(worldCenter, dustType, velocity, 40, default, Main.rand.NextFloat(1.0f, 1.35f));
                    mote.noGravity = true;
                }
            }

            return true;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            if (!WeaponReinforcement.Enabled || IsClaimed(tile))
            {
                return false;
            }

            int localX = (tile.TileFrameX / 18) % 2;
            int localY = (tile.TileFrameY / 18) % 2;
            if (localX != 0 || localY != 0)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Tile[Type].Value;
            if (texture == null)
            {
                return false;
            }

            bool radiant = IsRadiant(tile);
            int frameIndex = (int)((Main.GameUpdateCount / 7 + i * 3 + j * 5) % FrameCount);
            Rectangle sourceRect = new Rectangle(0, frameIndex * FrameHeight, FrameWidth, FrameHeight);
            Vector2 origin = new Vector2(FrameWidth * 0.5f, FrameHeight * 0.5f);

            Vector2 offScreen = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            float bobOffset = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f + i * 0.7f + j * 0.5f) * 3f;
            Vector2 drawPosition = new Vector2(i * 16 + 16, j * 16 + 12 + bobOffset) + offScreen - Main.screenPosition;

            Color auraColor;
            Color coreColor;
            if (radiant)
            {
                auraColor = new Color(255, 115, 205, 0) * 0.45f;
                coreColor = new Color(255, 180, 228, 235);
            }
            else
            {
                auraColor = new Color(220, 232, 248, 0) * 0.35f;
                coreColor = new Color(240, 244, 252, 230);
            }

            // Faint additive glow halo behind the 6-frame floating flame sprite.
            spriteBatch.Draw(texture, drawPosition, sourceRect, auraColor, 0f, origin, 1.14f, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, drawPosition, sourceRect, coreColor, 0f, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }

        // Server/singleplayer authority: validate tile & player distance, mark claimed, and spawn the ember reward.
        public static bool TryOpen(int i, int j, int playerIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !WeaponReinforcement.Enabled)
            {
                return false;
            }

            if (playerIndex < 0 || playerIndex >= Main.maxPlayers
                || i < 1 || j < 1 || i >= Main.maxTilesX - 1 || j >= Main.maxTilesY - 1)
            {
                return false;
            }

            Player player = Main.player[playerIndex];
            if (!player.active || player.dead)
            {
                return false;
            }

            Vector2 tilePos = new Vector2(i * 16 + 8, j * 16 + 8);
            if (Vector2.DistanceSquared(player.Center, tilePos) > 12f * 16f * 12f * 16f)
            {
                return false;
            }

            Tile clicked = Main.tile[i, j];
            int vesselTileType = ModContent.TileType<EmberVessel>();
            if (!clicked.HasTile || clicked.TileType != vesselTileType || IsClaimed(clicked))
            {
                return false;
            }

            bool radiant = IsRadiant(clicked);
            short baseFrameX = radiant ? RadiantFrameOffset : PaleFrameOffset;
            int originX = i - ((clicked.TileFrameX / 18) % 2);
            int originY = j - ((clicked.TileFrameY / 18) % 2);

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Tile part = Main.tile[originX + x, originY + y];
                    if (!part.HasTile || part.TileType != vesselTileType
                        || part.TileFrameX != baseFrameX + x * 18
                        || part.TileFrameY != y * 18)
                    {
                        return false;
                    }
                }
            }

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Main.tile[originX + x, originY + y].TileFrameX += ClaimedFrameOffset;
                }
            }

            int rewardItemType = radiant ? ModContent.ItemType<RadiantEmber>() : ModContent.ItemType<PaleEmber>();
            int itemIndex = Item.NewItem(
                new EntitySource_Misc("EmberVessel"),
                new Rectangle(originX * 16, originY * 16, 32, 32),
                rewardItemType);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, originX, originY, 3);
                NetMessage.SendData(MessageID.SyncItem, number: itemIndex);
            }

            return true;
        }
    }
}
