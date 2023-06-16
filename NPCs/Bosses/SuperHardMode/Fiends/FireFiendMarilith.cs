using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Projectiles.Enemy.Marilith;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends
{
    [AutoloadBossHead]
    class FireFiendMarilith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused,
                    BuffID.OnFire,
                    BuffID.OnFire3
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 120;
            NPC.height = 160;
            NPC.damage = 39;
            NPC.defense = 50;
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
            despawnHandler = new NPCDespawnHandler(LanguageUtils.GetTextValue("NPCs.FireFiendMarilith.DespawnHandler"), Color.OrangeRed, DustID.FireworkFountain_Red);
        }
        int holdBallDamage = 50;
        int fireBallDamage = 45;
        int lightningDamage = 55;
        int fireStormDamage = 50;
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
        public float introTimer = 0;
        bool displayedWarning = false;
        public override void AI()
        {            
            if(introTimer < 120)
            {
                introTimer++;
            }

            if (despawnHandler.TargetAndDespawn(NPC.whoAmI))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1.3f, Pitch = 0.9f }, NPC.Center);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CataclysmicFirestorm>(), 55, 0.5f, Main.myPlayer);
                }
            }

            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.8f, 0f, 0.2f);
            MoveTimer++;
            
            if (MoveList == null)
            {
                InitializeMoves();
                InitializeFirewalls();                
            }

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveIndex >= MoveList.Count)
            {
                MoveIndex = 0;
            }

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
                            UsefulFunctions.BroadcastText(LanguageUtils.GetTextValue("NPCs.FireFiendMarilith.Abyss"), Color.Purple);
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
                    if (MoveTimer % 60 == 0)
                    {
                        Vector2 lightningCenter = new Vector2(Main.rand.Next(3107, 3350), 1682) * 16;
                        float distance = Vector2.Distance(lightningCenter, Target.Center);
                        Vector2 lightningVector = UsefulFunctions.Aim(lightningCenter, Target.Center, distance / 30);
                        lightningVector += Target.velocity;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), lightningCenter, lightningVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                    }

                    Vector2 fireballCenter = new Vector2(Main.rand.Next(3107, 3350), 1687) * 16;
                    Vector2 fireballVector = UsefulFunctions.Aim(fireballCenter, Target.Center, 10);
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

        Vector2 targetVector;
        private void Barrage()
        {
            MarilithFloat();
            if(MoveTimer % 60 == 0)
            {
                float distance = NPC.Distance(Target.Center);
                targetVector = UsefulFunctions.Aim(NPC.Center, Target.Center, distance / 30);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                
                

                targetVector = Main.rand.NextVector2Circular(1, 1);
                targetVector.Normalize();
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector.RotatedBy(MathHelper.PiOver2), ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector.RotatedBy(MathHelper.Pi), ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                //Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector.RotatedBy(MathHelper.PiOver2 * 3), ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }
            if(MoveTimer % 60 == 1)
            {
                targetVector += Target.velocity;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }
            if (MoveTimer % 60 == 2)
            {
                targetVector += Target.velocity * 2;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, targetVector, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }

            Vector2 randomVel = Main.rand.NextVector2Circular(1, 1);
            if (MoveTimer % 6 == 5)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + randomVel * 8, randomVel, ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
            }

            if (MoveTimer >= 900)
            {
                NextAttack();
            }
        }

        //Gusts of wind emerge from Marilith pushing the player toward the walls
        //She fires barrages of Hold Balls at the player, rendering them unable to escape being pushed into the fire
        float[] angles;
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
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, new Vector2(3228, 1731) * 16, MoveCounter / 2);
            }
            else
            {
                NPC.velocity *= 0.95f;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        Main.player[i].AddBuff(ModContent.BuffType<Buffs.MarilithWind>(), 5);

                    }
                }

                float intensity = MoveTimer / 60f;
                if (intensity > 1)
                {
                    intensity = 1;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    
                    if (MoveTimer >= 60)
                    {
                        if (MoveTimer % 4 == 0)
                        {
                            if (MoveTimer == 60)
                            {
                                float offset = MathHelper.Pi / 3f;
                                angles = new float[6];
                                angles[0] = MathHelper.PiOver4;
                                angles[1] = angles[0] + offset;
                                angles[2] = angles[1] + offset;
                                angles[3] = angles[2] + offset;
                                angles[4] = angles[3] + offset;
                                angles[5] = angles[4] + offset;
                            }

                            for (int i = 0; i < angles.Length; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    angles[i] += (MoveTimer - 60f) / 1200f;
                                }
                                else
                                {
                                    angles[i] -= (MoveTimer - 60f) / 1200f;
                                }
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(7, 0).RotatedBy(angles[i]), ModContent.ProjectileType<MarilithHoldBall>(), holdBallDamage, 0.5f, Main.myPlayer);
                            }

                        }
                    }

                    if (MoveTimer % 120 == 0)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 1), ModContent.ProjectileType<MarilithLightning>(), lightningDamage, 0, Main.myPlayer, 1, NPC.whoAmI);
                    }
                }

                for (int i = 0; i < 30 * intensity; i++)
                {
                    Vector2 dustVec = Main.rand.NextVector2CircularEdge(280, 280);
                    Vector2 dustVel = dustVec;
                    dustVel.Normalize();
                    dustVel *= 12 * intensity;
                    Dust.NewDustPerfect(NPC.Center + dustVec, 57, dustVel, 0, default, 2).noGravity = true;
                }
            }           


            if (MoveTimer > 960)
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
            NPC.lifeMax = NPC.lifeMax * Main.CurrentFrameFlags.ActivePlayersCount;
            NPC.life = NPC.lifeMax;

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<MarilithAura>(), 0, 0.5f, Main.myPlayer);

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                //3111, 1682 Top left5
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
        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.MarilithBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeSapphire));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<FairyInABottle>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ForgottenRisingSun>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarrowBlade>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Ice3Tome>()));
            npcLoot.Add(notExpertCondition);
        }
        public override void OnKill()
        {           
            if(Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPCDirect(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<MarilithDeath>(), ai0: NPC.velocity.X, ai1: NPC.velocity.Y);
            }
        }
    }
}