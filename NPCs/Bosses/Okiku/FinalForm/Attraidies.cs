using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.ModLoader.Config;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FinalForm
{
    [AutoloadBossHead]
    class Attraidies : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 10;
            NPC.damage = 0;
            NPC.defense = 25;
            NPC.height = 100;
            NPC.width = 100;
            NPC.timeLeft = 22500;
            Music = 12;
            NPC.lifeMax = 250000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.noGravity = true;
            NPC.scale = 1.2f;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            Main.npcFrameCount[NPC.type] = 3;
            despawnHandler = new NPCDespawnHandler("With your death a dark shadow falls over the world...", Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public int ShadowOrbDamage = 35;
        public int PoisonTrailDamage = 40;
        public int BlackFireDamage = 35;
        public int StardustLaserDamage = 45;
        public int NebulaShotDamage = 35;
        public int SolarDetonationDamage = 45;
        public int LightningStrikeDamage = 35;
        public int DarkLaserDamage = 45;


        public int CurrentAttackMode
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float AttackTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public int Phase
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        List<Action> CurrentMoveList
        {
            get
            {
                switch (Phase)
                {
                    case 0:
                        return Phase0MoveList;
                    case 1:
                        return Phase1MoveList;
                    default:
                        return Phase2MoveList;
                }
            }
        }

        Action CurrentMove
        {
            get
            {
                if(Phase == 3)
                {
                    return FinalStand;
                }
                else
                {
                    return CurrentMoveList[CurrentAttackMode];
                }
            }
        }

        Player Target
        {
            get
            {
                return Main.player[NPC.target];
            }
        }

        List<Action> Phase0MoveList;
        List<Action> Phase1MoveList;
        List<Action> Phase2MoveList;
        int AuraState = 0;

        bool initialized = false;

        int introTimer = 0;
        int attackSwitchDelay = 0;
        int deathTimer;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            //if (NPC.life > 14000)
            {
                //NPC.life = 14000;
            }
            if (!initialized)
            {
                UsefulFunctions.BroadcastText("I am impressed you've made it this far, Red. But I'm done playing games. It's time to end this...", 175, 75, 255);
                InitializeMoves();
                if (!Main.tile[1365,280].IsActuated)
                {
                    ActuateAttraidiesArena();
                }
                initialized = true;
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (introTimer < 60)
            {
                Intro();
            }

            ManageLife();
            HandleAura();
            SpawnDusts();

            if (transitionTimer == 0)
            {
                if(attackSwitchDelay > 0)
                {
                    attackSwitchDelay--;
                    NPC.velocity *= 0.95f;
                }
                else
                {
                    CurrentMove();
                    AttackTimer++;
                }
            }
        }

        float transitionTimer = 0;
        float maxTime;
        void ManageLife()
        {
            if(NPC.life > NPC.lifeMax / 2)
            {
                Main.dayTime = false;
                float life = NPC.life;
                float maxOver2 = NPC.lifeMax / 2f;

                float ratio = (life - maxOver2) / maxOver2;
                maxTime = MathHelper.Lerp((float)Main.nightLength, 1, ratio);
            }
            else
            {
                Main.dayTime = true;
                maxTime = MathHelper.Lerp((float)Main.dayLength  * 0.3f, 1, (float)NPC.life / (float)(NPC.lifeMax / 2f));
            }

            if (Main.time < maxTime)
            {
                Main.time += 5;
            }
            else
            {
                Main.time = maxTime;
            }

            if (transitionTimer > 0)
            {
                transitionTimer--;
                NPC.velocity *= 0.9f;
                if (transitionTimer == 30)
                {
                    //Advance to the next aura state
                    AuraState = Phase + 1;
                    //Spawn shockwave
                }

                if (transitionTimer == 1)
                {
                    NPC.dontTakeDamage = false;
                    Phase++;
                    AuraState = Phase;
                    AttackTimer = 0;
                    CurrentAttackMode = 0;
                    StartAura(0.2f);
                    ClearObstructiveProjectiles();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<AttraidiesFragment>())
                        {
                            Main.npc[i].active = false;
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                            }
                        }
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }

                return;
            }
            else
            {
                //Initiate transitions with various durations
                if (Phase == 0 && NPC.life < NPC.lifeMax * 0.7f)
                {
                    NPC.dontTakeDamage = true;
                    transitionTimer = 120;
                }
                if (Phase == 1 && NPC.life < NPC.lifeMax * 0.4f)
                {
                    NPC.dontTakeDamage = true;
                    transitionTimer = 60;
                }
                if (Phase == 2 && NPC.life < NPC.lifeMax * 0.20f)
                {
                    ClearObstructiveProjectiles();
                    NPC.Center = Target.Center + new Vector2(0, -600);
                    NPC.dontTakeDamage = true;
                    UsefulFunctions.BroadcastText("You feel the dark power of Attraidies flare...", Color.Purple);
                    UsefulFunctions.BroadcastText("And the power of the Earth uplift you from below, giving you the strength to finish him!", Color.Green);
                    transitionTimer = 120;
                }
                if (NPC.life == 1)
                {
                    attackSwitchDelay = 999;
                    deathTimer++;
                    HandleDeath();
                }
            }
        }

        void ClearObstructiveProjectiles()
        {
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<VortexOrb>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<SolarDetonator>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<SolarBlast>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.Enemy.Marilith.MarilithLightning>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<DarkLaser>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<DarkLaserHost>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<StardustShot>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<StardustBeam>());
            UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<NebulaShot>());
        }
        

        //TODO: Create entirely
        void Intro()
        {
            introTimer++;
            //Distortion bubble collapses above player
            if (introTimer == 1)
            {
                NPC.dontTakeDamage = true;
                NPC.Center = Target.Center + new Vector2(0, -300);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.TriadDeath>(), 0, 0, Main.myPlayer);
                }
            }
            //Four loud lightning strikes
            //A flash, fading into an explosion like the triad death but rotated 45 degrees
            //The dude appears

            NPC.dontTakeDamage = false;
        }

        void SpawnDusts()
        {
            //Spawn dusts depending on his health
            if (Phase == 1)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }

            if(Phase == 2)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Red, 2f);
                Main.dust[dust].noGravity = true;
            }
        }

        
        void ChargingPoisonTrails()
        {
            Lighting.AddLight(NPC.position, Color.DarkOrange.ToVector3());

            if(Phase == 2)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active)
                    {
                        Main.player[i].AddBuff(ModContent.BuffType<Buffs.EarthAlignment>(), 300);
                    }
                }
            }

            float chargeDelay = 60;
            if(Phase == 1)
            {
                chargeDelay = 75;
            }
            if(Phase == 2)
            {
                chargeDelay = 90;
            }

            //Charging up VFX
            if(AttackTimer < 120)
            {
                return;
            }
            if(AttackTimer % chargeDelay == 0)
            {
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 20);
                if(Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PoisonTrail>(), PoisonTrailDamage, 1, Main.myPlayer, 1, NPC.whoAmI);
                }
            }

            if(AttackTimer % chargeDelay > 45)
            {
                NPC.velocity *= 0.9f;
            }

            if (AttackTimer % chargeDelay == 45 && Phase > 0 && AttackTimer < 860)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if(Phase >= 2 && AttackTimer % (chargeDelay * 2) == 45)
                    {
                        for(int i = 0; i < 4; i++)
                        {
                            Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 4f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, 1, Main.myPlayer, NPC.target, 120);

                        }
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 10f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, -1, Main.myPlayer, -1);
                    }
                }
            }
            
            if(AttackTimer >= 860)
            {
                NPC.velocity *= 0.9f;
            }

            if (AttackTimer > 870)
            {
                NPC.damage = 0;
                NextAttack();
            }
        }

        
        void BlackFireRain()
        {
            Lighting.AddLight(NPC.position, Color.Purple.ToVector3());
            UsefulFunctions.SmoothHoming(NPC, Target.Center + new Vector2(0, -300), 0.05f, 5f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer > 15 && (Main.GameUpdateCount % 45 == 0))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        //The first projectile, which he fires into the sky in clumps and is mostly for visual effect (still does damage, though)
                        Vector2 position = NPC.position + new Vector2(Main.rand.Next(-20, 20), Main.rand.Next(-20, 20));
                        Vector2 velocity = new Vector2(Main.rand.Next(-2, 2), -50);
                        Projectile blackFire = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, velocity, ModContent.ProjectileType<EnemyBlackFireVisual>(), BlackFireDamage, .5f, Main.myPlayer);
                        blackFire.timeLeft = 20;
                    }
                }

                if (AttackTimer > 75 && (Main.GameUpdateCount % 5 == 0))
                {
                    //The second projectile, which comes raining down a second later and means business
                    Vector2 position = Main.player[NPC.target].position + new Vector2(Main.rand.Next(-1400, 1400), -700);
                    //Very similar to normal Black Fire, but phases through blocks until it reaches the player's height.
                    //Also the explosion doesn't do damage (for obvious reasons)
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, new Vector2(0, 5), ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, .5f, Main.myPlayer, NPC.target);
                }
            }

            if(AttackTimer > 600)
            {
                NextAttack();
            }
        }

        
        int clockwise;
        void DarkLasers()
        {
            Lighting.AddLight(NPC.position, Color.Purple.ToVector3());
            if (AttackTimer < 90)
            {
                UsefulFunctions.SmoothHoming(NPC, Target.Center + new Vector2(0, -500), 0.5f, 50f);
            }
            else
            {
                NPC.velocity *= 0.9f;
            }

            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.EarthAlignment>(), 1200);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer == 5)
                {
                    clockwise = 1;
                    if (Phase != 2)
                    {
                        UsefulFunctions.BroadcastText("You suddenly feel weightless...", Color.DeepSkyBlue);
                    }

                    if (Phase != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesFragment>(), ai0: NPC.whoAmI, ai1: i, ai3: Phase);
                        }
                    }

                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkLaserHost>(), DarkLaserDamage, .5f, Main.myPlayer, NPC.whoAmI);
                }

                if (Phase == 0)
                {
                    if (AttackTimer % 2 == 0 && AttackTimer > 120)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(8, 8), ModContent.ProjectileType<ObscureShot>(), DarkLaserDamage, .5f, Main.myPlayer);
                    }
                }
                if(Phase == 1)
                {
                    if (AttackTimer > 120 && AttackTimer < 1000)
                    {
                        if (AttackTimer % 140 == 0)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                //The first projectile, which he fires into the sky in clumps and is mostly for visual effect (still does damage, though)
                                Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 4f);
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, .5f, Main.myPlayer, NPC.target, 100);
                            }
                        }
                        if (AttackTimer % 120 == 0)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 16f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, Main.myPlayer, clockwise);
                            }
                            clockwise *= -1;
                        }
                    }
                }
                if (Phase == 2)
                {
                    //Performs his dark lasers attack but instead of firing obscure drops he traps the player in a vortex with lightning orbs and solar detonators
                    if (AttackTimer == 5)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 velocity = new Vector2(10, 0).RotatedBy(Math.PI / 2f * i);
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<VortexOrb>(), LightningStrikeDamage, .5f, Main.myPlayer, i, Phase);
                        }
                    }
                    if(AttackTimer % 60 == 0 && AttackTimer < 1000)
                    {
                        Vector2 position = Target.Center + Main.rand.NextVector2Square(-300, 300);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, NPC.target);
                    }
                }
            }

            if(AttackTimer > 1200)
            {
                NextAttack();
            }
        }

        //TODO: Reflect on if this actually rocks or sucks
        //Add teleport VFX
        void StardustLasers()
        {
            Lighting.AddLight(NPC.position, Color.Cyan.ToVector3());
            if (AttackTimer == 0)
            {
                NPC.Center = Target.Center + new Vector2(-500, 0).RotatedBy(-Math.PI / 3f);
            }
            if (AttackTimer == 270)
            {
                NPC.Center = Target.Center + new Vector2(-500, 0).RotatedBy(4f * Math.PI / 3f);
            }
            if (AttackTimer == 540)
            {
                NPC.Center = Target.Center + new Vector2(-500, 0).RotatedBy(Math.PI / 2f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer % 45 == 0 && AttackTimer % 270 < 180)
                {
                    int speed = 35;
                    Vector2 vagueVelocity = new Vector2(Main.rand.Next(-speed, speed), Main.rand.Next(-speed, speed));
                    for (int i = 0; i < 4; i++)
                    {
                        //The first projectile, which he fires into the sky in clumps and is mostly for visual effect (still does damage, though)
                        Vector2 position = NPC.position;
                        Vector2 velocity = vagueVelocity + new Vector2(Main.rand.Next(-5, 5), Main.rand.Next(-5, 5));
                        int firingDelay = (int)(240 - AttackTimer % 270);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, velocity, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, .5f, Main.myPlayer, NPC.target, firingDelay);
                    }
                }
            }

            if(AttackTimer >= 809)
            {
                NextAttack();
            }
        }

        //TODO: VFX
        void VortexLightning()
        {
            Lighting.AddLight(NPC.position, Color.Teal.ToVector3());

            if (AttackTimer > 180)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && Main.player[i].Center.Distance(NPC.Center) > 1000)
                    {
                        Main.player[i].velocity = UsefulFunctions.GenerateTargetingVector(Main.player[i].Center, NPC.Center, 10);
                        Main.player[i].AddBuff(BuffID.Electrified, 100);
                    }
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer == 5)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.Center = Target.Center + new Vector2(0, -300);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 velocity = new Vector2(10, 0).RotatedBy(Math.PI / 2f * i);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<VortexOrb>(), LightningStrikeDamage, .5f, Main.myPlayer, i, Phase);
                    }
                }
            }

            if (AttackTimer > 600)
            {
                NextAttack();
            }
        }

        
        float solarDetonatorHoverAngle;
        void SolarDetonators()
        {
            Lighting.AddLight(NPC.position, Color.OrangeRed.ToVector3());
            Vector2 homingTarget = Target.Center + new Vector2(150, 0).RotatedBy(solarDetonatorHoverAngle);
            UsefulFunctions.SmoothHoming(NPC, homingTarget, 0.1f, 5f, bufferZone: true);
            solarDetonatorHoverAngle += 0.01f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer % 45 == 0 && AttackTimer < 550)
                {
                    Vector2 position = Target.Center + Main.rand.NextVector2Square(-600, 600);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, NPC.target);
                }
            }

            if(AttackTimer > 700)
            {
                NextAttack();
            }
        }

        
        void NebulaShitstorm()
        {
            Lighting.AddLight(NPC.position, Color.HotPink.ToVector3());
            NPC.velocity *= 0.95f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer == 90)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesFragment>(), ai0: NPC.whoAmI, ai1: i, ai3: Phase);
                    }
                }
                if (AttackTimer % 120 == 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 4f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, Main.myPlayer, clockwise);
                    }
                }
            }

            if (AttackTimer == 900)
            {
                NextAttack();
            }
        }

        void SolarAndVortex()
        {
            if (AttackTimer < 120)
            {
                return;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer > 300)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i] != null && Main.player[i].active && Main.player[i].Center.Distance(NPC.Center) > 1000)
                        {
                            Main.player[i].velocity = UsefulFunctions.GenerateTargetingVector(Main.player[i].Center, NPC.Center, 10);
                            Main.player[i].AddBuff(BuffID.Electrified, 100);
                        }
                    }
                }
                if (AttackTimer == 120)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 velocity = new Vector2(10, 0).RotatedBy(Math.PI / 2f * i);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<VortexOrb>(), LightningStrikeDamage, .5f, Main.myPlayer, i, Phase);
                    }
                }

                if (AttackTimer % 90 == 0 && AttackTimer < 800)
                {
                    Vector2 position = Target.Center + Main.rand.NextVector2Square(-600, 600);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, NPC.target);
                }
            }
            if (AttackTimer > 900)
            {
                NextAttack();
            }
        }


        void FinalStand()
        {
            NPC.velocity = Vector2.Zero;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active)
                {
                    Main.player[i].AddBuff(ModContent.BuffType<Buffs.EarthAlignment>(), 300);
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (AttackTimer == 2)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkLaserHost>(), DarkLaserDamage, .5f, Main.myPlayer, NPC.whoAmI, 1);
                }

                if (AttackTimer == 60)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 velocity = new Vector2(10, 0).RotatedBy(Math.PI / 2f * i);
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<VortexOrb>(), LightningStrikeDamage, .5f, Main.myPlayer, i, Phase);
                    }
                }

                if(AttackTimer == 240)
                {
                    for(int i = 0; i < 5; i++)
                    {
                        NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesFragment>(), ai0: NPC.whoAmI, ai1: i, ai3: Phase);
                    }
                }

                if (AttackTimer % 240 == 0)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 16f);
                        if (i % 4 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<StardustShot>(), StardustLaserDamage, 1, Main.myPlayer, NPC.target, 120);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, -1, Main.myPlayer, -1);
                        }
                    }
                }
                if (AttackTimer % 360 == 120)
                {
                    if (AttackTimer % 720 == 120)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -1600).RotatedBy(2 * Math.PI / 5f * i), Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, i, Phase);
                        }

                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Target.Center, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), SolarDetonationDamage, .5f, Main.myPlayer, 0, Phase);
                    }
                    else
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<PoisonTrail>(), PoisonTrailDamage, 2, Main.myPlayer, 2, NPC.whoAmI);
                    }
                }
            }
        }

        private void NextAttack()
        {
            CurrentAttackMode++;
            if (CurrentAttackMode >= CurrentMoveList.Count)
            {
                CurrentAttackMode = 0;
            }

            attackSwitchDelay = 90;
            StartAura(0.2f);

            AttackTimer = 0;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, this.NPC.whoAmI);
            }
        }

        List<List<Color>> Phase0MoveColors;
        List<List<Color>> Phase1MoveColors;
        List<List<Color>> Phase2MoveColors;
        private void InitializeMoves()
        {
            Phase0MoveList = new List<Action>
            {
                ChargingPoisonTrails,
                NebulaShitstorm,
                VortexLightning,
                BlackFireRain,
                DarkLasers,
                StardustLasers,
                SolarDetonators,
            };

            Phase1MoveList = new List<Action>
            {
                DarkLasers,
                SolarAndVortex,
                ChargingPoisonTrails,
            };
            Phase2MoveList = new List<Action>
            {
                DarkLasers,
                ChargingPoisonTrails,
            };           
        }

        private void InitializeColors()
        {
            Phase0MoveColors = new List<List<Color>>
            {
                new List<Color>{ Color.YellowGreen },
                new List<Color>{ Color.Purple * 3 },
                new List<Color>{ new Color(0, 255, 120) },
                new List<Color>{ Color.Purple },
                new List<Color>{ Color.MediumPurple },
                new List<Color>{ Color.Cyan },
                new List<Color>{ Color.OrangeRed }
            };

            Phase1MoveColors = new List<List<Color>>
            {
                new List<Color>{ Color.MediumPurple, Color.Cyan, Color.Purple * 3 },
                new List<Color>{ Color.OrangeRed, Color.Teal },
                new List<Color>{ Color.YellowGreen, Color.MediumPurple }
            };
            Phase2MoveColors = new List<List<Color>>
            {
                new List<Color>{ Color.MediumPurple, Color.OrangeRed, Color.Teal  },
                new List<Color>{ Color.YellowGreen, Color.MediumPurple, Color.Cyan }
            };
        }

        float auraRadius;
        float baseRadius;
        float collapseSpeed;
        float baseFade;
        float fadeSpeed;
        float fadeInPercent;
        float currentFadePercent;
        float ringCollapse;
        float effectTimer;
        public static Effect effect;
        public static Texture2D texture;
        List<Color> auraColors
        {
            get
            {
                InitializeColors();
                if (Phase == 3)
                {
                    return new List<Color> { Color.YellowGreen, Color.Purple * 3, Color.Teal, Color.YellowGreen, Color.MediumPurple, Color.Cyan };
                }
                else
                {
                    switch (Phase)
                    {
                        case 1:
                            return Phase1MoveColors[CurrentAttackMode];
                        case 2:
                            return Phase2MoveColors[CurrentAttackMode];
                        default:
                            return Phase0MoveColors[CurrentAttackMode];
                    }
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);
            auraRadius = 500;            

            if(auraColors == null)
            {
                return false;
            }

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            DrawAura();

            if (deathTimer > 150)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                DrawDeath();

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
            else
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


                //if(deathTimer == 0){
                //Draw the big bad himself
                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
                }

                Color lightingColor = Color.Lerp(Color.White, auraColors[0], 0.5f);
                lightingColor = Color.Lerp(drawColor, lightingColor, 0.5f);
                Rectangle sourceRectangle2 = NPC.frame;
                Vector2 origin2 = sourceRectangle2.Size() / 2f;
                float spriteFade = 1;
                if(deathTimer > 0)
                {
                    spriteFade = 1 - (deathTimer / 150f);
                }
                spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle2, lightingColor * spriteFade, NPC.rotation, origin2, 1, SpriteEffects.None, 0f);
            }
            return false;
        }

        public void DrawDeath()
        {

        }

        public void DrawAura()
        {
            Rectangle baseRectangle = new Rectangle(0, 0, (int)auraRadius, (int)auraRadius);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;

            for (int i = 0; i < auraColors.Count; i++)
            {
                //Pass relevant data to the shader via these parameters
                effect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture1.Width);
                effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
                Color primaryColor = auraColors[i];
                Color secondColor;
                if (i >= auraColors.Count - 1)
                {
                    secondColor = auraColors[0];
                }
                else
                {
                    secondColor = auraColors[i + 1];
                }

                if (attackSwitchDelay > 0)
                {
                    primaryColor = Color.Lerp(primaryColor, Color.White, attackSwitchDelay / 90f);
                    secondColor = Color.Lerp(secondColor, Color.White, attackSwitchDelay / 90f);
                }

                effect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(primaryColor, effectTimer / 25f).ToVector4());
                effect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(secondColor, effectTimer / 25f).ToVector4());
                effect.Parameters["ringProgress"].SetValue(baseRadius);
                effect.Parameters["fadePercent"].SetValue(baseFade);
                effect.Parameters["scaleFactor"].SetValue(.5f * 50);
                effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f * 0.5f);
                effect.Parameters["colorSplitAngle"].SetValue(2f * MathHelper.Pi / auraColors.Count);

                //Apply the shader
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture1, NPC.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.TwoPi * (float)i / (float)auraColors.Count, baseOrigin, NPC.scale, SpriteEffects.None, 0);
            }
        }

        public void StartAura(float radius, float ringSpeed = 1.05f, float fadeOutSpeed = 0.05f)
        {
            baseRadius = radius;
            collapseSpeed = ringSpeed;
            fadeSpeed = fadeOutSpeed;
            currentFadePercent = 0;
            ringCollapse = 1;
            fadeInPercent = 0;
        }

        void HandleAura()
        {
            if (fadeInPercent < 1)
            {
                fadeInPercent += 1f / 30f;
            }
            if (ringCollapse < 0.1f)
            {
                currentFadePercent += fadeSpeed;
            }
            else
            {
                ringCollapse /= collapseSpeed;
            }

            float intensityMinimum = 0.77f;
            float radiusMinimum = 0.06f;


            if (Phase == 3)
            {
                intensityMinimum = 0.45f;
                radiusMinimum = 0.25f;
            }

            if (baseFade < intensityMinimum)
            {
                baseFade += 0.02f;
            }
            if (baseFade > intensityMinimum)
            {
                baseFade = intensityMinimum;
            }
            if (baseRadius > radiusMinimum)
            {
                baseRadius -= 0.01f;
            }
            if (baseRadius < radiusMinimum)
            {
                baseRadius = radiusMinimum;
            }
        }

        float lightCooldown;
        float auraExpandedSize = 0;
        void HandleDeath()
        {
            StartAura(0.2f + auraExpandedSize);
            auraExpandedSize += 0.01f;
            NPC.dontTakeDamage = true;

            //Heavy Impact + lightning sfx to indicate death
            if (deathTimer == 1)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<AttraidiesFragment>())
                    {
                        Main.npc[i].active = false;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                        }
                    }
                }
                ClearObstructiveProjectiles();
                SoundEngine.PlaySound(new SoundStyle("Terraria/Sounds/Thunder_0"));
                SoundEngine.PlaySound(SoundID.Item62);
            }


            if(deathTimer == 150)
            {
                SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh") with { Volume = 2, PlayOnlyIfFocused = false, MaxInstances = 0 });
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, 1), ModContent.ProjectileType<Projectiles.VFX.RealityCrack>(), 0, 0, Main.myPlayer);
            }

            //Spawn distortion lightning effects
            if (deathTimer > 200)
            {
                float delay = 20;
                if(deathTimer > 280)
                {
                    delay = 10;
                }
                if (deathTimer % delay == 0)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<Projectiles.VFX.RealityCrack>(), 0, 0, Main.myPlayer);
                }
                //Growing X shape aura
            }

            if(deathTimer > 400)
            {
                //Attraidies dies
                //Spawn abyss portal NPC
                SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/MetalShatter") with { Volume = 1f, PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);
                NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<NPCs.Special.AbyssPortal>(), ai0: 1);
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.VFX.RealityCrack>());
                tsorcRevampWorld.AbyssPortalLocation = NPC.Center;
                NPC.dontTakeDamage = false;
                NPC.StrikeNPC(999999, 0, 0);
            }
        }

        public override bool CheckDead()
        {
            if (deathTimer < 300)
            {
                NPC.life = 1;
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return base.CanBeHitByProjectile(projectile); 
        }
        bool collisionAllowed = false;

        //Doomed from the start. The square vs circle collision check code could be useful for other physics stuff later though.
        void CustomCollision()
        {
            collisionAllowed = true;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.friendly == true && NPC.immune[projectile.owner] == 0 && projectile.Hitbox.ClosestPointInRect(NPC.Center).Distance(NPC.Center) < 200)
                    {
                        NPC.StrikeNPC(projectile.damage, 0, 0);
                        NPC.immune[projectile.owner] = NPC.immuneTime;
                    }
                }
            }
            collisionAllowed = false;
        }//*/


        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }
        
        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.AttraidiesBag>()));
        }

        public override void OnKill()
        {


            Player player = Main.player[NPC.target];
            //if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:AttraidiesShader"].IsActive())
            //{
            //    Filters.Scene["tsorcRevamp:TheAbyss"].Deactivate();
            //}

            for (int i = 0; i < 50; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(10, 10);
                int dust;
                dust = Dust.NewDust(NPC.Center, 30, 30, DustID.PurpleCrystalShard, vel.X, vel.Y, 100, default, 5f);
                Main.dust[dust].noGravity = true;
                vel = Main.rand.NextVector2Circular(10, 10);
                Dust.NewDust(NPC.Center, 30, 30, DustID.PurpleCrystalShard, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                vel = Main.rand.NextVector2Circular(20, 20);
                Dust.NewDust(NPC.Center, 30, 30, 234, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                Dust.NewDust(NPC.Center, 30, 30, DustID.Torch, vel.X, vel.Y, 200, default, 3f);
            }

            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.TheEnd>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.SoulOfAttraidies>(), Main.rand.Next(15, 23));
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 2000);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.BloomShards>(), 1, false, -1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.HeavenPiercer>());
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())) && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.EstusFlaskShard>());
            }

            if (!tsorcRevampWorld.SuperHardMode)
            {

                UsefulFunctions.BroadcastText("A portal from The Abyss has been opened!", new Color(255, 255, 60));
                UsefulFunctions.BroadcastText("Artorias, the Ancient Knight of the Abyss has entered this world!...", new Color(255, 255, 60));
                UsefulFunctions.BroadcastText("You must seek out the Shaman Elder, beyond the Western Sea...", new Color(249, 202, 12));

                Main.hardMode = true;
                tsorcRevampWorld.SuperHardMode = true;
                tsorcRevampWorld.TheEnd = false;
            }

            else
            {

                UsefulFunctions.BroadcastText("The portal from The Abyss remains open...", new Color(255, 255, 60));
                UsefulFunctions.BroadcastText("You must seek out the Shaman Elder, beyond the Western Sea...", new Color(249, 202, 12));

                tsorcRevampWorld.SuperHardMode = true;
                Main.hardMode = true;
                tsorcRevampWorld.TheEnd = false;
            }
        }

        public static void ActuateAttraidiesArena()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                for (int x = 1158; x < 1633; x++)
                {
                    for (int y = 59; y < 306; y++)
                    {
                        Wiring.ActuateForced(x, y);
                    }
                }
            }
        }
    }
}