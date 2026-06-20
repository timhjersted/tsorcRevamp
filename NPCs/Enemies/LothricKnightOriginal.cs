using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // Pre-port backup of LothricKnight (bespoke jump-ladder movement + hand-rolled targeting).
    // Kept for A/B reference; the live LothricKnight now runs the shared FighterAI + SF4 nav + poise.
    public class LothricKnightOriginal : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/LothricKnight";

        //AI
        bool slashing = false;
        bool jumpSlashing = false;
        bool shielding = false;
        bool stabbing = false;

        //Anim
        int shieldFrame;
        int shieldAnimTimer;
        bool countingUP = false;

        public int lothricDamage = 16;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 18;
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
        }
        public override void SetDefaults()
        {
            NPC.timeLeft = 60;
            NPC.npcSlots = 5;
            NPC.knockBackResist = 0.15f;
            NPC.aiStyle = -1;
            NPC.damage = 43;
            NPC.defense = 26;
            NPC.height = 44;
            NPC.width = 20;
            NPC.lifeMax = 750;
            if (Main.hardMode)
            {
                NPC.lifeMax = 1400;
                NPC.defense = 40;
                NPC.value = 7000;
                lothricDamage = 24;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 2500;
                NPC.defense = 60;
                NPC.damage = 80;
                NPC.value = 10000;
                lothricDamage = 34;
            }
            NPC.value = 3750;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.LothricKnightBanner>();
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.HealthScaledSpeedBase = 2.5f;
            g.HealthScaledSpeedMultiplier = -2.0f;
        }


        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 10; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 80; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 2.5f * hit.HitDirection, -1.5f, 70, default(Color), 1f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, 1.5f * hit.HitDirection, -2.5f, 50, default(Color), 1f);
                }
            }
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            UsefulFunctions.DustRing(NPC.Center, 300, DustID.YellowTorch, 5, 2f);
            if (NPC.Distance(player.Center) < 300)
            {
                player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 2);
            }
            if (Main.hardMode && NPC.Distance(player.Center) < 300)
            {
                player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
            }

            int lifePercentage = (NPC.life * 100) / NPC.lifeMax;
            float acceleration = 0.02f;
            float top_speed = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ComputeHealthScaledSpeed(NPC, 2.5f);
            float braking_power = 0.1f;

            int damage = NPC.damage / 4;

            #region target/face player, respond to boredom

            if (NPC.ai[0] == 0 && !jumpSlashing && !slashing && !stabbing)
            {
                NPC.TargetClosest(true);
            }
            if (NPC.velocity.X == 0 && !jumpSlashing && !shielding && !slashing && !stabbing)
            {
                NPC.ai[0]++;
                if (NPC.ai[0] > 120 && NPC.velocity.Y == 0)
                {
                    NPC.direction *= -1;
                    NPC.spriteDirection = NPC.direction;
                    NPC.ai[0] = 50;
                }
            }

            if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
            {
                NPC.ai[0] = 0;
            }

            #endregion

            #region melee movement

            if (NPC.ai[1] >= 390 && NPC.ai[1] <= 420)
            {
                top_speed = (lifePercentage * -0.015f) + 2.5f;
            }

            if (Math.Abs(NPC.velocity.X) > top_speed && NPC.velocity.Y == 0)
            {
                NPC.velocity *= (1f - braking_power);
            }
            if (NPC.velocity.X > 10.5f)
            {
                NPC.velocity.X = 10.5f;
            }
            if (NPC.velocity.X < -10.5f)
            {
                NPC.velocity.X = -10.5f;
            }
            else
            {
                NPC.velocity.X += NPC.direction * acceleration;
            }

            if (NPC.direction == 1)
            {
                if (NPC.velocity.X > -top_speed)
                {
                    NPC.velocity.X += 0.085f;
                }
                NPC.netUpdate = true;
            }
            if (NPC.direction == -1)
            {
                if (NPC.velocity.X < top_speed)
                {
                    NPC.velocity.X += -0.085f;
                }
                NPC.netUpdate = true;
            }

            if (Math.Abs(NPC.velocity.X) > 4f)
            {
                NPC.knockBackResist = 0;
            }
            if (Math.Abs(NPC.velocity.Y) > 0.1f)
            {
                NPC.knockBackResist = 0;
            }
            if (stabbing || jumpSlashing)
            {
                NPC.knockBackResist = 0;
            }
            else
            {
                NPC.knockBackResist = 0.1f;
            }

            NPC.noTileCollide = false;

            int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            if (Main.tile[(int)NPC.position.X / 16, y_below_feet].TileType == TileID.Platforms && Main.tile[(int)(NPC.position.X + (float)NPC.width) / 16, y_below_feet].TileType == TileID.Platforms && NPC.position.Y < (player.position.Y - 4 * 16))
            {
                NPC.noTileCollide = true;
            }

            #endregion

            #region check if standing on a solid tile
            bool standing_on_solid_tile = false;
            if (NPC.velocity.Y == 0f)
            {
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++)
                {
                    if (Main.tile[l, y_below_feet] == null)
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType])
                    {
                        standing_on_solid_tile = true;
                        break;
                    }
                }
            }
            #endregion

            #region new Tile()s, jumping
            if (standing_on_solid_tile && !slashing && !shielding && !jumpSlashing && !stabbing)
            {
                int x_in_front = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
                int y_above_feet = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);

                if (NPC.position.Y > player.position.Y + 3 * 16 && NPC.position.Y < player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    NPC.ai[3] = 22;
                    NPC.velocity.Y = -8f;
                    NPC.netUpdate = true;
                }

                if (NPC.position.Y >= player.position.Y + 8 * 16 && Math.Abs(NPC.Center.X - player.Center.X) < 3f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    NPC.ai[3] = 10;
                    NPC.velocity.Y = -9.5f;
                    NPC.netUpdate = true;
                }

                if (Main.tile[x_in_front, y_above_feet] == null) { Main.tile[x_in_front, y_above_feet].ClearTile(); }
                if (Main.tile[x_in_front, y_above_feet - 1] == null) { Main.tile[x_in_front, y_above_feet - 1].ClearTile(); }
                if (Main.tile[x_in_front, y_above_feet - 2] == null) { Main.tile[x_in_front, y_above_feet - 2].ClearTile(); }
                if (Main.tile[x_in_front, y_above_feet - 3] == null) { Main.tile[x_in_front, y_above_feet - 3].ClearTile(); }
                if (Main.tile[x_in_front, y_above_feet + 1] == null) { Main.tile[x_in_front, y_above_feet + 1].ClearTile(); }
                if (Main.tile[x_in_front + NPC.direction, y_above_feet - 1] == null) { Main.tile[x_in_front + NPC.direction, y_above_feet - 1].ClearTile(); }
                if (Main.tile[x_in_front + NPC.direction, y_above_feet + 1] == null) { Main.tile[x_in_front + NPC.direction, y_above_feet + 1].ClearTile(); }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                    {
                        if (Main.tile[x_in_front, y_above_feet - 2].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].TileType])
                        {
                            if (Main.tile[x_in_front, y_above_feet - 3].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].TileType])
                            {
                                NPC.velocity.Y = -8f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                NPC.velocity.Y = -7f;
                                NPC.netUpdate = true;
                            }
                        }
                        else if (Main.tile[x_in_front, y_above_feet - 1].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].TileType])
                        {
                            NPC.velocity.Y = -6f;
                            NPC.netUpdate = true;
                        }
                        else if (Main.tile[x_in_front, y_above_feet].HasTile && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].TileType])
                        {
                            NPC.velocity.Y = -5f;
                            NPC.netUpdate = true;
                        }
                        else if (NPC.directionY < 0 && (!Main.tile[x_in_front, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].TileType]) && (!Main.tile[x_in_front + NPC.direction, y_above_feet + 1].HasTile || !Main.tileSolid[(int)Main.tile[x_in_front + NPC.direction, y_above_feet + 1].TileType]))
                        {
                            NPC.velocity.Y = -8f;
                            NPC.velocity.X = NPC.velocity.X * 1.5f;
                            NPC.netUpdate = true;
                        }
                    }
                }
            }

            #endregion

            #region attacks

            if (NPC.ai[3] < 10)
            {
                ++NPC.ai[3];
            }

            if (!jumpSlashing && !stabbing)
            {
                if (NPC.ai[3] == 10 && NPC.Distance(player.Center) < 55 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    slashing = true;
                    shielding = false;
                }

                if (slashing)
                {
                    ++NPC.ai[3];

                    if (NPC.ai[3] < 26)
                    {
                        if (NPC.direction == 1)
                        {
                            NPC.velocity.X -= 0.25f;
                            if (NPC.velocity.X < 0) { NPC.velocity.X = 0; }
                        }
                        else
                        {
                            NPC.velocity.X += 0.25f;
                            if (NPC.velocity.X > 0) { NPC.velocity.X = 0; }
                        }
                    }

                    if (NPC.ai[3] == 26)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                if (!standing_on_solid_tile)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -66), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                            else
                            {
                                if (!standing_on_solid_tile)
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -66), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                                else
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-2, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.2f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            }
                        }
                    }

                    if (NPC.ai[3] >= 49)
                    {
                        slashing = false;
                        NPC.ai[3] = 0;
                    }
                }
            }

            if (NPC.ai[1] < 420)
            {
                ++NPC.ai[1];
            }

            if (NPC.ai[1] >= 390 && NPC.ai[1] <= 400)
            {
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 1.5f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }

            if (NPC.ai[1] >= 400 && NPC.ai[1] < 442)
            {
                if (NPC.direction == 1)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 9, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
                else
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(NPC.position.X + 3, NPC.position.Y), 4, 4, 183, NPC.velocity.X, NPC.velocity.Y, 180, default(Color), 0.8f)];
                    dust2.noGravity = true;
                    dust2.fadeIn = .3f;
                    dust2.velocity += NPC.velocity;
                }
            }

            if (!slashing && !stabbing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 150 && NPC.Distance(player.Center) >= 55 && NPC.velocity.Y == 0 && standing_on_solid_tile && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    jumpSlashing = true;
                    shielding = false;
                }

                if (jumpSlashing)
                {
                    ++NPC.ai[1];
                    if (NPC.ai[1] < 436)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; if (NPC.velocity.X < 0) { NPC.velocity.X = 0; } }
                        else { NPC.velocity.X += 0.15f; if (NPC.velocity.X > 0) { NPC.velocity.X = 0; } }
                    }

                    if (NPC.ai[1] == 436)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X += 5f; NPC.velocity.Y -= 3f; }
                        else { NPC.velocity.X -= 5f; NPC.velocity.Y -= 3f; }
                    }

                    if (NPC.ai[1] == 442)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f }, NPC.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(24, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                            else
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-8, -20), new Vector2(0, 4f), ModContent.ProjectileType<Projectiles.Enemy.MediumWeaponSlash>(), (int)(lothricDamage * 1.4f), 5, Main.myPlayer, NPC.whoAmI, 0);
                        }
                    }
                    if (NPC.ai[1] > 470 && NPC.ai[1] < 489)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.3f; if (NPC.velocity.X < 0) { NPC.velocity.X = 0; } }
                        else { NPC.velocity.X += 0.3f; if (NPC.velocity.X > 0) { NPC.velocity.X = 0; } }
                    }
                    if (NPC.ai[1] >= 489)
                    {
                        jumpSlashing = false;
                        NPC.ai[1] = 150;
                    }
                }
            }

            if (!slashing && !jumpSlashing)
            {
                if (NPC.ai[1] == 420 && NPC.Distance(player.Center) < 300 && NPC.Distance(player.Center) >= 150 && NPC.velocity.Y == 0 && Math.Abs(NPC.Center.Y - player.Center.Y) < 6.5f * 16 && Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    stabbing = true;
                    shielding = false;
                }

                if (stabbing)
                {
                    ++NPC.ai[1];

                    if (NPC.ai[1] < 436)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; if (NPC.velocity.X < 0) { NPC.velocity.X = 0; } }
                        else { NPC.velocity.X += 0.15f; if (NPC.velocity.X > 0) { NPC.velocity.X = 0; } }
                    }

                    if (NPC.ai[1] == 436)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 1.0f, PitchVariance = 0.3f }, player.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (NPC.direction == 1)
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), (int)(lothricDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X += 10.5f;
                                NPC.velocity.Y -= 2f;
                            }
                            else
                            {
                                Projectile stab = Main.projectile[Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-44, -2), new Vector2(0, 0), ModContent.ProjectileType<Projectiles.Enemy.Spearhead>(), (int)(lothricDamage * 1.5f), 5, Main.myPlayer, NPC.whoAmI, 0)];
                                NPC.velocity.X -= 10.5f;
                                NPC.velocity.Y -= 2f;
                            }
                        }
                    }

                    if (NPC.ai[1] > 470 && NPC.ai[1] < 489)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.3f; if (NPC.velocity.X < 0) { NPC.velocity.X = 0; } }
                        else { NPC.velocity.X += 0.3f; if (NPC.velocity.X > 0) { NPC.velocity.X = 0; } }
                    }

                    if (NPC.ai[1] > 489)
                    {
                        NPC.ai[1] = 280;
                        stabbing = false;
                    }
                }
            }

            if (shielding || NPC.Distance(player.Center) < 220 || NPC.ai[2] > 300)
            {
                NPC.ai[2]++;

                if (!jumpSlashing && !slashing && !stabbing && NPC.velocity.Y == 0)
                {
                    if (NPC.ai[2] > 300 && NPC.ai[2] <= 310)
                    {
                        if (NPC.direction == 1) { NPC.velocity.X -= 0.15f; }
                        else { NPC.velocity.X += 0.15f; }
                    }

                    if (NPC.ai[2] > 310)
                    {
                        NPC.velocity.X = 0;
                        shielding = true;
                    }

                    if (NPC.ai[2] > 500)
                    {
                        shielding = false;
                        NPC.ai[2] = 0;
                    }
                }
            }
            #endregion
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            int shieldPower = NPC.defense * 2;

            if (shielding)
            {
                if (NPC.direction == 1)
                {
                    if (player.position.X > NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 340) { NPC.ai[2] -= 35; }
                    }
                }
                else
                {
                    if (player.position.X < NPC.position.X)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                        modifiers.SourceDamage.Flat -= shieldPower;
                        if (NPC.ai[2] > 340) { NPC.ai[2] -= 35; }
                    }
                }
            }

            if (NPC.direction == 1)
            {
                if (player.position.X < NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                }
            }
            else
            {
                if (player.position.X > NPC.position.X)
                {
                    CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                    modifiers.FinalDamage *= 2;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                }
            }

            NPC.ai[2] += 10;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[NPC.target];

            int direction = modifiers.HitDirection;

            Type hm = typeof(NPC.HitModifiers);
            PropertyInfo prop = hm.GetProperty("HitDirectionOverride");
            int? over = (int?)prop.GetValue(modifiers);

            if (over != null && over != 0)
            {
                direction = over.Value;
            }

            int shieldPower = NPC.defense * 3;

            if (projectile.type != ModContent.ProjectileType<Items.Weapons.Ranged.Specialist.BlizzardBlasterShot>())
            {
                if (shielding)
                {
                    if (NPC.direction == 1)
                    {
                        if (projectile.Center.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) { NPC.ai[1] += 70; }
                            if (NPC.ai[2] > 340) { NPC.ai[2] -= 35; }
                        }
                        else if (direction == -1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) { NPC.ai[1] += 80; }
                            if (NPC.ai[2] > 340) { NPC.ai[2] -= 35; }
                        }
                    }
                    else
                    {
                        if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) { NPC.ai[1] += 70; }
                            if (NPC.ai[2] > 350) { NPC.ai[2] -= 35; }
                        }
                        else if (direction == 1 && (projectile.DamageType != DamageClass.Melee || projectile.aiStyle == 19))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f }, NPC.Center);
                            modifiers.SourceDamage.Flat -= shieldPower;
                            modifiers.Knockback *= 0f;
                            if (NPC.ai[1] < 340) { NPC.ai[1] += 80; }
                            if (NPC.ai[2] > 340) { NPC.ai[2] -= 35; }
                        }
                    }
                }

                if (NPC.direction == 1)
                {
                    if (projectile.oldPosition.X < NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == 1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }
                else
                {
                    if (projectile.oldPosition.X > NPC.Center.X && projectile.DamageType == DamageClass.Melee && projectile.aiStyle != 19)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                    else if (direction == -1)
                    {
                        CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.WeakSpot"), false, false);
                        modifiers.FinalDamage *= 2;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit18 with { PitchVariance = 0.3f }, NPC.Center);
                    }
                }

                if (NPC.Distance(player.Center) > 220 && !shielding)
                {
                    NPC.ai[2] += 120;
                }

                if (NPC.ai[1] < 400)
                {
                    NPC.ai[1] += 10;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (spawnInfo.Water) return 0f;
            if (spawnInfo.Player.ZoneGlowshroom) return 0f;

            if (spawnInfo.Player.ZoneDungeon) return chance = 0.02f;

            if (tsorcRevampWorld.SuperHardMode && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return 0.03f;

            if (Main.bloodMoon && spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneSkyHeight && NPC.downedBoss3) return chance = 0.02f;
            if (Main.bloodMoon && NPC.downedBoss3 && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.02f;

            if (NPC.downedBoss3 && spawnInfo.Player.ZoneOverworldHeight && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.005f;
            if (NPC.downedBoss3 && spawnInfo.Player.ZoneOverworldHeight && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.015f;
            if (NPC.downedBoss3 && !(spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneBeach || spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneUnderworldHeight)) return chance = 0.003f;

            return chance;
        }

        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.SoulCoin>(), 1, 12, 24));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofLight));
            npcLoot.Add(hmCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Items.Potions.RadiantLifegem>(), 4, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpikedIronShield>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.LostUndeadSoul>(), 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.RagePotion, 13));
            npcLoot.Add(ItemDropRule.Common(ItemID.WrathPotion, 13));
        }

        #region Drawing and Animation

        public override void DrawEffects(ref Color drawColor) { }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Vector2 drawOrigin = new Vector2(NPC.position.X, NPC.position.Y);
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if ((NPC.velocity.X > 5f || NPC.velocity.X < -5f) && stabbing)
            {
                for (int k = 0; k < NPC.oldPos.Length; k++)
                {
                    Vector2 drawPos = NPC.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, NPC.gfxOffY);
                    Color color = NPC.GetAlpha(lightColor) * ((float)(NPC.oldPos.Length - k) / (float)NPC.oldPos.Length);
                    spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, drawPos, new Rectangle(NPC.frame.X, NPC.frame.Y, 74, 56), color, NPC.rotation, new Vector2(NPC.position.X + 26, NPC.position.Y + 12), NPC.scale, effects, 0f);
                }
            }
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D shieldTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/LothricKnight_Shield");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = shieldTexture.Frame(1, 15, 0, shieldFrame);
            if (shielding && NPC.velocity.X == 0 && !jumpSlashing && !slashing && !stabbing)
            {
                spriteBatch.Draw(shieldTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(43, 32), NPC.scale, effects, 0f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.X != 0)
            {
                float framecountspeed = Math.Abs(NPC.velocity.X) * 2.2f;
                NPC.frameCounter += framecountspeed;
                NPC.spriteDirection = NPC.direction;

                if (NPC.frameCounter < 12) { NPC.frame.Y = 2 * frameHeight; }
                else if (NPC.frameCounter < 24) { NPC.frame.Y = 3 * frameHeight; }
                else if (NPC.frameCounter < 36) { NPC.frame.Y = 4 * frameHeight; }
                else if (NPC.frameCounter < 48) { NPC.frame.Y = 5 * frameHeight; }
                else if (NPC.frameCounter < 60) { NPC.frame.Y = 6 * frameHeight; }
                else if (NPC.frameCounter < 72) { NPC.frame.Y = 7 * frameHeight; }
                else if (NPC.frameCounter < 84) { NPC.frame.Y = 8 * frameHeight; }
                else if (NPC.frameCounter < 96) { NPC.frame.Y = 9 * frameHeight; }
                else { NPC.frameCounter = 0; }
            }

            if (NPC.velocity.Y != 0 && (!jumpSlashing && !shielding && !stabbing))
                NPC.frame.Y = 1 * frameHeight;

            if (slashing)
            {
                NPC.spriteDirection = NPC.direction;
                if (NPC.ai[3] < 18) { NPC.frame.Y = 11 * frameHeight; }
                else if (NPC.ai[3] < 26) { NPC.frame.Y = 12 * frameHeight; }
                else if (NPC.ai[3] < 29) { NPC.frame.Y = 13 * frameHeight; }
                else if (NPC.ai[3] < 32) { NPC.frame.Y = 14 * frameHeight; }
                else if (NPC.ai[3] < 35) { NPC.frame.Y = 15 * frameHeight; }
                else if (NPC.ai[3] < 49) { NPC.frame.Y = 16 * frameHeight; }
            }

            if (jumpSlashing)
            {
                NPC.spriteDirection = NPC.direction;
                if (NPC.ai[1] < 428) { NPC.frame.Y = 11 * frameHeight; }
                else if (NPC.ai[1] < 436) { NPC.frame.Y = 12 * frameHeight; }
                else if (NPC.ai[1] < 439) { NPC.frame.Y = 13 * frameHeight; }
                else if (NPC.ai[1] < 442) { NPC.frame.Y = 14 * frameHeight; }
                else if (NPC.ai[1] < 445) { NPC.frame.Y = 15 * frameHeight; }
                else if (NPC.ai[1] < 489) { NPC.frame.Y = 16 * frameHeight; }
            }

            if (stabbing)
            {
                NPC.spriteDirection = NPC.direction;
                if (NPC.ai[1] < 436) { NPC.frame.Y = 2 * frameHeight; }
                else if (NPC.ai[1] < 470) { NPC.frame.Y = 17 * frameHeight; }
                else if (NPC.ai[1] < 475) { NPC.frame.Y = 15 * frameHeight; }
                else if (NPC.ai[1] < 489) { NPC.frame.Y = 16 * frameHeight; }
            }

            if (NPC.velocity.X == 0 && NPC.velocity.Y == 0 && shielding && !jumpSlashing && !slashing && !stabbing)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frame.Y = 10 * frameHeight;
            }

            if (shielding)
            {
                shieldFrame = shieldAnimTimer / 4;
                if (shieldFrame == 0) { countingUP = true; }
                if (shieldFrame <= 14 && countingUP) { shieldAnimTimer++; }
                if (shieldFrame == 14) { countingUP = false; }
                if (shieldFrame >= 0 && !countingUP) { shieldAnimTimer--; }
            }
        }

        #endregion
    }
}
