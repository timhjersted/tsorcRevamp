using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Marilith;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    [AutoloadBossHead]
    class FireFiendMarilith : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 120;
            NPC.height = 160;
            NPC.damage = 60;
            NPC.defense = 38;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 400000;
            NPC.timeLeft = 22500;
            NPC.alpha = 100;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("Fire Fiend Marilith decends to the underworld...", Color.OrangeRed, DustID.FireworkFountain_Red);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Fiend Marilith");
        }

        int holdBallDamage = 40;
        int fireBallDamage = 45;
        int lightningDamage = 45;
        int fireStormDamage = 50;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
        }


        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        MarilithMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<MarilithMove> MoveList;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveCounter
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public Player Target
        {
            get => Main.player[NPC.target];
        }

        public int MoveTimer = 0;
        NPCDespawnHandler despawnHandler;
        float introTimer = 0;
        bool displayedWarning = false;
        public override void AI()
        {
            if(introTimer < 120)
            {
                introTimer++;
            }
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.8f, 0f, 0.2f);
            MoveTimer++;
            
            if (MoveList == null)
            {
                InitializeMoves();
                InitializeFirewalls();                
            }

            testAttack = 2;
            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveIndex >= MoveList.Count)
            {
                MoveIndex = 0;
            }

            //Main.NewText(Main.dust);
            CurrentMove.Move();

            for(int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    float distance = NPC.Distance(Main.player[i].Center);


                    if (!Main.player[i].fireWalk && distance < (270f * introTimer / 120f))
                    {
                        Main.player[i].AddBuff(BuffID.OnFire, 180);
                        Main.player[i].AddBuff(BuffID.Burning, 180);
                    }

                    if(distance <  2000)
                    {
                        Main.player[i].AddBuff(BuffID.Oiled, 180);
                        Main.player[i].AddBuff(BuffID.OnFire, 180);

                        bool hasCovenant = false;
                        for (int j = 3; j < 8 + Main.player[i].GetAmountOfExtraAccessorySlotsToShow(); j++)
                        {
                            if (Main.player[i].armor[j].type == ModContent.ItemType<Items.Accessories.Defensive.CovenantOfArtorias>())
                            {
                                hasCovenant = true;
                                break;
                            }
                        }

                        if (!hasCovenant && !displayedWarning)
                        {
                            UsefulFunctions.BroadcastText("The abyss permiates the arena, without protection you will not survive...", Color.Purple);
                            displayedWarning = true;
                        }
                    }
                }
            }
        }

        //Marilith fires a barrage of fireballs which home in on the player
        //A dust ring around them shrinks, and when it hits radius 0 the bombs explode like Voodoo Shaman poison storms (except fire)
        private void ExpandingFireBombs()
        {
            MarilithFloat();
            if (MoveTimer % 240 == 0 && Main.netMode != NetmodeID.MultiplayerClient && MoveTimer <= 1250)
            {
                float speed = 8;

                Vector2 predictiveVector = new Vector2(0, speed).RotatedByRandom(MathHelper.Pi);

                
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, predictiveVector, ModContent.ProjectileType<MarilithCataclysm>(), fireStormDamage, 0, Main.myPlayer, 0, Target.whoAmI);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, predictiveVector.RotatedBy(2 * MathHelper.Pi / 3), ModContent.ProjectileType<MarilithCataclysm>(), fireStormDamage, 0, Main.myPlayer, 0, Target.whoAmI);
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, predictiveVector.RotatedBy(-2 * MathHelper.Pi / 3), ModContent.ProjectileType<MarilithCataclysm>(), fireStormDamage, 0, Main.myPlayer, 0, Target.whoAmI);
                
            }

            if (MoveTimer >= 1300)
            {
                NextAttack();
            }
        }

        //Marilith summons a plume of ash that rises to the top of the arena and spreads out, creating storm clouds across the top that rain and extinguish the fire
        //Lightning and fireballs rain down on the player while Marilith fires homing fireballs right at the player
        //When the attack ends and the fire around the arena border re-ignites
        private void VolcanicStorm()
        {
            MarilithFloat();            
            if(MoveTimer < 300)
            {
                Vector2 smokeOrigin = NPC.Center;
                smokeOrigin.Y -= 100;

                int i = (int)smokeOrigin.X / 16;
                int j = (int)smokeOrigin.Y / 16;
                int heightToTop = 0;
                for (int index = 1; index < 800; index++)
                {
                    if (!Main.tile[i, j - index].HasTile || (!Main.tileSolid[Main.tile[i, j - index].TileType]))
                    {
                        heightToTop++;
                    }
                    else
                    {
                        break;
                    }
                }

                for (int l = 0; l < 5; l++)
                {
                    float height;

                    if(heightToTop * 16 <= 100)
                    {
                        height = 0;
                    }
                    else
                    {
                        height = Main.rand.Next((-heightToTop * 16), -100);
                    }

                    float percent = (MoveTimer / 180f);
                    if(percent > 1)
                    {
                        percent = 1;
                    }
                    height *= percent;

                    smokeOrigin.Y = NPC.Center.Y + height;
                    Dust d = Dust.NewDustPerfect(smokeOrigin + new Vector2(0, 48), 174, Vector2.Zero, 0, default, 2);
                    d.noGravity = true;
                    d.velocity.X = Main.rand.NextFloat(-height / 20, height / 20);
                    d.velocity.Y = -8;
                    int goreIndex = Gore.NewGore(NPC.GetSource_FromThis(), smokeOrigin + new Vector2(-15, 0), default(Vector2), Main.rand.Next(61, 64), Main.rand.NextFloat(1, 3));
                    Main.gore[goreIndex].velocity.X = Main.rand.NextFloat(-height / 50, height / 50);
                    Main.gore[goreIndex].velocity.Y = 5 / (height / 100);
                }
            }
            else
            {
                if (MoveTimer % 30 == 0 && MoveTimer <= 1700)
                {
                    float dist = Vector2.Distance(NPC.Center, Target.Center);                    
                    float time = dist / 16;

                    //Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithFireball>(), fireBallDamage, 0, Main.myPlayer, 0, Target.whoAmI);

                    if (MoveTimer % 60 == 0)
                    {
                        Vector2 lightningCenter = new Vector2(Main.rand.Next(3107, 3350), 1682) * 16;
                        float distance = Vector2.Distance(lightningCenter, Target.Center);
                        Vector2 lightningVector = UsefulFunctions.GenerateTargetingVector(lightningCenter, Target.Center, distance / 30);
                        lightningVector += Target.velocity;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), lightningCenter, lightningVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                    }

                    Vector2 fireballCenter = new Vector2(Main.rand.Next(3107, 3350), 1687) * 16;
                    Vector2 fireballVector = UsefulFunctions.GenerateTargetingVector(fireballCenter, Target.Center, 10);
                    fireballVector += Target.velocity * Main.rand.NextFloat(0, 1);
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), fireballCenter, fireballVector, ModContent.ProjectileType<MarilithFireball>(), fireBallDamage, 0, Main.myPlayer, 0, Target.whoAmI);

                }
            }

            if (MoveTimer >= 1800)
            {
                /*
                for(int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<MarilithFireball>())
                    {
                        Main.projectile[i].Kill();
                    }
                }*/
                NextAttack();
            }
        }

        //Marilith aimes a set of either 5 red or one blue targeting laser near the player, aimed at their predicted future position
        //One second later she fires either a barrage of inferno blasts from the incineration tome if it's red, or a bolt of lightning if it's blue
        private void Barrage()
        {
            MarilithFloat();
            if(MoveTimer % 60 == 0)
            {
                float distance = NPC.Distance(Target.Center);
                Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Target.Center, distance / 30);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                targetVector += Target.velocity;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                targetVector += Target.velocity * 2;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }
            if(MoveTimer >= 600)
            {
                NextAttack();
            }
        }

        //Gusts of wind emerge from Marilith pushing the player toward the walls
        //She fires barrages of Hold Balls at the player, rendering them unable to escape being pushed into the fire
        private void HoldBallStorm()
        {
            if(NPC.Distance(new Vector2(3228, 1731) * 16) > 90 && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                MoveTimer = 0;
                MoveCounter++;
                if(MoveCounter > 15)
                {
                    MoveCounter = 15;
                }
                NPC.velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, new Vector2(3228, 1731) * 16, MoveCounter / 2);
            }
            else
            {
                NPC.velocity *= 0.95f;

                float intensity = MoveTimer / 30f;
                if (MoveTimer > 60)
                {
                    intensity = 1;

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (Main.player[i].active && !Main.player[i].dead)
                        {
                            Main.player[i].AddBuff(ModContent.BuffType<Buffs.MarilithWind>(), 5);

                            if (MoveTimer % 45 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                holdBallDamage = 50;
                                int pattern = Main.rand.Next(4);
                                if (pattern == 0)
                                {
                                    Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[i].Center, 17);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(128, 0).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-128, 0).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                }

                                if (pattern == 1)
                                {
                                    Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[i].Center, 17) + Main.player[i].velocity / 5;
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 80).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -80).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 160).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -160).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 240).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, -240).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                }

                                if (pattern == 2)
                                {
                                    Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[i].Center, 17) + Main.player[i].velocity / 2;
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(128, 128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-128, 128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(128, -128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-128, -128).RotatedBy(targetVector.ToRotation()), targetVector, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                }

                                if (pattern == 3)
                                {
                                    for (int j = 0; j < 12; j++)
                                    {
                                        Vector2 offset = Main.rand.NextVector2CircularEdge(120, 120);
                                        Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(NPC.Center + offset, Main.player[i].Center + Main.rand.NextVector2CircularEdge(500, 500), 14);
                                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + offset, targetVector + Main.player[i].velocity / 2, ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                                    }
                                }
                            }
                        }
                    }
                }


                for (int i = 0; i < 30 * intensity; i++)
                {
                    Vector2 dustVec = Main.rand.NextVector2CircularEdge(300, 300);
                    Vector2 dustVel = new Vector2(Main.rand.NextFloat(0, 17), 0);
                    if (dustVec.X < 0)
                    {
                        dustVel.X *= -1;
                    }
                    Dust.NewDustPerfect(NPC.Center + dustVec, DustID.InfernoFork, dustVel, 0, default, 2).noGravity = true;
                }

                for (int i = 0; i < 5 * intensity; i++)
                {
                    Vector2 dustVec = NPC.Center;
                    dustVec.X += Main.rand.NextFloat(-2000, 2000);
                    dustVec.Y += Main.rand.NextFloat(-1000, 1000);
                    Vector2 dustVel = new Vector2(16, Main.rand.NextFloat(-2, 2));
                    if (dustVec.X < NPC.Center.X)
                    {
                        dustVel.X *= -1;
                    }

                    if (Main.rand.NextBool())
                    {
                        Gore.NewGore(NPC.GetSource_FromThis(), dustVec, dustVel, Main.rand.Next(61, 64), Main.rand.NextFloat(0.5f, 2));
                    }
                    else
                    {
                        Dust.NewDustPerfect(NPC.Center + dustVec, DustID.TintableDust, dustVel, 0, Color.White * 0.8f, Main.rand.NextFloat(0.5f, 3));
                    }
                }
            }
            


            if (MoveTimer > 900)
            {
                NextAttack();
            }
        }

        private void MarilithFloat()
        {
            Vector2 marilithMaxSpeed = new Vector2(6, 4);
            float marilithAccelerationX = 0.1f;
            float marilithAccelerationY = 0.1f;

            if (NPC.Center.X < Target.Center.X)
            {
                NPC.velocity.X += marilithAccelerationX;
            }
            else
            {
                NPC.velocity.X -= marilithAccelerationX;
            }

            //This is the part that makes it bob up and down as it moves
            //If it's moving up
            if (NPC.velocity.Y < 0)
            {
                //And it's not more than 120 units above the player
                if (Target.Center.Y - NPC.Center.Y <= 120)
                {
                    //Keep moving up
                    NPC.velocity.Y -= marilithAccelerationY;
                }
                //If we are more than 120 units above the player, start accelerating down
                else
                {
                    NPC.velocity.Y += marilithAccelerationY;
                }
            }
            else
            {
                //Do the same thing, but reversed if it's moving down. Could probably simplify this, but this format makes it clear what it's doing.
                if (Target.Center.Y - NPC.Center.Y <= -120)
                {
                    NPC.velocity.Y -= marilithAccelerationY;
                }
                else
                {
                    NPC.velocity.Y += marilithAccelerationY;
                }
            }

            NPC.velocity = Vector2.Clamp(NPC.velocity, -marilithMaxSpeed, marilithMaxSpeed);


        }

        private void NextAttack()
        {
            MoveIndex++;
            if (MoveIndex > MoveList.Count)
            {
                MoveIndex = 0;
            }

            MoveTimer = 0;
            MoveCounter = 0;
        }

        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<MarilithMove> {
                new MarilithMove(ExpandingFireBombs, MarilithAttackID.Firebombs, "ExpandingFireBombs"),
                new MarilithMove(VolcanicStorm, MarilithAttackID.Stormclouds, "VolcanicStorm"),
                new MarilithMove(HoldBallStorm, MarilithAttackID.Flamewalls, "HoldBallStorm"),
                new MarilithMove(Barrage, MarilithAttackID.Barrage, "Barrage"),
                };
        }

        private void InitializeFirewalls()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                //3111, 1682 Top left
                //3346, 1682 Top right
                //3346, 1781 Bottom right
                //3111, 1781 Bottom left

                //Left firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(3111, 1731) * 16, Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 0, 100);
                //Right firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(3346, 1731) * 16, Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 1, 100);
                //Top firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(3228, 1682) * 16, Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 2, 237);
                //Bottom firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), new Vector2(3228, 1784) * 16, Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 3, 237);
            }
            else
            {
                //TODO: Actually configure these correctly lol
                //Left firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(-1000, 0), Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 0, 100);
                //Right firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(1000, 0), Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 1, 100);
                //Top firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 800), Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 2, 237);
                //Bottom firewall
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 800), Vector2.Zero, ModContent.ProjectileType<MarilithFirewall>(), 15, 0, Main.myPlayer, 3, 237);
            }
        }

        private class MarilithAttackID
        {
            public const short Firebombs = 0;
            public const short Barrage = 1;
            public const short Stormclouds = 2;
            public const short Flamewalls = 3;
        }
        private class MarilithMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public MarilithMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }
        public static Texture2D texture;
        public static ArmorShaderData data;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)        
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);


            //Apply the shader, caching it as well
            if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/MarilithFireAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "MarilithFireAuraPass");
            }


            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }


            //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

            //Pass the fire flow direction parameter in through the "color" variable, because there isn't a "direction" one
            data.UseColor(NPC.Center.X, NPC.Center.Y, 0);
            data.UseSaturation(introTimer / 120f);
            //Apply the shader
            data.Apply(null);

            Rectangle recsize = new Rectangle(0, 0, texture.Width, texture.Height);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(texture, NPC.Center - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * 2.5f, recsize, Color.White, 0, Vector2.Zero, 2.5f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);


            return true;
        }

        public override void FindFrame(int currentFrame)
        {
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
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.MarilithBag>()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 8").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 9").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.LargeSapphire);
                }
                if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
                }
            }            
        }
    }
}