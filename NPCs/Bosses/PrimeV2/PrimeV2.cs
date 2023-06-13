using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class PrimeV2 : BossBase
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }

        public override void SetDefaults()
        {
            //Calling base.SetDefaults takes care of a lot of variables that all bosses share
            //Things like making them hostile, marking them as a boss, etc
            base.SetDefaults();

            //The rest are unique to this specific boss, and we have to set here:
            NPC.width = 100;
            NPC.height = 100;
            NPC.damage = 50;
            NPC.defense = 999999;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 15000;
            NPC.timeLeft = 22500;
            NPC.value = 600000;
            despawnHandler = new NPCDespawnHandler("The Machine's temple falls silent once more...", Color.DarkGray, DustID.Torch);

            //You can also specify BossBase specific values here
            introDuration = 120;
            attackTransitionDuration = 60;
            phaseTransitionDuration = 120;
            deathAnimationDuration = 180;
        }

        public static int torchID;

        /// <summary>
        /// Add all the moves and damage numbers for your boss in here!
        /// You need the function name, the time the attack lasts, and optionally can specify a color (for use with things like VFX or lighting)
        /// </summary>
        public override void InitializeMovesAndDamage()
        {
            //Create a new function for every move of your boss, and then add them to this list alongside the duration of th attack
            MoveList = new List<BossMove> {
                new BossMove(Beam, 600),
                new BossMove(Ion, 600),
                new BossMove(Buzzsaw, 600),
                new BossMove(Gatling, 600),
                new BossMove(Launcher, 600),
                new BossMove(Welder, 600),
                //new BossMove(Flamethrower, 600),
                };


            //Set the damage numbers for every attack or projectile your boss uses here
            //Remember: Contact damage is doubled, and projectile damage is multiplied by 4!
            DamageNumbers = new Dictionary<string, int>
            {
                ["Flamethrower"] = 65,
            };
        }

        public static Vector2 PrimeCeilingPoint = new Vector2(81048, 16224);
        public static Vector2 PrimeCenterPoint = new Vector2(81048, 17664);

        int fireChargeTimer = 0;
        bool activated;
        public override void AI()
        {
            if (despawning)
            {
                if (Main.tile[5152, 1106].TileType != TileID.Glass)
                {
                    ActuateBottomHalf();
                }
            }
            PrimeCeilingPoint = new Vector2(81048, 16424);
            if (!activated)
            {
                NPC.Center = PrimeCeilingPoint + new Vector2(0, -200);
                for(int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && NPC.Distance(Main.player[i].Center) < 550)
                    {
                        activated = true;
                        break;
                    }
                }

                if (!activated)
                {
                    return;
                }
            }
            base.AI();            

            if (!aiPaused)
            {

                if (Phase == 0)
                {
                    NPC.Center = PrimeCeilingPoint;
                }
                else
                {
                    NPC.Center = PrimeCenterPoint;
                }                
            }

            if(Phase == 1)
            {
                fireChargeTimer++;
                //TODO: Fire chargeup VFX
                if(fireChargeTimer == 180)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500, 80);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 50, 0, Main.myPlayer, 1100, 80);
                    }
                }
            }
        }


        public static void LightPrimeArena()
        {
            for (int x = 5010; x < 5133; x += 32)
            {
                for (int y = 988; y < 1140; y += 32)
                {
                    if (!UsefulFunctions.IsTileReallySolid(x, y))
                    {
                        if (y % 64 < 32)
                        {
                            Lighting.AddLight(x, y + (int)((Main.timeForVisualEffects / 5f) % 64), torchID, 1f);
                        }
                        else
                        {
                            Lighting.AddLight(x + 16, y + (int)((Main.timeForVisualEffects / 5f) % 64), torchID, 1f);
                        }
                    }
                }
            }
        }

        public void Beam()
        {
            torchID = TorchID.Crimson;
            //Sweeps lasers across the player
        }

        //A second example attack, for demonstration purposes
        public void Ion()
        {
            torchID = TorchID.Ice;
            //Spawn ion bombs once every 2 seconds, with a 1 second fuse
            //Each bomb creates an explosion like mariliths waves, 6 lightning, and 6 projectiles rotated offset from the lightning

            //In phase 2 it spits out 3 and teleports them all at once, but with smaller explosions and no projectiles
        }

        public void Buzzsaw()
        {
            torchID = TorchID.Ichor;
            //Fires the buzzsaw, causing it to bounce around the arena
        }

        public void Gatling()
        {
            torchID = TorchID.Cursed;
            //Laser gets overcharged, firing rapidly
        }
        public void Launcher()
        {
            torchID = TorchID.Orange;
            //Fires homing rockets that must be shot down
        }

        public void Welder()
        {
            torchID = TorchID.Bone;
            //Slashes
        }

        NPC BeamNPC;
        NPC IonNPC;
        NPC BuzzsawNPC;
        NPC GatlingNPC;
        NPC LauncherNPC;
        NPC SeverNPC;

        /// <summary>
        /// Controls what this boss does during its intro
        /// </summary>
        float progress = 0;
        public override void HandleIntro()
        {
            float percent = (float)Math.Pow((float)introTimer / ((float)introDuration - 30), 4f);
            if(percent > 1)
            {
                percent = 1;
            }
            NPC.Center = PrimeCeilingPoint + Vector2.Lerp(new Vector2(0, -200), Vector2.Zero, percent);

            UsefulFunctions.SetAllCameras(NPC.Center, ref progress);

            

            if (introTimer == 0)
            {
                BeamNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeBeam>(), ai1: NPC.whoAmI);
                IonNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeIon>(), ai1: NPC.whoAmI);
                BuzzsawNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeBuzzsaw>(), ai1: NPC.whoAmI);
                GatlingNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeGatling>(), ai1: NPC.whoAmI);
                LauncherNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeSiege>(), ai1: NPC.whoAmI);
                SeverNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeWelder>(), ai1: NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            if (introTimer == introDuration - 30)
            {

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Spawn a shockwave
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 1100, 60);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
            }
        }       
        
        public override void HandleLife()
        {
            if (inIntro)
            {
                return;
            }

            int totalLife = BeamNPC.life + IonNPC.life + BuzzsawNPC.life + GatlingNPC.life + LauncherNPC.life + SeverNPC.life;
            NPC.life = totalLife;
            if (Phase == 0)
            {
                int totalLifeMax = BeamNPC.lifeMax + IonNPC.lifeMax + BuzzsawNPC.lifeMax + GatlingNPC.lifeMax + LauncherNPC.lifeMax + SeverNPC.lifeMax;
                NPC.lifeMax = totalLifeMax;

                if (totalLife < totalLifeMax / 2)
                {
                    NextPhase();
                }
            }

            if(totalLife <= 6)
            {
                NPC.life = 1;
            }
        }

        public override void BossHeadSlot(ref int index)
        {
            if (!activated)
            {
                index = -1;
            }
        }

        public static int PrimeArmHealth = 15000;
        public static void PrimeDamageShare(int thisNPC, int damage)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && i != thisNPC)
                {
                    int type = Main.npc[i].type;
                    if (Main.npc[i].life != 1 && (
                        type == ModContent.NPCType<PrimeBeam>() ||
                        type == ModContent.NPCType<PrimeBuzzsaw>() ||
                        type == ModContent.NPCType<PrimeGatling>() ||
                        type == ModContent.NPCType<PrimeIon>() ||
                        type == ModContent.NPCType<PrimeSiege>() ||
                        type == ModContent.NPCType<PrimeWelder>()))
                    {
                        Main.npc[i].life -= damage / 4;
                        CombatText.NewText(Main.npc[i].Hitbox, CombatText.DamagedHostile, damage / 4);

                        if (Main.npc[i].life < 1)
                        {
                            Main.npc[i].life = 1;
                        }
                    }
                }
            }
        }

        public static void PrimeProjectileBalancing(ref Projectile projectile)
        {
            if (projectile.type == ProjectileID.RainbowRodBullet || projectile.type == ProjectileID.HallowStar)
            {
                projectile.damage /= 2;
            }
        }

        public static void ActuatePrimeArena()
        {
            //Turn the top row of glass into platforms
            for (int x = 4991; x < 5153; x++)
            {
                if (Main.tile[x, 1100].TileType == TileID.Glass || Main.tile[x, 1100].TileType == 0)
                {
                    if (x < 5050 || x > 5078)
                    {
                        Main.tile[x, 1100].ResetToType(TileID.Platforms);
                        Main.tile[x, 1100].TileFrameX = 0;
                        Main.tile[x, 1100].TileFrameY = 522;
                    }
                }
                else if (Main.tile[x, 1100].TileType == TileID.Platforms)
                {
                    if (x < 5050 || x > 5078)
                    {
                        Main.tile[x, 1100].ResetToType(TileID.Glass);
                    }
                }

                if (Main.tile[x, 1106].TileType == TileID.Glass)
                {
                    if (Main.tile[x, 1106].IsActuated)
                    {
                        Wiring.ActuateForced(x, 1106);
                    }
                }
            }


            //Actuate the center
            for (int x = 5050; x < 5079; x++)
            {
                for (int y = 1085; y < 1145; y++)
                {
                    if (Main.tile[x, y].TileType != TileID.Platforms)
                    {
                        if (y != 1106)
                        {
                            Wiring.ActuateForced(x, y);
                        }
                    }
                }
            }

            //Turn the random obstructive tin bricks into platforms and delete the chains
            for (int x = 4983; x < 5159; x++)
            {
                for (int y = 1000; y < 1156; y++)
                {
                    if(Main.tile[x, y].TileType == TileID.Torches)
                    {
                        Main.tile[x, y].ClearTile();
                    }
                    if (Main.tile[x, y].TileType == TileID.TinBrick)
                    {
                        if (Main.tile[x - 1, y].TileType == TileID.Platforms)
                        {
                            Main.tile[x, y].ResetToType(TileID.Platforms);
                            Main.tile[x, y].TileFrameX = 0;
                            Main.tile[x, y].TileFrameY = 522;
                            WorldGen.Reframe(x, y);
                        }
                        else if (Main.tile[x + 1, y].TileType == TileID.Platforms)
                        {
                            Main.tile[x, y].ResetToType(TileID.Platforms);
                            Main.tile[x, y].TileFrameX = 0;
                            Main.tile[x, y].TileFrameY = 522;
                            WorldGen.Reframe(x, y);
                        }
                    }
                    else
                    {
                        if (Main.tile[x, y].TileType == TileID.Chain)
                        {
                            Main.tile[x, y].ClearTile();
                        }
                        if (Main.tile[x, y].TileType == TileID.Platforms)
                        {
                            Main.tile[x, y].TileFrameX = 0;
                            Main.tile[x, y].TileFrameY = 522;
                            WorldGen.Reframe(x, y);
                        }
                    }


                    WorldGen.Reframe(x, y);
                }
            }
        }

        public static void ActuateBottomHalf()
        {
            for (int x = 4991; x < 5153; x++)
            {
                if (Main.tile[x, 1105].TileType != TileID.DemonAltar)
                {
                    Wiring.ActuateForced(x, 1106);
                }

                for(int y = 1106; y < 1180; y++)
                {
                    Main.tile[x, y].LiquidAmount = 0;
                }
            }
        }

        /// <summary>
        /// Override this to make things happen when this boss is killed
        /// In this case, it spawns a bunch of shockwaves with 20 frames between them
        /// </summary>
        public override void HandleDeath()
        {
            UsefulFunctions.SetAllCameras(NPC.Center, ref progress);

            if (deathAnimationProgress % 20 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(100, 100), Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 1100, 40);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + Main.rand.NextVector2Circular(100, 100), Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 0, 0, Main.myPlayer, 1100, 40);
                }
            }

            //Having to write this was punishment for my hubris and lack of foresight (not just putting these in an array I can loop through)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (BeamNPC != null && BeamNPC.active)
                {
                    BeamNPC.StrikeInstantKill();
                    BeamNPC.netUpdate = true;
                }

                if (IonNPC != null && IonNPC.active)
                {
                    IonNPC.StrikeInstantKill();
                    IonNPC.netUpdate = true;
                }

                if (BuzzsawNPC != null && BuzzsawNPC.active)
                {
                    BuzzsawNPC.StrikeInstantKill();
                    BuzzsawNPC.netUpdate = true;
                }

                if (GatlingNPC != null && GatlingNPC.active)
                {
                    GatlingNPC.StrikeInstantKill();
                    GatlingNPC.netUpdate = true;
                }

                if (LauncherNPC != null && LauncherNPC.active)
                {
                    LauncherNPC.StrikeInstantKill();
                    LauncherNPC.netUpdate = true;
                }

                if (SeverNPC != null && SeverNPC.active)
                {
                    SeverNPC.StrikeInstantKill();
                    SeverNPC.netUpdate = true;
                }
            }

            //The base class handles actually killing the NPC when the timer runs out
            base.HandleDeath();
        }

        public override void AttackTransition()
        {
            //TODO: Maybe make its aura flash once I add that??
        }

        /// <summary>
        /// Override this to make the boss do things while transitioning between phases
        /// In this example, it spawns some dust and spawns a shockwave every time it's another third of the way through the phase transition
        /// </summary>
        public override void PhaseTransition()
        {
            NPC.Center = Vector2.Lerp(PrimeCeilingPoint, PrimeCenterPoint, (float)Math.Pow((float)(phaseTransitionDuration - phaseTransitionTimeRemaining) / phaseTransitionDuration, 4f));
            UsefulFunctions.SetAllCameras(NPC.Center, ref progress);

            if (phaseTransitionTimeRemaining  == 1)
            {
                if (!Main.tile[5152, 1106].IsActuated)
                {
                    ActuateBottomHalf();
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 1100, 80);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 50, 0, Main.myPlayer, 1100, 80);
                }
            }
        }

        /// <summary>
        /// Override this to add custom VFX to your boss
        /// </summary>
        public static Texture2D texture;
        public static Texture2D eyeTexture;
        public static Texture2D boneTexture;
        int frameIndex;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //Scale it up a little

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeV2");
            UsefulFunctions.EnsureLoaded(ref eyeTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/Bone_Eyes");
            UsefulFunctions.EnsureLoaded(ref boneTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/Arm_Bone_2");

            if (inIntro)
            {
                frameIndex = 1;
            }

            if (!aiPaused)
            {
                NPC.frameCounter++;
                if (NPC.frameCounter > 20)
                {
                    NPC.frameCounter = 0;
                    frameIndex++;
                    if (frameIndex >= 2)
                    {
                        frameIndex = 0;
                    }
                }
            }

            Rectangle sourceRectangle = new Rectangle(0, frameIndex * (texture.Height / 6), texture.Width, texture.Height / 6);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, 0, drawOrigin, 1f, SpriteEffects.None, 0);

            Rectangle eyeRectangle = new Rectangle(0, 0, eyeTexture.Width, eyeTexture.Height / 3);
            Vector2 eyeOrigin = new Vector2(eyeRectangle.Width * 0.5f, eyeRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(eyeTexture, NPC.Center - Main.screenPosition - new Vector2(-2, 1), eyeRectangle, Color.White, 0, eyeOrigin, 1f, SpriteEffects.None, 0);



            //Glowmask for the eyes
            if (introTimer >= introDuration)
            {
                LightPrimeArena();
            }
            return false;
        }
                

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
    }
}