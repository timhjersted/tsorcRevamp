using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class TheMachine : BossBase
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
            NPCID.Sets.MustAlwaysDraw[NPC.type] = true;
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
            despawnHandler = new NPCDespawnHandler(LanguageUtils.GetTextValue("NPCs.TheMachine.DespawnHandler"), Color.DarkGray, DustID.Torch);
            NPC.friendly = false;

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
            NPC.friendly = false;
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
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500, 80);
                    }
                }
                if(fireChargeTimer >= 120)
                {
                    phase2Rotation -= 0.007f;
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
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    BeamNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeBeam>(), ai1: NPC.whoAmI);
                    IonNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeIon>(), ai1: NPC.whoAmI);
                    BuzzsawNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeBuzzsaw>(), ai1: NPC.whoAmI);
                    GatlingNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeGatling>(), ai1: NPC.whoAmI);
                    LauncherNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeSiege>(), ai1: NPC.whoAmI);
                    SeverNPC = NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<PrimeWelder>(), ai1: NPC.whoAmI);
                    NPC.netUpdate = true;
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            if (introTimer == introDuration - 30)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 1100, 60);
                }
                SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
            }
        }

        public bool AllPartsValid()
        {
            if(BeamNPC != null && BeamNPC.active && IonNPC != null && IonNPC.active && BuzzsawNPC != null && BuzzsawNPC.active && GatlingNPC != null && GatlingNPC.active && LauncherNPC != null && LauncherNPC.active && SeverNPC != null && SeverNPC.active &&
                BeamNPC.type == ModContent.NPCType<PrimeBeam>() && IonNPC.type == ModContent.NPCType<PrimeIon>() && BuzzsawNPC.type == ModContent.NPCType<PrimeBuzzsaw>() && 
                GatlingNPC.type == ModContent.NPCType<PrimeGatling>() && LauncherNPC.type == ModContent.NPCType<PrimeSiege>() && SeverNPC.type == ModContent.NPCType<PrimeWelder>())
            {
                return true;
            }

            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(BeamNPC.whoAmI);
            writer.Write(IonNPC.whoAmI);
            writer.Write(BuzzsawNPC.whoAmI);
            writer.Write(GatlingNPC.whoAmI);
            writer.Write(LauncherNPC.whoAmI);
            writer.Write(SeverNPC.whoAmI);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            BeamNPC = Main.npc[reader.ReadInt32()];
            IonNPC = Main.npc[reader.ReadInt32()];
            BuzzsawNPC = Main.npc[reader.ReadInt32()];
            GatlingNPC = Main.npc[reader.ReadInt32()];
            LauncherNPC = Main.npc[reader.ReadInt32()];
            SeverNPC = Main.npc[reader.ReadInt32()];
        }

        bool finalStand;
        public override void HandleLife()
        {
            if (inIntro)
            {
                return;
            }

            if(AllPartsValid() && !finalStand)
            {
                NPC.life = 35000 + BeamNPC.life + IonNPC.life + BuzzsawNPC.life + GatlingNPC.life + LauncherNPC.life + SeverNPC.life;
                NPC.lifeMax = 35000 + BeamNPC.lifeMax + IonNPC.lifeMax + BuzzsawNPC.lifeMax + GatlingNPC.lifeMax + LauncherNPC.lifeMax + SeverNPC.lifeMax;
            }
            
            if (Phase == 0 && NPC.life < NPC.lifeMax / 1.8f)
            {
                NextPhase();                
            }

            if(NPC.life <= 35006 && !finalStand)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 0, 80);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 50, 0, Main.myPlayer, 1);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 50, 0, Main.myPlayer, 2);
                }
                finalStand = true;
                NPC.defense = 0;
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

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return base.CanBeHitByItem(player, item);
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return base.CanBeHitByProjectile(projectile);
        }

        public static void PrimeProjectileBalancing(ref Projectile projectile)
        {
            if (projectile.type == ProjectileID.HallowStar)
            {
               projectile.damage = (int)(projectile.damage * 0.7f);
            }
            if (projectile.type == ProjectileID.RainbowRodBullet)
            {
                projectile.damage = (int)(projectile.damage * 0.9f);
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

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (deathAnimationProgress % 7 == 0)
                {
                    Vector2 randomPoint = Main.rand.NextVector2Circular(20, 20);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));                    
                }

                if (deathAnimationProgress % 12 == 0)
                {
                    Vector2 randomPoint = Main.rand.NextVector2Circular(20, 20);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), BeamNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), BeamNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), IonNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), IonNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), BuzzsawNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), BuzzsawNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), GatlingNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), GatlingNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), LauncherNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), LauncherNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), SeverNPC.Center + randomPoint * 3, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400, 40);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), SeverNPC.Center + randomPoint, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Main.DiscoColor));
                }
            }

            if (deathAnimationProgress == deathAnimationDuration - 1)
            {
                NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);
                NPC.downedMechBoss3 = true;
                NPC.downedMechBossAny = true;
                UsefulFunctions.SimpleGore(NPC, "TheMachine_Destroyed_1");
                UsefulFunctions.SimpleGore(NPC, "TheMachine_Destroyed_2");
                UsefulFunctions.SimpleGore(NPC, "TheMachine_Destroyed_3");

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    //Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 0, 0, Main.myPlayer, 4, UsefulFunctions.ColorToFloat(Color.White));

                    //Having to write this was punishment for my hubris and lack of foresight (not just putting these in an array I can loop through)
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
            }

            

            //The base class handles actually killing the NPC when the timer runs out
            base.HandleDeath();
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.5f;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if(projectile.DamageType == DamageClass.Melee)
            {
                modifiers.FinalDamage *= 1.5f;
            }
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
            float percent = (float)Math.Pow(((float)phaseTransitionDuration - (float)phaseTransitionTimeRemaining) / ((float)phaseTransitionDuration - 30f), 4f);
            if (percent > 1)
            {
                percent = 1;
            }
            NPC.Center = Vector2.Lerp(PrimeCeilingPoint, PrimeCenterPoint, percent);

            if (phaseTransitionDuration - phaseTransitionTimeRemaining == phaseTransitionDuration - 30f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2Circular(5, 5), ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 1100, 60);
                }
                SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
            }


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
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Marilith.SyntheticFirestorm>(), 50, 0, Main.myPlayer, 0);
                }
            }
        }

        public static Texture2D texture;
        public static Texture2D phase2Texture;
        float phase2Warmup;
        float phase2Rotation;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawMachineAura(Color.Gray * 0.8f, true, NPC);

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/TheMachine");
            UsefulFunctions.EnsureLoaded(ref phase2Texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/TheMachine_Phase2");


            int phaseFrame = 0;
            if(phaseTransitionDuration - phaseTransitionTimeRemaining < 30)
            {
                phaseFrame = (int)(5 * ((phaseTransitionDuration - phaseTransitionTimeRemaining) / 30f));
            }

            if(Phase != 0)
            {
                phaseFrame = 6;
            }

            Rectangle sourceRectangle = new Rectangle(0, phaseFrame * (texture.Height / 7), texture.Width, texture.Height / 7);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, 0, drawOrigin, 1.2f, SpriteEffects.None, 0);

            if (Phase != 0)
            {
                phase2Warmup += 1 / 60f;
                if(phase2Warmup > 1)
                {
                    phase2Warmup = 1;
                }
                Rectangle phase2Rectangle = new Rectangle(0, 0, phase2Texture.Width, phase2Texture.Height / 2);
                Vector2 phase2Origin = new Vector2(phase2Rectangle.Width * 0.5f, phase2Rectangle.Height * 0.5f);
                Main.EntitySpriteDraw(phase2Texture, NPC.Center - Main.screenPosition, phase2Rectangle, Color.White, phase2Rotation, phase2Origin, 1f, SpriteEffects.None, 0);
                phase2Rectangle.Y += phase2Texture.Height / 2;
                Main.EntitySpriteDraw(phase2Texture, NPC.Center - Main.screenPosition, phase2Rectangle, Color.White * phase2Warmup, phase2Rotation, phase2Origin, 1.2f, SpriteEffects.None, 0);
            }


            DrawEnergyShield();

            if (introTimer >= introDuration)
            {
                LightPrimeArena();
            }
            return false;
        }

        public static Effect effect;
        public static void DrawMachineAura(Color rgbColor, bool active, NPC npc, float auraBonus = 0)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            float effectIntensity = 0;
            int? machineIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<TheMachine>());
            if(machineIndex != null && active)
            {
                effectIntensity = 1;
                int effectTimer = ((TheMachine)(Main.npc[machineIndex.Value].ModNPC)).MoveTimer;
                if(effectTimer > 540)
                {
                    effectIntensity = 1f - ((effectTimer - 540f) / 60f);
                }

                if (effectTimer < 20)
                {
                    effectIntensity = effectTimer  / 20f;
                }
            }

            float timeFactor = -1;
            float scaleFactor = 4;
            if(npc.type == ModContent.NPCType<TheMachine>())
            {
                timeFactor = 1;
                scaleFactor = 3;
                effectIntensity = 1.0f;
                if (npc.life <= 35006){
                    rgbColor = Color.OrangeRed;
                }
                auraBonus = -.2f;
            }

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            float colorIntensity = 0.25f;
            if (active)
            {
                colorIntensity = .25f + (.75f * effectIntensity) + auraBonus;
                rgbColor = Color.Lerp(Color.Gray, rgbColor, effectIntensity);
            }
            else
            {
                rgbColor = Color.Gray;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, 300 + (int)(70 * (effectIntensity + auraBonus)), 300 + (int)(70 * (effectIntensity + auraBonus)));
            Vector2 origin = sourceRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseMarble.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4() * colorIntensity * 0.3f);
            effect.Parameters["ringProgress"].SetValue(0.4f + .25f * effectIntensity + auraBonus);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            effect.Parameters["scaleFactor"].SetValue(scaleFactor);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseMarble, npc.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, npc.scale, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public static Effect shieldEffect;
        void DrawEnergyShield()
        {
            return;
            if (shieldEffect == null)
            {
                shieldEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SimpleRing", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            
            
            
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheMachineBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedFocusingLens>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedIncinerator>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedMachiningTools>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DamagedDronePart>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Lore.CrestOfSteel>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.HallowedBar, 1, 25, 40));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofFright, 1, 20, 40));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.SkeletronPrimeMask, 7));
            npcLoot.Add(notExpertCondition);
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
    }
}