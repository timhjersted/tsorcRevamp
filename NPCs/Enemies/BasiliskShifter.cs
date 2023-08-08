using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies
{
    class BasiliskShifter : ModNPC
    {
        //HARD MODE VARIANT 
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 12;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 2;
            AnimationType = 28;
            NPC.aiStyle = 3;
            NPC.damage = 30;
            NPC.defense = 55;
            NPC.height = 54;
            NPC.width = 54;
            NPC.lifeMax = 350; 
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 1750; // health / 2 : was 233
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BasiliskShifterBanner>();
        }

        float breathTimer = 60;
       

        float shotTimer;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;
        int cursedBreathDamage = 13;
        int darkExplosionDamage = 18;
        int hypnoticDisruptorDamage = 18;
        int bioSpitDamage = 18;


        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = P.ZoneOverworldHeight;
            bool InBrownLayer = P.ZoneDirtLayerHeight;
            bool InGrayLayer = P.ZoneRockLayerHeight;
            bool InHell = P.ZoneUnderworldHeight;
            bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
            // P.townNPCs > 0f // is no town NPCs nearby

            //Ensuring it can't spawn if two already exists.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 1)
                    {
                        return 0;
                    }
                }
            }

            if (spawnInfo.Water) return 0f;

            //SPAWNS IN HM JUNGLE AT NIGHT ABOVE GROUND AFTER THE RAGE IS DEFEATED
            if (Main.hardMode && Jungle && !Corruption && !Main.dayTime && AboveEarth && !Ocean && P.townNPCs <= 0f && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())) && Main.rand.NextBool(30)) return 1;

            //SPAWNS IN HM METEOR UNDERGROUND AT NIGHT
            if (Main.hardMode && Meteor && !Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(10)) return 1;

            if (Main.hardMode && Meteor && Main.dayTime && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(20)) return 1;

            //SPAWNS AGAIN IN CORRUPTION AND NOW CRIMSON
            if (Main.hardMode && Corruption && !Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(20)) return 1;

            if (Main.hardMode && Corruption && Main.dayTime && !Ocean && (InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.rand.NextBool(30)) return 1;

            //SPAWNS IN DUNGEON AT NIGHT RARELY
            if (Main.hardMode && Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.NextBool(45)) return 1;

            //SPAWNS IN HM HALLOW RARELY
            if (Main.hardMode && (InBrownLayer || InGrayLayer) && Hallow && !Ocean && !spawnInfo.Water && Main.rand.NextBool(45)) return 1;

            //SPAWNS RARELY IN HM JUNGLE UNDERGROUND
            if (Main.hardMode && Jungle && InGrayLayer && !Ocean && !spawnInfo.Water && Main.rand.NextBool(60)) return 1;

            //BLOODMOON HIGH SPAWN IN METEOR OR JUNGLE
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && (Meteor || Jungle) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && !spawnInfo.Water && Main.bloodMoon && Main.rand.NextBool(5)) return 1;

            return 0;
        }
        #endregion

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            tsorcRevampAIs.FighterAI(NPC, 1, 0.03f, canTeleport: false, randomSound: SoundID.Mummy, soundFrequency: 1000, enragePercent: 0.5f, enrageTopSpeed: 2);

            shotTimer++;

            if (shotTimer >= 85)
            {
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 1f);
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemEmerald, NPC.velocity.X, NPC.velocity.Y);
                }


                if (shotTimer >= 100f)
                {
                    NPC.TargetClosest(true);
                    //DISRUPTOR ATTACK
                    Player player3 = Main.player[NPC.target];
                    if (Main.rand.NextBool(200) && NPC.Distance(player3.Center) > 190)
                    {
                        Vector2 projectileVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 4f, 1.06f, true, true);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projectileVelocity, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 5f, Main.myPlayer);
                        //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f }, player.Center); //wobble
                                                                                                                            //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        shotTimer = 1f;

                        NPC.netUpdate = true;
                    }

                    //CHANCE TO JUMP BEFORE ATTACK
                    //FOR MAIN
                    if (shotTimer == 105 && Main.rand.NextBool(3) && NPC.life >= NPC.lifeMax / 2)
                    {
                        
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, -4f);
                    }
                    //FOR FINAL
                    if (shotTimer >= 185 && Main.rand.NextBool(2) && NPC.life <= NPC.lifeMax / 2)
                    {
                        NPC.velocity.Y = Main.rand.NextFloat(-10f, 3f);
                    }


                }

            }


            // NEW BREATH ATTACK 
            breathTimer++;
            if (breathTimer > 480 && Main.rand.NextBool(2) && shotTimer <= 99f && NPC.life >= NPC.lifeMax / 2)
            {
                breathTimer = -60;
                shotTimer = -60f;
            }

            if (breathTimer < 0)
            {
                NPC.velocity.X = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 9);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreathCollides>(), cursedBreathDamage, 0f, Main.myPlayer);
                    NPC.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                }
            }

            if (breathTimer > 360 && NPC.life >= NPC.lifeMax / 2)
            {
                shotTimer = -60f;
                UsefulFunctions.DustRing(NPC.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 5);
            }

            if (breathTimer == 0)
            {
                shotTimer = 1f;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }

            int choice = Main.rand.Next(4);
            //PURPLE MAGIC LOB ATTACK; && Main.rand.NextBool(2)
            if (shotTimer >= 110f && NPC.life >= NPC.lifeMax / 2 && choice <= 1)
            {
                bool clearSpace = true;
                for (int i = 0; i < 15; i++)
                {
                    if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                    {
                        clearSpace = false;
                    }
                }
                if (clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);

                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, bioSpitDamage, 0f, Main.myPlayer);                       
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                    }

                    if (shotTimer >= 154f)
                    {
                        shotTimer = 1f;
                    }
                }
            }
            //NORMAL SPIT ATTACK
            if (shotTimer >= 115f && NPC.life >= NPC.lifeMax / 2 && choice >= 2)
            {
                if (Collision.CanHitLine(NPC.Center, 0, 0, Main.player[NPC.target].Center, 0, 0))
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9);

                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int num555 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
                        Main.projectile[num555].timeLeft = 300; //40
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        shotTimer = 1f;
                    }
                }
            }
            //FINAL DESPERATE ATTACK
            if (shotTimer >= 175f && Main.rand.NextBool(2) && NPC.life <= NPC.lifeMax / 2)
            {
                int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, 150, Color.Blue, 1f);
                Main.dust[dust2].noGravity = true;

                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 10);

                if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 0f, Main.myPlayer);
                    //Terraria.Audio.SoundEngine.PlaySound(4, (int)npc.position.X, (int)npc.position.Y, 9);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.1f }, NPC.Center);
                    //customAi1 = 1f;
                }
                if (shotTimer >= 206f)
                {
                    shotTimer = 1f;
                }
            }
            //KNOCKBACK CONDITIONAL
            if (NPC.life >= NPC.lifeMax / 2)
            {
                NPC.knockBackResist = 0.04f;
            }
            else
            {
                NPC.knockBackResist = 0.0f;
            }
            //MAKE SOUND WHEN JUMPING/HOVERING
            if (Main.rand.NextBool(12) && NPC.velocity.Y <= -1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.2f, Pitch = 0.1f }, NPC.Center);
            }
            //TELEGRAPH DUSTS
            if (shotTimer >= 100)
            {
                Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
                    //Dust.NewDust(npc.position, npc.width, npc.height, DustID.GemEmerald, npc.velocity.X, npc.velocity.Y);
                }
            }
            //reset attack timer when hit in melee range
            if (NPC.justHit && NPC.Distance(player.Center) < 100 && NPC.life >= NPC.lifeMax / 2)
            {
                shotTimer = 0f;
            }
            //jump back when hit at close range
            if (NPC.justHit && NPC.Distance(player.Center) < 150 && Main.rand.NextBool(3))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-6f, -4f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-7f, -4f);
                shotTimer = 50f;
                NPC.netUpdate = true;
            }
            //jump forward when hit at range
            if (NPC.justHit && NPC.Distance(player.Center) > 150 && Main.rand.NextBool(2))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-10f, -3f);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(7f, 3f);
                NPC.netUpdate = true;
            }
            //Shift toward the player randomly
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.NextBool(80) && NPC.Distance(player.Center) > 200)
                {
                    Lighting.AddLight(NPC.Center, Color.Red.ToVector3() * 3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
                    NPC.damage = 35;
                    chargeDamage++;
                }
                if (chargeDamage >= 70)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 30;
                    chargeDamage = 0;
                }
            }
        }

        #region Find Frame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.Y == 0f)
            {
                if (NPC.direction == 1)
                {
                    NPC.spriteDirection = 1;
                }
                if (NPC.direction == -1)
                {
                    NPC.spriteDirection = -1;
                }
                if (NPC.velocity.X == 0f)
                {
                    NPC.frame.Y = 0;
                    NPC.frameCounter = 0.0;
                }
                else
                {
                    NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * 2f);
                    NPC.frameCounter += 1.0;
                    if (NPC.frameCounter > 6.0)
                    {
                        NPC.frame.Y = NPC.frame.Y + num;
                        NPC.frameCounter = 0.0;
                    }
                    if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
                    {
                        NPC.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y = num;
                NPC.frame.Y = 0;
            }
        }

        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false); //-20 life if counter hits 100
            target.AddBuff(BuffID.Poisoned, 10 * 60, false);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 10 * 60, false);
            }
            if (Main.rand.NextBool(4))
            {
                target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 300 * 60, false);   
            }
        }
        #endregion

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Parasite Zombie Gore 3").Type, 1.1f);
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BloodredMossClump>(), 2, 1, 2));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight, 3));
            npcLoot.Add(hmCondition);
        }
    }
}