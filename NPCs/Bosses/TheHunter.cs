using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
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
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class TheHunter : ModNPC
    {
        int sproutDamage = 33;
        int cursedBreathDamage = 30;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 22000;
            NPC.damage = 65;
            NPC.defense = 36;
            NPC.knockBackResist = 0f;
            NPC.scale = 1.4f;
            NPC.value = 220000;
            NPC.npcSlots = 6;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            DrawOffsetY = 70;
            NPC.width = 110;
            NPC.height = 60;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.TheHunter.DespawnHandler"), Color.Green, 89);
        }

        int hitTime = 0;
        public float flapWings;
        public float FrogSpawnTimer;
        public float FrogSpawnCounter;
        bool ChildrenSpawned = false;
        float breathTimer = 60;
        float breathTimer2 = 600;

        public float EnrageDamageCounter
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float SprouterShotTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public float AttackPhaseTimer
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        public float EnrageTimer
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }
        public bool PhaseTwo
        {
            get => NPC.life < NPC.lifeMax / 2;
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            SprouterShotTimer++;
            AttackPhaseTimer++;
            hitTime++;

            Player player = Main.player[NPC.target];

            if (EnrageDamageCounter > 0)
            {
                EnrageDamageCounter -= hitTime / 10;
            }


            if (NPC.life == 1)
            {
                deathTimer++;
                if (deathTimer > 60)
                {
                    NPC.StrikeNPC(NPC.CalculateHitInfo(9999, 1, true, 0), false, false);
                }

                if (deathTimer % 5 == 0 && Main.myPlayer != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.YellowGreen));
                }
            }

            // Flap Wings
            flapWings++;
            if (flapWings == 30 || flapWings == 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.position); //wing flap sound
            }
            if (flapWings == 95)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = -0.1f }, NPC.position);
                flapWings = 0;
            }

            // Frogs spawn above half health
            if (!PhaseTwo)
            {
                FrogSpawnTimer++;
            }

            // Both phases: Ichor debuff triggers when super close
            if (NPC.Distance(player.Center) < 20)
            {
                player.AddBuff(BuffID.Ichor, 180, false);
            }

            // Spawn the child!
            if (!ChildrenSpawned && PhaseTwo)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int Child = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.TheHunterChild>(), 0);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Child, 0f, 0f, 0f, 0);
                    }
                }

                if (NPC.Distance(player.Center) < 1550)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TheHunter.Child"), 235, 199, 23);//deep yellow
                }

                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.01f }, NPC.Center);
                ChildrenSpawned = true;
            }


            // Mutant Toad Spawn
            // Counts up each tick. used to space out spawns
            if (FrogSpawnTimer >= 120 && FrogSpawnCounter < 3 && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.MutantToad>()) < 3)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.MutantToad>(), 0);
                }

                FrogSpawnTimer = 0;
                FrogSpawnCounter++;

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie13 with { Volume = 0.5f }, NPC.Center);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PoisonStaff, NPC.velocity.X, NPC.velocity.Y);
            }

            // Chance to trigger frogs spawning
            if (Main.rand.NextBool(600) && !PhaseTwo)
            {
                FrogSpawnCounter = 0;
            }

            // Normal Phase
            if (EnrageTimer == 0)
            {
                NPC.damage = 130;
                NPC.defense = 26;

                //For the first 10 seconds: Fire miracle sprouters
                //Chance to use 'final breath attack' if below 25% health
                if (AttackPhaseTimer < 600)
                {
                    int breathTimerCap = 220;

                    // Final Breath Attack. Only initiate it if below 25% health
                    if (NPC.life <= NPC.lifeMax / 4)
                    {
                        if (AttackPhaseTimer == 300)
                        {
                            breathTimer2 = breathTimerCap + 200;

                            //Telegraph breath
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.GlowingEnergy>(), 0, 0, Main.myPlayer, NPC.whoAmI, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                            }
                        }                           
                    }

                    //Pause the AttackTimer at 300 until the breath attack is over
                    //Once the breath timer hits 0, increment AttackTimer to move on to the next attack phase
                    if (breathTimer2 > 0)
                    {
                        AttackPhaseTimer = 300;
                        breathTimer2--;

                        //Slow down NPC
                        if (breathTimer2 > breathTimerCap)
                        {
                            NPC.velocity.X *= 0.95f;
                            NPC.velocity.Y *= 0.95f;
                        }

                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, Main.rand.Next(-2, 2), Main.rand.Next(-20, 2), 200, Color.OrangeRed, 1f);
                        Lighting.AddLight(NPC.Center, Color.PaleVioletRed.ToVector3() * 5);

                        //Slow NPC and fire breath projectiles
                        if (breathTimer2 < breathTimerCap)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 9);
                                breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.CursedDragonsBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                            }

                            //play breath sound
                            if (Main.rand.NextBool(3))
                            {
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, PitchVariance = 1f }, NPC.Center); //flame thrower
                            }
                        }
                    }
                    else
                    {
                        //If not doing breath attack, do movement and fire sprouters
                        //Movement
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

                        //Fire sprouters
                        if (SprouterShotTimer >= 0 && AttackPhaseTimer > 120 && AttackPhaseTimer < 600)
                        {
                            int type = ModContent.ProjectileType<MiracleSprouter>();
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.8f, PitchVariance = 2f }, NPC.Center);
                            Vector2 aimVector = UsefulFunctions.Aim(NPC.Center, player.Center, 9);
                            Vector2 fireOriginOffset = new Vector2(0, -70);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector + player.velocity, type, sproutDamage, 0, Main.myPlayer, 55);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(-0.4) + player.velocity, type, sproutDamage, 0, Main.myPlayer, 55);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(0.4) + player.velocity, type, sproutDamage, 0, Main.myPlayer, 55);
                            }
                            SprouterShotTimer = -90;
                            NPC.netUpdate = true;
                        }
                    }
                }
                //From 10 to 14 seconds: Chance to use normal breath attack
                else if (AttackPhaseTimer >= 600 && AttackPhaseTimer < 850) //was 750
                {
                    //Decide how long the breath attack should be
                    int breathTimerCap = 140;
                    if (PhaseTwo)
                    {
                        breathTimerCap = 190;
                    }

                    //Randomly decide whether to use the breath attack this time
                    if (AttackPhaseTimer == 600)
                    {
                        breathTimer = breathTimerCap + 200;

                        //Telegraph breath
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.GlowingEnergy>(), 0, 0, Main.myPlayer, NPC.whoAmI, UsefulFunctions.ColorToFloat(Color.YellowGreen));
                        }
                    }

                    //Pause the AttackTimer at 848 until the breath attack is over
                    //Once the breath timer hits 0, increment AttackTimer to move on to the next attack phase
                    if(breathTimer > 0)
                    {
                        AttackPhaseTimer = 848;
                        breathTimer--;
                        if(breathTimer == 0)
                        {
                            AttackPhaseTimer++;
                        }

                        //Hold NPC in place
                        if (breathTimer > breathTimerCap)
                        {
                            NPC.velocity.X *= 0.95f;
                            NPC.velocity.Y *= 0.95f;
                        }

                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 131, Main.rand.Next(-2, 2), Main.rand.Next(-20, 2), 200, default, 1f);
                        Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
                    }
                    

                    //Slow NPC and fire breath projectiles
                    if (breathTimer < breathTimerCap)
                    {
                        if (player.Center.X < NPC.Center.X)
                        {
                            NPC.velocity.X -= 0.1f;
                        }
                        if (player.Center.X > NPC.Center.X)
                        {
                            NPC.velocity.X += 0.1f;
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 9);
                            breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedBreathDamage, 0f, Main.myPlayer);
                        }

                        //play breath sound
                        if (Main.rand.NextBool(3))
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.9f, PitchVariance = 1f }, NPC.Center); //flame thrower
                        }
                    }
                }
                //From 14 to 24 seconds: Dashes
                else if (AttackPhaseTimer >= 850 && AttackPhaseTimer < 1350)
                {
                    NPC.velocity.X *= 0.98f;
                    NPC.velocity.Y *= 0.98f;
                    if ((NPC.velocity.X < 2f) && (NPC.velocity.X > -2f) && (NPC.velocity.Y < 2f) && (NPC.velocity.Y > -2f))
                    {
                        float rotation = (float)Math.Atan2((NPC.Center.Y) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), (NPC.Center.X) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        NPC.velocity.X = ((float)(Math.Cos(rotation) * 25) * -1) + Main.player[NPC.target].velocity.X;
                        NPC.velocity.Y = ((float)(Math.Sin(rotation) * 25) * -1) + Main.player[NPC.target].velocity.Y;
                    }
                }
                //Then reset the loop
                else AttackPhaseTimer = 0;
            }
            else
            {
                // Enrage Phase
                EnrageTimer++;
                NPC.alpha = 235;
                NPC.defense = 70;//+14 for all birds
                NPC.damage = 170;//+10 for all birds

                //Movement
                if (player.Center.X < NPC.Center.X)
                {
                    if (NPC.velocity.X > -6) { NPC.velocity.X -= 0.22f; }
                }
                if (player.Center.X > NPC.Center.X)
                {
                    if (NPC.velocity.X < 6) { NPC.velocity.X += 0.22f; }
                }
                if (player.Center.Y < NPC.Center.Y)
                {
                    if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.8f;
                    else NPC.velocity.Y -= 0.07f;
                }
                if (player.Center.Y > NPC.Center.Y)
                {
                    if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.8f;
                    else NPC.velocity.Y += 0.07f;
                }

                //Fire miracle sprouters quickly while enraged
                if (SprouterShotTimer >= 0)
                {
                    float invulnDamageMult = 1.72f; //+0.20 for all birds
                    int type = ModContent.ProjectileType<MiracleSprouter>();
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    Vector2 aimVector = UsefulFunctions.Aim(NPC.Center, player.Center, 12);
                    Vector2 fireOriginOffset = new Vector2(0, -70);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 35);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(-0.4) + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 40);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + fireOriginOffset, aimVector.RotatedBy(0.4) + player.velocity, type, (int)(sproutDamage * invulnDamageMult), 0, Main.myPlayer, 35);
                    }
                    SprouterShotTimer = -90;
                    NPC.netUpdate = true;
                }

                //End enrage
                if (EnrageTimer >= 0)
                {
                    EnrageTimer = 0;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 1200, 60);
                    }
                    for (int i = 0; i < 40; i++)
                    {
                        Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 0, default, 1f); //was 3f
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
            Player player = Main.player[NPC.target];

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
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
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (EnrageTimer == 0)
            {
                NPC.alpha = 0;

                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 0.5f);
            }
            else
            {
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
            }
        }

        public static string FilterID = "HunterFilter";
        public void HandleScreenShader()
        {
            if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID] == null)
            {
                Filters.Scene[FilterID] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/Meltwater", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "EffectPass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
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
                else if (EnrageTimer == 0)
                {
                    intensity = 0;
                }

                if (EnrageTimer != 0)
                {
                    exponent *= 2;
                    intensity *= 4;
                }

                Filters.Scene[FilterID].GetShader().UseTargetPosition(NPC.Center + new Vector2(0, -150)).UseOpacity(exponent).UseIntensity(intensity);
            }
        }

        public static Texture2D blurTexture;
        public static Texture2D glowmaskTexture;
        public static Effect hunterEffect;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);
            UsefulFunctions.EnsureLoaded(ref blurTexture, "tsorcRevamp/NPCs/Bosses/TheHunterBlur");
            UsefulFunctions.EnsureLoaded(ref glowmaskTexture, "tsorcRevamp/NPCs/Bosses/TheHunterGlowmask");

            if (hunterEffect == null || hunterEffect.IsDisposed)
            {
                hunterEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HunterEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            hunterEffect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            hunterEffect.Parameters["length"].SetValue(.07f * 1000);

            hunterEffect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            hunterEffect.Parameters["sourceRectY"].SetValue(NPC.frame.Y);

            float opacity = EnrageDamageCounter / (NPC.lifeMax / 10f) * 0.8f;
            if (EnrageTimer != 0)
            {
                opacity = 1;
            }

            //Its death animation works a tad different from its normal draw code
            if (deathTimer > 0)
            {
                opacity = deathTimer / 60f;

                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(glowmaskTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);
                hunterEffect.Parameters["opacity"].SetValue(opacity);
                hunterEffect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                return false;
            }

            hunterEffect.Parameters["opacity"].SetValue(opacity);
            hunterEffect.CurrentTechnique.Passes[0].Apply();

            //Only draw the bird if it's not enraged
            if (EnrageTimer == 0 || Main.LocalPlayer.detectCreature)
            {
                Main.spriteBatch.Draw(blurTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                Main.spriteBatch.Draw(TextureAssets.Npc[NPC.type].Value, NPC.Center - Main.screenPosition, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
                Main.spriteBatch.Draw(glowmaskTexture, NPC.Center - Main.screenPosition, NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() / 2, NPC.scale, SpriteEffects.None, 0);
            }

            return false;
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


        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Bleeding, 30 * 60, false);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            EnrageDamageCounter += hit.Damage;
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            EnrageDamageCounter += hit.Damage;
        }
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            hitTime = 0;

            //Activate enrage if damage counter has stacked up more than 10% of its health
            if (EnrageDamageCounter > (NPC.lifeMax / 10))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TheHunter.Enrage"), Color.Orange);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 1200, 60);
                }

                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 4, 0, 0, 100, default, 3f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 18, 0, 0, 100, default, 3f);
                }

                EnrageTimer = -300;
                SprouterShotTimer = 0;
                EnrageDamageCounter = 0;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.TheHunterBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrestOfEarth>(), 1, 2, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.Drax));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.AngelWings));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TheHunterMask>(), 7));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<TheHunterRelic>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TheHunterTrophy>(), 10));
        }

        public override void OnKill()
        {
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
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.YellowGreen));
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Gore_1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Gore_2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("TheHunter_Gore_3").Type, 1f);                
            }

            for (int i = 0; i < 100; i++)
            {
                int dust = Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 89, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 100; i++)
            {
                Dust.NewDust(NPC.position, (int)(NPC.width * 1.5), (int)(NPC.height * 1.5), 131, Main.rand.Next(-30, 30), Main.rand.Next(-20, 20), 100, Color.Orange, 3f);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
        }
    }
}
