using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using System;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class OmnirsDemonLordApocalypse : ModNPC
    {
        int attackState = 0; // 0: Idle/Chase, 1: Fire Breath, 2: Armageddon, 3: Great Fireball, 4: Lightning Storm, 5: Sudden Death, 6: Toxic Gas Nova
        int attackTimer = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 13;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 65;
            NPC.height = 104;
            NPC.damage = 100;
            NPC.defense = 60;
            NPC.lifeMax = 40000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 100000f;
            NPC.npcSlots = 100;
            NPC.knockBackResist = 0.05f;
            NPC.scale = 1.3f;

            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            // Phase 2: SmartFighter4AI movement. minSurfaceWidth (in AI) keeps this big demon off narrow ledges;
            // NavSearchRadius enables A*. Already slow + teleports, so no speed/jump tuning.
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 16;
            // On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
            EvasiveProfile.HeavyBeast(g);
            // Phase 1 (beast positioner): never stand still — oscillate in a large band when it can't path; wander
            // off if it can't reach you AND you stop hitting it for ~10s. Tune the band to taste.
            g.KiteRangeMin = 12f;
            g.KiteRangeMax = 30f;
            g.PatrolMode = NPCs.PatrolMode.Wander;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            if (attackState == 0)
            {
                // Walk slowly (60% slower of 1.2f is 0.48f), jump modestly, teleport closer if player is too far
                tsorcRevampAIs.FighterAI(NPC, topSpeed: 0.48f, acceleration: 0.02f, canTeleport: true, lavaJumping: true, canDodgeroll: false, canPounce: false, minSurfaceWidth: 3, canWalkBackwards: true); // support core (center 3 tiles); sprite edges clip/sink (Phase 3)

                // Increment attack cooldown timer
                NPC.localAI[1]++;
                if (NPC.localAI[1] >= 240f) // every 4 seconds
                {
                    NPC.localAI[1] = 0f;
                    attackState = Main.rand.Next(1, 7); // choose an attack (1 to 6)
                    attackTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                // Stop horizontal movement during telegraph/firing phase
                NPC.velocity.X *= 0.8f;
                if (Math.Abs(NPC.velocity.X) < 0.1f) NPC.velocity.X = 0f;

                // Slow air descent
                if (NPC.velocity.Y > 0f) NPC.velocity.Y *= 0.95f;

                attackTimer++;

                Vector2 mouthPos = new Vector2(NPC.Center.X + (10 * NPC.direction), NPC.Center.Y - 25f);

                // TELEGRAPH STAGE
                if (attackTimer < 45)
                {
                    // Charging sound
                    if (attackTimer == 1)
                    {
                        if (attackState == 1) // Fire breath growl
                        {
                            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/low-dragon-growl") with { Volume = 0.5f }, NPC.Center);
                        }
                        else if (attackState == 4) // Lightning charging
                        {
                            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = -0.5f }, NPC.Center);
                        }
                        else
                        {
                            SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f }, NPC.Center);
                        }
                    }

                    // Visual particles at mouth
                    if (attackState == 1 || attackState == 3) // Fire
                    {
                        UsefulFunctions.DustRing(mouthPos, (int)(32 * ((45 - attackTimer) / 45.0)), DustID.Torch, 8, 2f);
                    }
                    else if (attackState == 2) // Armageddon (meteors rising at player)
                    {
                        int dust = Dust.NewDust(player.position - new Vector2(100, 0), 200, player.height, DustID.Torch, 0f, -4f, 100, default, 1.5f);
                        Main.dust[dust].noGravity = true;
                    }
                    else if (attackState == 4) // Lightning (electric sparkles)
                    {
                        UsefulFunctions.DustRing(mouthPos, (int)(32 * ((45 - attackTimer) / 45.0)), DustID.Vortex, 8, 2f);
                    }
                    else if (attackState == 5) // Sudden Death (spooky purple)
                    {
                        UsefulFunctions.DustRing(mouthPos, (int)(32 * ((45 - attackTimer) / 45.0)), DustID.GemRuby, 8, 2f);
                    }
                    else if (attackState == 6) // Toxic (green gas)
                    {
                        UsefulFunctions.DustRing(mouthPos, (int)(32 * ((45 - attackTimer) / 45.0)), DustID.CorruptPlants, 8, 2f);
                    }
                }
                // FIRING STAGE
                else
                {
                    if (attackState == 1) // Fire Breath (channeled)
                    {
                        if (attackTimer < 105)
                        {
                            if (attackTimer % 3 == 0)
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Vector2 breathVel = UsefulFunctions.Aim(mouthPos, player.Center, 8f);
                                    breathVel += Main.rand.NextVector2Circular(-1.2f, 1.2f);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), mouthPos.X, mouthPos.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), 25, 0f, Main.myPlayer);
                                }
                                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center);
                            }
                        }
                        else
                        {
                            attackState = 0;
                            NPC.localAI[1] = 0f;
                        }
                    }
                    else if (attackTimer == 45) // Instant spell cast
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (attackState == 2) // Armageddon Blast (Meteors from above)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    float offsetX = Main.rand.Next(-180, 181);
                                    Vector2 spawnPos = new Vector2(player.Center.X + offsetX, player.Center.Y - 600f);
                                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 9f);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos.X, spawnPos.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>(), 45, 0f, Main.myPlayer);
                                }
                                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f }, NPC.Center);
                            }
                            else if (attackState == 3) // Great Fireball
                            {
                                Vector2 velocity = UsefulFunctions.Aim(mouthPos, player.Center, 5.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouthPos.X, mouthPos.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), 55, 0f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
                            }
                            else if (attackState == 4) // Lightning Storm
                            {
                                Vector2 velocity = UsefulFunctions.Aim(mouthPos, player.Center, 11f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouthPos.X, mouthPos.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>(), 60, 0f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f }, NPC.Center);
                            }
                            else if (attackState == 5) // Sudden Death Ball
                            {
                                Vector2 velocity = UsefulFunctions.Aim(mouthPos, player.Center, 7.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouthPos.X, mouthPos.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), 75, 0f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.NPCDeath9 with { Volume = 0.8f }, NPC.Center);
                            }
                            else if (attackState == 6) // Toxic Gas Nova (Poison cloud)
                            {
                                Vector2 velocity = UsefulFunctions.Aim(mouthPos, player.Center, 7f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), mouthPos.X, mouthPos.Y, velocity.X, velocity.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 40, 0f, Main.myPlayer);
                                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.8f }, NPC.Center);
                            }
                        }

                        // Complete non-breath spell instant launch
                        attackState = 0;
                        NPC.localAI[1] = 0f;
                    }
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = NPC.direction;

            if (NPC.velocity.Y != 0f)
            {
                NPC.frame.Y = 0 * frameHeight; // Jump frame is Frame 0
                NPC.frameCounter = 0.0;
            }
            else if (NPC.velocity.X == 0f)
            {
                NPC.frame.Y = 1 * frameHeight; // Idle frame is Frame 1
                NPC.frameCounter = 0.0;
            }
            else
            {
                NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 1.5f);
                NPC.frameCounter += 1.0;
                if (NPC.frameCounter > 6.0)
                {
                    NPC.frame.Y = NPC.frame.Y + frameHeight;
                    NPC.frameCounter = 0.0;
                }
                if (NPC.frame.Y / frameHeight < 2 || NPC.frame.Y / frameHeight >= 13)
                {
                    NPC.frame.Y = frameHeight * 2;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            
            int singleFrameHeight = texture.Height / Main.npcFrameCount[NPC.type];
            Vector2 origin = new Vector2(texture.Width / 2, singleFrameHeight / 2);
            
            // By default, Terraria aligns the bottom of the sprite with the bottom of the hitbox.
            // Since NPC.height = 104 and frame height = 96, default center is Center.Y + 4.
            // To shift it 2 pixels lower than default, we use an offset of +6f.
            Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY + 6f);
            
            spriteBatch.Draw(texture, drawPos, NPC.frame, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);
            return false; // Skip default drawing
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsDemonLordGore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsDemonLordGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsDemonLordGore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsDemonLordGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsDemonLordGore3").Type, 1f);
            }
            // Drops commented out per user instruction
        }
    }
}
