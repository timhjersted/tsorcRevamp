using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Placeable.Relics;
using tsorcRevamp.Items.Placeable.Trophies;
using tsorcRevamp.Items.Vanity;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheRage : ModNPC
    {
        int fireTrailsDamage = 24;   
        int rageBreathDamage = 27;
        int demonBoltDamage = 30;
        int homingFireDamage = 33;
        int rageFirebombDamage = 36;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 16000;
            NPC.damage = 60;
            NPC.defense = 32;
            NPC.knockBackResist = 0f;
            NPC.value = 120000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.DD2_BetsyDeath;
            NPC.timeLeft = 22500;

            DrawOffsetY = +70;
            NPC.width = 160;
            NPC.height = 75;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.TheRage.DespawnHandler"), Color.OrangeRed, 127);
        }

        public float flapWings;
        int hitTime = 0;

        public float FlameShotTimer;
        public float FlameShotCounter;

        public float FlameShotTimer2;
        public float FlameShotCounter2;

        public float FlameShotTimer3;
        public float FlameShotCounter3;

        int holdTimer = 0;
        int breathTimer = 0;

        public bool secondPhase
        {
            get => NPC.life <= NPC.lifeMax / 2;
        }

        public int Timer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public Player Target
        {
            get => Main.player[NPC.target];
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            NPC.width = 160;
            NPC.height = 75;
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            HandleScreenShader();
            if (NPC.life == 1)
            {
                deathTimer++;
                if (deathTimer > 60)
                {
                    NPC.StrikeNPC(NPC.CalculateHitInfo(9999, 1, true, 0), false, false);
                }

                if (deathTimer % 5 == 0 && Main.myPlayer != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                }
            }

            NPC.ai[2]++;
            NPC.ai[1]++;
            hitTime++;
            if (NPC.ai[0] > 0)
            {
                NPC.ai[0] -= hitTime / 10;
            }

            flapWings++;

            //Flap Wings Sound
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.position); //wing flap sound

            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, PitchVariance = 1f }, NPC.position);
                flapWings = 0;
            }

            // Demon Attack - 1st phase
            if (NPC.life > NPC.lifeMax / 2)
            {
                FlameShotTimer2++;
            }
            // Flame attack - 2nd phase
            if (NPC.life < NPC.lifeMax / 2)
            {
                FlameShotTimer++;
                FlameShotTimer3++;
            }

            Player player = Main.player[NPC.target];

            // Announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }

            // Proximity Debuffs
            if (NPC.Distance(player.Center) < 1550 && NPC.life < NPC.lifeMax / 2)
            {
                player.AddBuff(BuffID.OnFire, 30, false);
                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TheRage.HeatWave"), 235, 199, 23);//deep yellow
                    holdTimer = 15000;
                }
            }

            // Getting close to the rage triggers on fire!
            if (NPC.Distance(player.Center) < 40)
            {
                player.AddBuff(BuffID.OnFire, 300, false);
            }

            // Chance to trigger fire from above / 2nd phase 
            if (Main.rand.NextBool(500))
            {
                FlameShotCounter2 = 0;
                NPC.netUpdate = true;
            }

            // Demon Sickle Attack: 1st Phase
            // Counts up each tick. used to space out shots
            if (FlameShotTimer2 >= 30 && FlameShotCounter2 < 6 && NPC.Distance(player.Center) > 200)
            {
                for (int i = 0; i < 20; i++)
                {
                    int fireDust = Dust.NewDust(NPC.Center, DustID.ShadowbeamStaff, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                    Main.dust[fireDust].noGravity = true;
                }
                // Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, PitchVariance = 2 }, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 0.035f); //needs to be slow for demon sickle
                    speed += Main.player[NPC.target].velocity / 5; //10 works for demon sickle, /2 was way too sensitive to player speed

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.Birbs.RageDemonBolt>(), demonBoltDamage, 0f, Main.myPlayer);
                }

                FlameShotTimer2 = 0;
                FlameShotCounter2++;
            }

            // Fire From Above Attack: 2nd Phase
            // Counts up each tick. used to space out shots
            if (FlameShotTimer >= 45 && FlameShotCounter < 15)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 600 + Main.rand.Next(1200), (float)player.position.Y - 600f, (float)(-40 + Main.rand.Next(80)) / 10, 2f, ModContent.ProjectileType<FireTrails>(), fireTrailsDamage, 2f, Main.myPlayer); //  ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>()
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.9f, PitchVariance = 2 }, NPC.Center);

                FlameShotTimer = 0;
                FlameShotCounter++;
            }
            // Homing Fireballs Attack: Part of 2nd phase 
            if (FlameShotTimer3 >= 25 && FlameShotCounter3 < 5)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 200 + Main.rand.Next(400), (float)player.position.Y - 480f, (float)(-40 + Main.rand.Next(80)) / 10, 3f, ProjectileID.CultistBossFireBall, homingFireDamage, 3f, Main.myPlayer); //ProjectileID.NebulaBlaze2 would be cool to use at the end of attraidies or gwyn fight with the text, "The spirit of your father summons cosmic light to aid you!"
                }
                FlameShotTimer3 = 0;
                FlameShotCounter3++;
            }

            // Fire Breath Attack: Aggressive frequency, during all phases of movement, triggers at 3/4th health
            if (NPC.life < NPC.lifeMax / 2 * 1.5)
            {
                breathTimer++;
            }
            if (breathTimer > 780)
            {
                breathTimer = -19;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 12);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, breathVel, ModContent.ProjectileType<Projectiles.Enemy.Birbs.RageFireBreath>(), rageBreathDamage, 0f, Main.myPlayer);
                }
            }
            if (breathTimer < 0)
            {
                //NPC.velocity.X *= 0.15f;
                //NPC.velocity.Y *= 0.15f;
                // Play breath sound
                if (Main.rand.NextBool(3))
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 1.3f, PitchVariance = 1f }, NPC.Center); //flame thrower
                }
            }
            if (breathTimer > 580)
            {

                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1);
            }
            if (breathTimer == 580 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.GlowingEnergy>(), 0, 0, Main.myPlayer, NPC.whoAmI, UsefulFunctions.ColorToFloat(Color.Red));
            }


            // Fire Bomb Attack
            Timer++;

            int bombFrequency = 325; // 1st phase frequency
            if (NPC.life < NPC.lifeMax / 2)
            {
                bombFrequency = 125; // 2nd phase frequency
            }

            if (Main.rand.NextBool(bombFrequency) && player.position.Y + 30 >= NPC.position.Y)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int pink = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CrystalSerpent, NPC.velocity.X, NPC.velocity.Y, Scale: 3f);
                        Main.dust[pink].noGravity = true;
                    }


                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 6.5f, .1f, true, true);
                    velocity += Target.velocity / 1.5f;

                    if (velocity != Vector2.Zero && Math.Abs(velocity.X) < -velocity.Y) //No throwing if it failed to find a valid trajectory, or if it'd throw at too shallow of an angle for players to dodge
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.Birbs.RageFirebomb>(), rageFirebombDamage, 0.5f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity + Main.rand.NextVector2Circular(1, 1), ModContent.ProjectileType<Projectiles.Enemy.Birbs.RageFirebomb>(), rageFirebombDamage, 0.5f, Main.myPlayer); //ProjectileID.LostSoulHostile
                    }

                }

            }

            // Spawn Meteor Hell at 1/5th life
            if (Main.rand.NextBool(120) && NPC.Distance(player.Center) > 160 && NPC.life <= NPC.lifeMax / 5)
            {

                if (player.position.Y + 50 >= NPC.position.Y)
                {
                    int Meteor = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), NPCID.MeteorHead, 0);

                    for (int i = 0; i < 20; i++)
                    {
                        int fireDust = Dust.NewDust(new Vector2(NPC.Center.X + 500, NPC.Center.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                        fireDust = Dust.NewDust(new Vector2(NPC.Center.X - 500, NPC.Center.Y - 100), DustID.Shadowflame, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                        Main.dust[fireDust].noGravity = true;
                    }

                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit24 with { Volume = 0.5f }, NPC.Center);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Meteor, 0f, 0f, 0f, 0);
                    }
                }
            }

            if (NPC.ai[3] == 0)
            {
                // Normal Phase
                NPC.alpha = 0;
                NPC.damage = 70;
                //NPC.defense = 32;
                NPC.netUpdate = true;

                if (NPC.ai[2] < 600)
                {
                    if (Main.player[NPC.target].position.X < NPC.Center.X)
                    {

                        if (NPC.velocity.X > -8) { NPC.velocity.X -= 0.22f; }
                    }
                    if (Main.player[NPC.target].position.X > NPC.Center.X)
                    {
                        if (NPC.velocity.X < 8) { NPC.velocity.X += 0.22f; }
                    }

                    if (Main.player[NPC.target].position.Y < NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].position.Y > NPC.Center.Y + 300)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }

                    if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                    {
                        float speed = 6f;
                        int type = ModContent.ProjectileType<FireTrails>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.7f, PitchVariance = 1f }, NPC.Center); //flame thrower
                        float rotation = (float)Math.Atan2(NPC.Center.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), NPC.Center.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + 600, NPC.Center.Y - 120, (float)((Math.Cos(rotation) * speed) * -1), (float)((Math.Sin(rotation) * speed) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 120, (float)((Math.Cos(rotation + 0.2) * speed) * -1), (float)((Math.Sin(rotation + 0.4) * speed) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X - 600, NPC.Center.Y - 120, (float)((Math.Cos(rotation - 0.2) * speed) * -1), (float)((Math.Sin(rotation - 0.4) * speed) * -0.45), type, fireTrailsDamage, 0f, Main.myPlayer);
                        NPC.ai[1] = -90;
                        // Added some dust so the projectiles aren't just appearing out of thin air
                        for (int i = 0; i < 20; i++)
                        {
                            int fireDust = Dust.NewDust(new Vector2(NPC.Center.X + 600, NPC.Center.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(NPC.Center.X - 600, NPC.Center.Y - 120), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Purple, 2f);
                            Main.dust[fireDust].noGravity = true;
                        }
                    }
                }
                else if (NPC.ai[2] >= 600 && NPC.ai[2] < 850)
                {
                    FlameShotCounter3 = 0;
                    // Then chill for a second.
                    // This exists to delay switching to the 'charging' pattern for a moment to give time for the player to make distance
                    NPC.velocity.X *= 0.95f;
                    NPC.velocity.Y *= 0.95f;

                }
                else if (NPC.ai[2] >= 850 && NPC.ai[2] < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {

                        float rotation = (float)Math.Atan2(NPC.Center.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), NPC.Center.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = (float)(Math.Cos(rotation) * 22) * -1;
                        NPC.velocity.Y = (float)(Math.Sin(rotation) * 22) * -1;//22 w 25, seems to be speed of dash}
                    }
                    }
                    else NPC.ai[2] = 0;
                    FlameShotCounter = 0;
                }
                else
                {
                    // Enrage Phase              
                    NPC.ai[3]++;
                    //NPC.alpha = 210; //No longer goes invisible
                    //NPC.defense = 32;
                    NPC.damage = 140;
                    NPC.netUpdate = true;

                    // FlameShotCounter2 = 0;

                    if (Main.player[NPC.target].position.X < NPC.Center.X)
                    {
                        if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                    }
                    if (Main.player[NPC.target].position.X > NPC.Center.X)
                    {
                        if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                    }
                    if (Main.player[NPC.target].position.Y < NPC.Center.Y)
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                        else NPC.velocity.Y -= 0.07f;
                    }
                    if (Main.player[NPC.target].position.Y > NPC.Center.Y)
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                        else NPC.velocity.Y += 0.07f;
                    }
                    if (NPC.ai[1] >= 0 && NPC.ai[2] > 120 && NPC.ai[2] < 600)
                    {

                        float speed = 7f;
                        float invulnDamageMult = 1.64f;
                        int type = ModContent.ProjectileType<FireTrails>();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        float rotation = (float)Math.Atan2(NPC.Center.Y - 600 - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), NPC.Center.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + 300, NPC.Center.Y - 150, (float)((Math.Cos(rotation) * speed) * -1), (float)((Math.Sin(rotation) * speed) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y - 150, (float)((Math.Cos(rotation + 0.2) * speed) * -1), (float)((Math.Sin(rotation + 0.4) * speed) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X - 300, NPC.Center.Y - 150, (float)((Math.Cos(rotation - 0.2) * speed) * -1), (float)((Math.Sin(rotation - 0.4) * speed) * -0.45), type, (int)(fireTrailsDamage * invulnDamageMult), 0f, Main.myPlayer);
                        NPC.ai[1] = -90;
                        // Added some dust so the projectiles aren't just appearing out of thin air
                        for (int i = 0; i < 20; i++)
                        {
                            int fireDust = Dust.NewDust(new Vector2(NPC.Center.X + 300, NPC.Center.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(NPC.Center.X, NPC.Center.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                            fireDust = Dust.NewDust(new Vector2(NPC.Center.X - 300, NPC.Center.Y - 150), 20, 20, 244, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.Orange, 2f);
                            Main.dust[fireDust].noGravity = true;
                        }
                    }

                    if (NPC.ai[3] == 100)
                    {
                        NPC.ai[3] = 1;

                        // Gains life on enrage: re-added to fit with rage theme                 
                        NPC.life += 300;

                        if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
                    }
                    if (NPC.ai[1] >= 0)
                    {
                        NPC.ai[3] = 0;
                        for (int i = 0; i < 40; i++)
                        {
                            Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, 0, 0, 0, new Color(), 3f);
                        }
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 1200, 60);
                        }


                    }
                }
            }
        public override void FindFrame(int currentFrame)
        {
            if (Main.dedServ)
            {
                return;
            }

            NPC.frame.Width = TextureAssets.Npc[NPC.type].Value.Width;
            NPC.frame.Height = TextureAssets.Npc[NPC.type].Value.Height;

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / 7;
            }
            NPC.frame.Height = num;

            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * 7)
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.ai[0] += hit.Damage;
        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            hitTime = 0;
            if (NPC.ai[0] > (NPC.lifeMax / 10))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TheRage.Enrage"), Color.Orange);

                NPC.ai[3] = 1;
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, 0, 0, 100, default, 3f);
                }
                NPC.ai[1] = -250;
                NPC.ai[0] = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 1200, 60);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, NPC.Center);
            }
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheRageBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrestOfFire>(), 1, 2, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.CobaltDrill));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PhoenixEgg>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TheRageMask>(), 7));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<TheRageRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheRageTrophy>(), 10));
        }

        int deathTimer;
        public override bool CheckDead()
        {
            if (deathTimer < 60)
            {
                NPC.life = 1;
                return false;
            }
            return true;
        }

        public override void OnKill()
        {
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1.3f });

            if (FilterID != null)
            {
                if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID] != null && Filters.Scene[FilterID].IsActive())
                {
                    Filters.Scene[FilterID].Deactivate();
                    tsorcRevampWorld.boundShaders.Remove(FilterID);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }

            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom") with { Volume = 1.3f });

            for (int i = 0; i < 100; i++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 127, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 70; i++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 130, Main.rand.Next(-50, 50), Main.rand.Next(-40, 40), 100, Color.Orange, 3f);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (!Main.dedServ)
            {
                if (NPC.life <= 0)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheRage_Gore_1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheRage_Gore_2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheRage_Gore_3").Type, 1f);
                }
            }
        }

        public static string FilterID = "RageFilter";
        public void HandleScreenShader()
        {
            if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID] == null)
            {
                Filters.Scene[FilterID] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/HeatWave", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "HeatWavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
                tsorcRevampWorld.boundShaders.Add(FilterID);
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene[FilterID].IsActive())
            {
                Filters.Scene.Activate(FilterID, NPC.Center).GetShader().UseTargetPosition(NPC.Center);
                if (!tsorcRevampWorld.boundShaders.Contains(FilterID))
                {
                    tsorcRevampWorld.boundShaders.Add(FilterID);
                }
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID].IsActive())
            {
                float exponent = 0.15f;
                float intensity = 0.02f;
                if (NPC.life < NPC.lifeMax / 2)
                {
                    exponent = MathHelper.Lerp(0.1f, 1, ((float)NPC.life / NPC.lifeMax) * 2);
                }
                else if (NPC.ai[3] == 0)
                {
                    intensity = 0;
                }

                if (NPC.ai[3] != 0)
                {
                    exponent *= 2;
                    intensity *= 4;
                }

                Filters.Scene[FilterID].GetShader().UseTargetPosition(NPC.Center + new Vector2(0, -150)).UseOpacity(exponent).UseIntensity(intensity);
            }
        }

        public static Texture2D blurTexture;
        public static Texture2D enrageTexture;
        public static Texture2D glowmaskTexture;
        public static Effect rageEffect;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);
            UsefulFunctions.EnsureLoaded(ref blurTexture, "tsorcRevamp/NPCs/Bosses/TheRageBlur");
            UsefulFunctions.EnsureLoaded(ref enrageTexture, "tsorcRevamp/NPCs/Bosses/TheRageEnrage");
            UsefulFunctions.EnsureLoaded(ref glowmaskTexture, "tsorcRevamp/NPCs/Bosses/TheRageGlowmask");

            if (rageEffect == null || rageEffect.IsDisposed)
            {
                rageEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RageEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            rageEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            rageEffect.Parameters["length"].SetValue(.07f * 1000);
            rageEffect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            rageEffect.Parameters["sourceRectY"].SetValue(NPC.frame.Y);

            float opacity = NPC.ai[0] / (NPC.lifeMax / 10f) * 0.8f;
            if (NPC.ai[3] != 0)
            {
                opacity = 1;
            }

            //Its death animation works a tad different from its normal draw code
            if (deathTimer > 0)
            {
                opacity = deathTimer / 60f;

                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glowmaskTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                rageEffect.Parameters["opacity"].SetValue(opacity);
                rageEffect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(enrageTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                return false;
            }

            rageEffect.Parameters["opacity"].SetValue(opacity);
            rageEffect.CurrentTechnique.Passes[0].Apply();

            if (NPC.ai[3] == 0)
            {
                Main.spriteBatch.Draw(blurTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, 1.5f * NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, 1.5f * NPC.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glowmaskTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, 1.5f * NPC.scale, SpriteEffects.None, 0);
            }
            else
            {
                Main.spriteBatch.Draw(enrageTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, 1.5f * NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
            }


            return false;
        }

        /*         
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            RenderTarget2D rageTarget = new RenderTarget2D(device, enrageTexture.Width, enrageTexture.Height, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);
            device.SetRenderTarget(rageTarget);
            device.Clear(Color.Transparent);

            Effect blurEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlurEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //blurEffect.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(enrageTexture, Vector2.Zero, new Rectangle(0, 0, enrageTexture.Width, enrageTexture.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

            char separator = Path.DirectorySeparatorChar;
            string path = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "rageEnrage.png";
            FileStream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            rageTarget.SaveAsPng(stream, enrageTexture.Width, enrageTexture.Height);
            stream.Close();
            stream.Dispose();
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            device.SetRenderTarget(null);
         
         
         */
    }
}
