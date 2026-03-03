using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Utilities;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Chaos;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Chaos : ModNPC
    {
        int teleportTimer = 0;
        Vector2 teleportPosition;

        int xOffset = 0;
        int yOffset = 0;
        int telegraphTimer = 0;
        Vector2 anchoredPosition;

        float angle = 0;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 130;
            NPC.height = 160;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.npcSlots = 100;
            NPC.aiStyle = -1;//22;
            AnimationType = -1;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.damage = 100;
            NPC.defense = 80;
            NPC.lifeMax = 450000;
            NPC.value = 700000f;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Chaos.DespawnHandler"), Color.Yellow, DustID.GoldFlame);
        }
        NPCDespawnHandler despawnHandler;
        private enum EyeFrame
        {
            Idle1,
            Idle2,
            Idle3,
            Idle4,
            Idle5,
            Idle6,
            Idle7,
            Idle8,
        }


        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += 1.0;
            switch (NPC.frameCounter)
            {
                case 4 * 1:
                    NPC.frame.Y = (int)EyeFrame.Idle1 * frameHeight;
                    break;
                case 4 * 2:
                    NPC.frame.Y = (int)EyeFrame.Idle2 * frameHeight;
                    break;
                case 4 * 3:
                    NPC.frame.Y = (int)EyeFrame.Idle3 * frameHeight;
                    break;
                case 4 * 4:
                    NPC.frame.Y = (int)EyeFrame.Idle4 * frameHeight;
                    break;
                case 4 * 5:
                    NPC.frame.Y = (int)EyeFrame.Idle5 * frameHeight;
                    break;
                case 4 * 6:
                    NPC.frame.Y = (int)EyeFrame.Idle6 * frameHeight;
                    break;
                case 4 * 7:
                    NPC.frame.Y = (int)EyeFrame.Idle7 * frameHeight;
                    break;
                case 4 * 8:
                    NPC.frame.Y = (int)EyeFrame.Idle8 * frameHeight;
                    NPC.frameCounter = 0;
                    break;
            }
        }

        private void ShootProjectile(NPC NPC, Player target, int speed, int type, bool hasTileCollide, int count, float startAngle, float angleDecrement, Vector2 startPosition, float radius)
        {
            Vector2 distance = target.Center - startPosition;
            Vector2 distanceNormalized = distance.SafeNormalize(Vector2.UnitX);
            float angle = startAngle;
            for (int i = 0; i < count; i++)
            {
                int projectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), startPosition, (distanceNormalized.RotatedBy(MathHelper.ToRadians(angle)) * speed).RotatedByRandom(MathHelper.ToRadians(radius)), type, NPC.damage / 6, 1);

                Main.projectile[projectile].tileCollide = hasTileCollide;
                Main.projectile[projectile].friendly = false;
                Main.projectile[projectile].hostile = true;
                angle -= angleDecrement;
            }
        }

        private void FloatAbovePlayer(Player target, float offset, int floatDirection, NPC NPC, float speed, float inertia, float yOffset, Vector2 destination)
        {
            Vector2 toPlayer = target.Center - NPC.Center;

            float offsetX = 7.5f;

            Vector2 abovePlayer = target.Top + new Vector2(NPC.direction * offsetX, -NPC.height + offset);

            if (floatDirection == 2)
            {
                abovePlayer = target.Center + new Vector2((NPC.direction + offset) * offsetX, yOffset);
            }

            if (destination != Vector2.Zero)
            {
                abovePlayer = destination;
            }


            Vector2 toAbovePlayer = abovePlayer - NPC.Center;
            Vector2 toAbovePlayerNormalized = toAbovePlayer.SafeNormalize(Vector2.UnitY);

            // The NPC tries to go towards the offsetX position, but most likely it will never get there exactly, or close to if the player is moving
            // This checks if the npc is "70% there", and then changes direction

            // If the boss is somehow below the player, move faster to catch up

            Vector2 moveTo = toAbovePlayerNormalized * speed;
            NPC.velocity = (NPC.velocity * (inertia - 1) + moveTo) / inertia;
        }

        int attackTimer = 600;
        int attackTracker = 1;
        int phase = 1;
        private Vector2[] oldPositions = new Vector2[5];
        int flyParity = -25;
        int flyPosition = 0;
        int dashTimer = 0;
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Color RealDrawColor = new Color(100 + drawColor.R / 2, 100 + drawColor.G / 2, 100 + drawColor.B / 2);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Rectangle sourceRect = NPC.frame;

            Vector2 origin2 = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);

            for (int i = 1; i < oldPositions.Length; i++)
            {
                int WhichOne = i * (-1) + oldPositions.Length;
                float alpha = 1f * (1f + i / (float)oldPositions.Length);
                float scale = 1f * (1f - i / (float)oldPositions.Length);
                Vector2 drawPos = oldPositions[WhichOne] - screenPos;
                spriteBatch.Draw(
                    texture,
                    drawPos,
                    sourceRect,
                    RealDrawColor * (alpha / 3),
                    NPC.rotation,
                    origin2,
                    NPC.scale,
                    SpriteEffects.None,
                    0f
                );
            }
            spriteBatch.Draw(
                texture,
                NPC.Center - screenPos,
                sourceRect,
                RealDrawColor,
                NPC.rotation,
                origin2,
                NPC.scale,
                SpriteEffects.None,
                0f
            );

            return false;
        }

        public override void AI()
        {
            for (int i = oldPositions.Length - 1; i > 0; i--)
            {
                oldPositions[i] = oldPositions[i - 1];
            }
            oldPositions[1] = NPC.Center;
            if (NPC.direction == 1)
            {
                NPC.spriteDirection = 1;
            }
            if (NPC.direction == -1)
            {
                NPC.spriteDirection = -1;
            }
            NPC.rotation = NPC.velocity.X * 0.04f;

            Player target = Main.player[NPC.target];
            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
            {
                NPC.TargetClosest();
            }
            if (target.dead)
            {
                NPC.velocity.Y -= 3f;
                NPC.EncourageDespawn(10);
                return;
            }

            attackTimer--;
            dashTimer--;
            teleportTimer--;
            telegraphTimer--;


            if (phase == 1 && NPC.life <= NPC.lifeMax * 0.66f)
            {
                phase = 2;
                SoundEngine.PlaySound(SoundID.Roar);
                attackTimer = 600;
                attackTracker = 1;
            }

            if (phase == 2 && NPC.life <= NPC.lifeMax * 0.33f)
            {
                phase = 3;
                SoundEngine.PlaySound(SoundID.ForceRoarPitched);
                attackTimer = 600;
                attackTracker = 1;
            }

            if (phase == 1)
            {
                if (attackTracker == 1)
                {
                    FloatAbovePlayer(target, 0, 0, NPC, 30, 100, 0, target.Center);
                    if (attackTimer % 45 == 0)
                    {
                        ShootProjectile(NPC, target, 15, ProjectileID.Fireball, false, 5, 45, 22.5f, NPC.Center - new Vector2(0, 40), 0);
                    }


                    if (attackTimer == 0)
                    {
                        attackTimer = 400;
                        attackTracker++;
                    }
                }

                else if (attackTracker == 2)
                {
                    if (attackTimer % 75 == 0)
                    {
                        Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.velocity = new Vector2(35f, 35f) * direction;
                        ShootProjectile(NPC, target, 15, ProjectileID.DemonSickle, false, 20, 0, 18, NPC.Center - new Vector2(0, 40), 0);
                    }
                    else
                    {
                        NPC.velocity *= 0.97f;
                    }


                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker++;
                        flyPosition = 0;
                    }
                }
                else if (attackTracker == 3)
                {
                    flyPosition += flyParity;
                    FloatAbovePlayer(target, 0, 0, NPC, 30, 10, 0, new Vector2(target.Center.X + flyPosition, target.Center.Y - 500));

                    if (attackTimer % 80 == 0)
                    {
                        flyParity *= -1;
                    }

                    if (attackTimer % 10 == 0)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, -10), ModContent.ProjectileType<ChaosBlackFire>(), NPC.damage / 6, 1);
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker = 1;
                    }
                }
            }
            else if (phase == 2)
            {
                if (attackTracker == 1)
                {
                    FloatAbovePlayer(target, 0, 0, NPC, 5, 10, 0, target.Center);
                    if (attackTimer % 5 == 0)
                    {
                        ShootProjectile(NPC, target, 10, ProjectileID.Flames, false, 1, 0, 0, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer % 60 == 0)
                    {
                        ShootProjectile(NPC, target, 15, ProjectileID.Fireball, false, 5, 45, 22.5f, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker++;
                    }
                }
                else if (attackTracker == 2)
                {
                    if (attackTimer % 40 == 0)
                    {
                        Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                        NPC.velocity = new Vector2(35f, 35f) * direction;
                        dashTimer = 30;
                    }
                    else
                    {
                        NPC.velocity *= 0.97f;
                    }
                    float currentSpeed = (NPC.velocity.Y + NPC.velocity.X) / 2;
                    if (dashTimer > 0)
                    {
                        if (attackTimer % 10 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(0, -10), ModContent.ProjectileType<ChaosBlackFire>(), NPC.damage / 6, 1);
                        }
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker++;
                    }
                }
                else if (attackTracker == 3)
                {
                    NPC.velocity *= 0.97f;
                    if (attackTimer % 50 == 0)
                    {
                        ShootProjectile(NPC, target, 15, ProjectileID.Fireball, false, 20, Main.rand.Next(-360, 361), 18, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer % 20 == 0)
                    {
                        ShootProjectile(NPC, target, 15, ProjectileID.Fireball, false, 10, Main.rand.Next(-360, 361), 36, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker = 1;
                    }
                }
            }

            else if (phase == 3)
            {
                if (attackTracker == 1)
                {
                    //shadow flame teleports
                    FloatAbovePlayer(target, 0, 0, NPC, 10, 20, 0, target.Center);
                    if (attackTimer % 5 == 0)
                    {
                        ShootProjectile(NPC, target, 10, ProjectileID.ShadowFlame, false, 1, 0, 0, NPC.Center - new Vector2(0, 40), 30);
                    }

                    if (attackTimer % 120 == 0)
                    {
                        teleportTimer = 80;
                        teleportPosition = new Vector2(target.Center.X + Main.rand.Next(-300, 300), target.Center.Y + Main.rand.Next(-300, 300));
                    }

                    if (teleportTimer > 0)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Dust.NewDust(teleportPosition, 130, 140, DustID.Shadowflame);
                        }
                    }

                    if (teleportTimer == 0)
                    {
                        NPC.Center = teleportPosition;
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker++;
                    }
                }

                if (attackTracker == 2)
                {
                    //laser grid
                    FloatAbovePlayer(target, 0, 0, NPC, 10, 20, 0, target.Center + new Vector2(600, 0));

                    if (attackTimer % 80 == 0)
                    {
                        ShootProjectile(NPC, target, 7, ProjectileID.Fireball, true, 3, 22.5f, 22.5f, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer % 100 == 0)
                    {
                        xOffset = Main.rand.Next(-2100, -1400);
                        yOffset = Main.rand.Next(700, 1400);
                        anchoredPosition = target.Center;

                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 startPosition = target.Center + new Vector2(xOffset + (i * 200), -1000);
                            Dust.QuickDustLine(startPosition, startPosition + new Vector2(0, 3000), (startPosition + new Vector2(0, 3000)).Length() / 400, Color.Purple);
                            telegraphTimer = 60;
                        }

                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 startPosition = target.Center + new Vector2(1400, yOffset - (i * 200));
                            Dust.QuickDustLine(startPosition, startPosition + new Vector2(-3000, 0), (startPosition + new Vector2(0, 3000)).Length() / 400, Color.Purple);
                        }
                    }

                    if (telegraphTimer == 0)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 startPosition = anchoredPosition + new Vector2(xOffset + i * 200, -900);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), startPosition, new Vector2(0, 20), ModContent.ProjectileType<ChaosDemonBolt>(), NPC.damage / 6, 1);
                        }

                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 startPosition = target.Center + new Vector2(1400, yOffset - (i * 200));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), startPosition, new Vector2(-20, 0), ModContent.ProjectileType<ChaosDemonBolt>(), NPC.damage / 6, 1);
                        }
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker++;
                    }
                }
                else if (attackTracker == 3)
                {
                    //circle attack
                    angle += 4f;
                    FloatAbovePlayer(target, 0, 0, NPC, 50, 5, 0, target.Center + new Vector2(1, 1).RotatedBy(MathHelper.ToRadians(angle)) * 700);

                    if (attackTimer % 15 == 0)
                    {
                        ShootProjectile(NPC, target, 4, ProjectileID.DemonSickle, false, 1, 0, 0, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer % 120 == 0)
                    {
                        ShootProjectile(NPC, target, 7, ProjectileID.Fireball, false, 20, Main.rand.Next(-360, 361), 20, NPC.Center - new Vector2(0, 40), 0);
                    }

                    if (attackTimer == 0)
                    {
                        attackTimer = 600;
                        attackTracker = 1;
                    }
                }
            }
        }

        public override void OnKill() //special death animation
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)NPC.position.X, (int)NPC.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
            }

            if (tsorcRevampWorld.RemixMap && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Chaos.DarknessLifted"), new Color(255, 225, 20));
            }
        }
    }
}
