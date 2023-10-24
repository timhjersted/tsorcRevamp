using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Placeable;
using tsorcRevamp.NPCs.Special;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Tiles
{
    public class BonfireCheckpoint : ModTile
    {
        int bonfireEffectTimer = 0;
        int boneDustEffectTimer = 0;
        int bonfireHealTimer = 0;
        float healquantity = 0f;
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileBlockLight[Type] = false;
            Main.tileNoAttach[Type] = true;
            Main.tileWaterDeath[Type] = false;
            Main.tileLavaDeath[Type] = false;
            Main.tileSpelunker[Type] = true;

            TileID.Sets.DisableSmartCursor[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 18 };
            TileObjectData.addTile(Type);

            AnimationFrameHeight = 74;
            DustType = DustID.Web;
            AdjTiles = new int[] { TileID.Campfire };

            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(215, 60, 0), name);

        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged)
        {
            return !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            bool lit = Main.tile[i, j].TileFrameY >= 74;
            r = lit ? 0.7f : 0.28f;
            g = lit ? 0.4f : 0.16f;
            b = lit ? 0.28f : 0.04f;

            if (lit)
            {
                return;
            }

            bool noFirefly = true;
            for (int n = 0; n < Main.maxNPCs; n++)
            {
                var npc = Main.npc[n];
                if (!npc.active || npc.friendly)
                {
                    continue;
                }

                if (npc.type == ModContent.NPCType<Bonfirefly>() && npc.Distance(new Vector2(i * 16, j * 16)) < (6 * 16))
                {
                    noFirefly = false;
                    break;
                }
            }

            bool nearPlayer = false;
            for (int n = 0; n < Main.maxPlayers; n++)
            {
                var player = Main.player[n];
                if (!player.active)
                {
                    continue;
                }

                if (player.Distance(new Vector2(i * 16, j * 16)) < (60 * 16))
                {
                    nearPlayer = true;
                    break;
                }
            }

            if (noFirefly && nearPlayer)
            {
                NPC.NewNPC(Entity.GetSource_NaturalSpawn(), i * 16, j * 16, ModContent.NPCType<Bonfirefly>(), 0, i, j);
            }
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            var player = Main.LocalPlayer;
            var pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
            var distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

            if (Main.tile[i, j].TileFrameY >= 74 && distance <= 80f && !player.dead)
            {
                player.AddBuff(ModContent.BuffType<Buffs.Bonfire>(), 30);

                player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;

                bool bossActive = false;
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    if (Main.npc[n].active && Main.npc[n].boss)
                    {
                        bossActive = true;
                        break;
                    }
                }

                if (player.velocity != Vector2.Zero)
                {
                    bonfireHealTimer = 0;
                    healquantity = 0;
                }

                if (bonfireEffectTimer > 28)
                {
                    if (player.HasBuff(ModContent.BuffType<InCombat>()))
                    {
                        player.ClearBuff(ModContent.BuffType<InCombat>());
                    }

                    if (!bossActive && player.velocity == Vector2.Zero)
                    {
                        foreach (int buffType in player.buffType)
                        {
                            if (Main.debuff[buffType] && !BuffID.Sets.NurseCannotRemoveDebuff[buffType])
                            {
                                player.ClearBuff(buffType);
                            }
                        }
                    }
                }


                // Only heal when no bosses are alive, hp isn't full and the player is standing still
                if (!bossActive && player.statLife < player.statLifeMax2 && player.velocity == Vector2.Zero)
                {

                    if (player.velocity.X == 0 && player.velocity.Y == 0)
                    {
                        bonfireHealTimer++;
                    }

                    // Wind up 1
                    if (bonfireEffectTimer > 0 && bonfireEffectTimer <= 12)
                    {
                        healquantity += (float)player.statLifeMax2 / 4000f;
                        if (bonfireHealTimer % 6 == 0)
                        {
                            player.statLife += (int)healquantity;
                            if (healquantity >= 1)
                            {
                                healquantity = 0;
                            }
                        }

                        if (Main.rand.NextBool(8))
                        {
                            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.FlameBurst, Alpha: 120);
                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.015f, 10f, player.Center);
                        }
                    }


                    // Wind up 2
                    if (bonfireEffectTimer > 12 && bonfireEffectTimer <= 20)
                    {
                        healquantity += (float)player.statLifeMax2 / 3500f;
                        if (bonfireHealTimer % 6 == 0)
                        {
                            player.statLife += (int)healquantity;
                            if (healquantity >= 1)
                            {
                                healquantity = 0;
                            }
                        }

                        if (Main.rand.NextBool(4))
                        {
                            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.FlameBurst, Alpha: 120);
                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.025f, 15f, player.Center);
                        }
                    }

                    // Wind up 3
                    if (bonfireEffectTimer > 20 && bonfireEffectTimer <= 28)
                    {
                        healquantity += (float)player.statLifeMax2 / 2800f;
                        if (bonfireHealTimer % 6 == 0)
                        {
                            player.statLife += (int)healquantity;
                            if (healquantity >= 1)
                            {
                                healquantity = 0;
                            }
                        }

                        if (Main.rand.NextBool(2))
                        {
                            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.FlameBurst, Alpha: 120);
                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.035f, 20f, player.Center);
                        }
                    }

                    // Full effect
                    if (bonfireEffectTimer > 28)
                    {
                        healquantity += (float)player.statLifeMax2 / 2000f;
                        if (bonfireHealTimer % 6 == 0)
                        {
                            player.statLife += (int)healquantity;
                            if (healquantity >= 1)
                            {
                                healquantity = 0;
                            }
                        }


                        var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.FlameBurst, Alpha: 120);
                        HandleDust(ref dust, Main.rand.Next(80, 95) * 0.043f, 25f, player.Center);
                    }
                }
            }

            if (player.whoAmI == Main.myPlayer && distance > 120f && distance < 300f)
            {
                player.ClearBuff(ModContent.BuffType<Buffs.Bonfire>());
            }
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            // Spend 5 ticks on each of 20 frames
            if (++frameCounter > 5)
            {
                frameCounter = 0;
                frame = ++frame % 20;
            }
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            var tile = Framing.GetTileSafely(i, j);

            if (Main.rand.NextBool(100))
            {
                var dust = Dust.NewDustDirect(new Vector2(i * 16, j * 16), 16, 16, DustID.RedTorch);
                dust.noGravity = true;
            }

            var player = Main.LocalPlayer;
            var pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
            var distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

            if (player.velocity.X != 0 || player.velocity.Y != 0) //reset if player moves
            {
                bonfireEffectTimer = 0;
            }

            if (!Main.gamePaused && Main.instance.IsActive && (!Lighting.UpdateEveryFrame || Main.rand.NextBool(4)))
            {
                short frameX = tile.TileFrameX;
                short frameY = tile.TileFrameY;

                if (tile.TileFrameY >= 74 && player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && distance < 120f)
                {
                    int style = frameY / 74;

                    if (frameY / 18 % 5 == 0 && frameX / 18 % 3 == 0) //this changes the height
                    {
                        if (player.velocity.X == 0 && player.velocity.Y == 0)
                        {
                            bonfireEffectTimer++;
                        }

                        if (player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()) && distance < 120f && player.HeldItem.type == ModContent.ItemType<Items.SublimeBoneDust>() && player.itemTime != 0)
                        {
                            boneDustEffectTimer++;
                            if (boneDustEffectTimer == 1)
                            {
                                SoundEngine.PlaySound(SoundID.Item20, new Vector2(i * 16 + 25, j * 16 + 32));

                                for (int q = 0; q < 30; q++)
                                {
                                    var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                    HandleDust(ref dust, Main.rand.Next(50, 100) * 0.2f, 10f, new Vector2(i * 16 + 25, j * 16 + 32));
                                }

                                for (int q = 0; q < 30; q++)
                                {
                                    var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                    HandleDust(ref dust, Main.rand.Next(50, 100) * 0.12f, 30f, new Vector2(i * 16 + 25, j * 16 + 32));
                                }
                            }
                        }
                        else
                        {
                            boneDustEffectTimer = 0;
                        }


                        int dustChoice = -1;
                        if (style >= 1)
                        {
                            dustChoice = DustID.FlameBurst; // A green dust.
                        }

                        // We can support different dust for different styles here
                        if (dustChoice != -1)
                        {
                            if (!Main.gamePaused && Main.instance.IsActive && (!Lighting.UpdateEveryFrame || Main.rand.NextBool(4)) && player.statLife < player.statLifeMax2)
                            {
                                bool bossActive = false;
                                for (int n = 0; n < Main.maxNPCs; n++)
                                {
                                    if (Main.npc[n].active && Main.npc[n].boss)
                                    {
                                        bossActive = true;
                                        break;
                                    }
                                }

                                // Only heal when no bosses are alive and the player is standing still
                                if (!bossActive && player.velocity == Vector2.Zero)
                                {
                                    // Wind up 1
                                    if (bonfireEffectTimer > 0 && bonfireEffectTimer <= 12)
                                    {
                                        var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, dustChoice, Alpha: 120);
                                        HandleDust(ref dust, Main.rand.Next(50, 100) * 0.06f, 40f, new Vector2(i * 16 + 25, j * 16 + 32));

                                        for (int q = 0; q < 3; q++)
                                        {
                                            var dust2 = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, dustChoice, Alpha: 120);
                                            HandleDust(ref dust2, Main.rand.Next(50, 100) * 0.015f, 10f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }
                                    }

                                    // Wind up 2
                                    if (bonfireEffectTimer > 12 && bonfireEffectTimer <= 20)
                                    {
                                        var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                        HandleDust(ref dust, Main.rand.Next(50, 100) * 0.07f, 45f, new Vector2(i * 16 + 25, j * 16 + 32));

                                        for (int q = 0; q < 5; q++)
                                        {
                                            var dust2 = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                            HandleDust(ref dust2, Main.rand.Next(50, 100) * 0.025f, 15f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }
                                    }

                                    // Wind up 3
                                    if (bonfireEffectTimer > 20 && bonfireEffectTimer <= 28)
                                    {
                                        for (int q = 0; q < 5; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.08f, 50f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }

                                        for (int q = 0; q < 10; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.035f, 20f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }
                                    }

                                    // Blast
                                    if (bonfireEffectTimer == 28)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item20, new Vector2(i * 16 + 25, j * 16 + 32));

                                        for (int q = 0; q < 30; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, dustChoice, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.2f, 10f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }

                                        for (int q = 0; q < 30; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, dustChoice, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.12f, 30f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }
                                    }

                                    // Full effect
                                    if (bonfireEffectTimer > 28)
                                    {
                                        for (int q = 0; q < 5; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.085f, 55f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }

                                        for (int q = 0; q < 17; q++)
                                        {
                                            var dust = Dust.NewDustDirect(new Vector2(i * 16 + 25, j * 16 + 32), 40, 56, DustID.FlameBurst, Alpha: 120);
                                            HandleDust(ref dust, Main.rand.Next(50, 100) * 0.045f, 25f, new Vector2(i * 16 + 25, j * 16 + 32));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            Vector2 zero = new Vector2(Main.offScreenRange, Main.offScreenRange);
            if (Main.drawToScreen)
            {
                zero = Vector2.Zero;
            }

            int height = tile.TileFrameY % AnimationFrameHeight == 54 ? 18 : 16;
            int animate = 0;
            if (tile.TileFrameY >= 74)
            {
                animate = Main.tileFrame[Type] * AnimationFrameHeight;
            }

            Main.spriteBatch.Draw(TextureAssets.Tile[Type].Value, new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.TileFrameX, tile.TileFrameY + animate, 16, height), Lighting.GetColor(i, j));
            Main.spriteBatch.Draw((Texture2D)Mod.Assets.Request<Texture2D>("Tiles/BonfireCheckpoint_Glow"), new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y) + zero, new Rectangle(tile.TileFrameX, tile.TileFrameY + animate, 16, height), Color.White);

            return false;
        }

        public override void MouseOver(int i, int j)
        {
            var player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<BonfirePlaceable>();
        }

        public override bool RightClick(int i, int j)
        {
            var tile = Framing.GetTileSafely(i, j);
            if (tile.TileFrameY / 74 == 0)
            {
                var player = Main.LocalPlayer;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/bonfire-lit") with { Volume = 0.2f }, player.Center);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("UI.BonfireLit"), 250, 110, 90);

                int spawnX = (int)((player.position.X + player.width / 2.0) / 16.0);
                int spawnY = (int)((player.position.Y + player.height) / 16.0);
                player.ChangeSpawn(spawnX, spawnY);
                player.FindSpawn();

                if (tsorcRevampWorld.LitBonfireList == null || tsorcRevampWorld.LitBonfireList.Count == 0)
                {
                    tsorcRevampWorld.LitBonfireList = new List<Vector2>();
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("UI.BonfireTP"), Color.Orange);
                }

                tsorcRevampWorld.LitBonfireList.Add(new Vector2(i, j));

                int x = i - Main.tile[i, j].TileFrameX / 18 % 3; // 16 pixels in a block + 2 pixels for the buffer. 3 because its 3 blocks wide
                int y = j - Main.tile[i, j].TileFrameY / 18 % 4; // 4 because it is 4 blocks tall
                for (int l = x; l < x + 3; l++)             // this chunk of code basically makes it so that when you right click one tile, 
                {              // because 3x4 tile         // it counts as the whole 3x4 tile, not 4 individual tiles that can all be clicked
                    for (int m = y; m < y + 4; m++)         //Code taken from VoidMonolith - example mod
                    {
                        var tile2 = Framing.GetTileSafely(l, m);
                        if (tile2.HasTile && tile2.TileType == Type)
                        {
                            if (tile2.TileFrameY < 74)
                            {
                                tile2.TileFrameY += 74;
                            }
                            else
                            {
                                tile2.TileFrameY -= 74;
                            }
                        }

                    }
                }

                //syncs the tile frames
                NetMessage.SendTileSquare(-1, x, y + 1, 5);

                //Tell the server to add the new bonfire to the list
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    ModPacket bonfirePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                    bonfirePacket.Write(tsorcPacketID.SyncBonfire);
                    bonfirePacket.WriteVector2(new Vector2(i, j));
                    bonfirePacket.Send();
                }

                //Tell the server to sync the list to the clients
                NetMessage.SendData(MessageID.WorldData);
            }
            else if (tile.TileFrameY / 74 >= 1)
            {
                BonfireUIState.Visible = !BonfireUIState.Visible;
                SoundEngine.PlaySound(BonfireUIState.Visible ? SoundID.MenuOpen : SoundID.MenuClose);
            }

            return true;
        }

        private static void HandleDust(ref Dust dust, float scaleFactor, float scaleFactor2, Vector2 position)
        {
            dust.noGravity = true;
            dust.velocity *= 2.75f;
            dust.fadeIn = 1.3f;

            var vectorOther = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
            vectorOther.Normalize();
            vectorOther *= scaleFactor;
            dust.velocity = vectorOther;

            vectorOther.Normalize();
            vectorOther *= scaleFactor2;
            dust.position = position - vectorOther;
        }
    }
}