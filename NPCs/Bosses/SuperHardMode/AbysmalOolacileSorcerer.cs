using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class AbysmalOolacileSorcerer : ModNPC
    {
        int darkBeadDamage = 46;
        int darkOrbDamage = 49;
        int seekerDamage = 40;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned,
                    BuffID.OnFire,
                    BuffID.Confused
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 110;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 83400;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.boss = true;
            NPC.scale = 1.1f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 430000;
            NPC.width = 28;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.DespawnHandler"), Color.DarkRed, DustID.Firework_Red);
        }
        public float DarkBeadShotTimer
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public float TeleportTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        public float DarkBeadShotCounter
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        public float SecondAttackCounter
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        float NPCSpawningTimer;
        float NPCSpawningTimer2;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            bool Meteor = P.ZoneMeteor;
            bool Jungle = P.ZoneJungle;
            bool Dungeon = P.ZoneDungeon;
            bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
            bool Hallow = P.ZoneHallow;
            bool AboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool InBrownLayer = spawnInfo.SpawnTileY >= Main.worldSurface && spawnInfo.SpawnTileY < Main.rockLayer;
            bool InGrayLayer = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < (Main.maxTilesY - 200) * 16;
            bool InHell = spawnInfo.SpawnTileY >= (Main.maxTilesY - 200) * 16;
            bool Ocean = spawnInfo.SpawnTileX < 3600 || spawnInfo.SpawnTileX > (Main.maxTilesX - 100) * 16;

            // these are all the regular stuff you get , now lets see......

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && AboveEarth && Main.rand.NextBool(200)) return 1;

            if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && InBrownLayer && Main.rand.NextBool(500)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && AboveEarth && Main.rand.NextBool(50)) return 1;

            if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())) && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())) && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())) && AboveEarth && Main.rand.NextBool(2850)) return 1;

            return 0;
        }
        #endregion


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            DarkBeadShotTimer++; //Counts up each tick. Used to space out shots
            TeleportTimer++; //When this hits 200 (120 if low health) the boss teleports
            SecondAttackCounter++; //When this hits 60 the boss has will begin randomly deciding whether to fire extra projectiles.

            if (NPC.life > NPC.lifeMax / 4)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 210, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Black, 3f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnNPCs();
                FireProjectiles();
            }

            if (TeleportTimer >= 20) //How long it should float after teleporting before coming to a stop
            {
                NPC.velocity.X *= 0.27f;
                NPC.velocity.Y *= 0.17f;
            }

            OolacileTeleport();
        }

        public void SpawnNPCs()
        {
            NPCSpawningTimer++;
            NPCSpawningTimer2++;
            //NPCSpawningTimer += (Main.rand.Next(2, 5) * 0.1f);
            if (NPCSpawningTimer >= 10f)
            {
                if ((NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.BarrowWightPhantom>()) < 4) && Main.rand.NextBool(250))
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightPhantom>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);

                    }
                }
                if ((NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.BarrowWightNemesis>()) < 2) && Main.rand.NextBool(2000))
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightNemesis>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    NPCSpawningTimer = 0;
                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
                
            }
            if (NPCSpawningTimer2 >= 5000f)
            {
                if (NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.TaurusKnight>()) < 1)
                {
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.TaurusKnight>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    NPCSpawningTimer2 = 0;
                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }
            }
        }

        public void FireProjectiles()
        {
            if (DarkBeadShotTimer >= 12 && DarkBeadShotCounter < 5)
            {
                Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 7);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkBead>(), darkBeadDamage, 0f, Main.myPlayer);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                DarkBeadShotTimer = 0;
                DarkBeadShotCounter++;
            }

            if (SecondAttackCounter >= 60)
            {
                if (Main.rand.NextBool(20))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 2);
                    projVelocity.Y -= 520;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkOrb>(), darkOrbDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24, NPC.Center);
                    NPCSpawningTimer = 1f;
                    SecondAttackCounter = 0;
                }

                if (Main.rand.NextBool(16))
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 8);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileSeeker>(), seekerDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPCSpawningTimer = 1f;
                }
            }
        }

        public void OolacileTeleport()
        {
            //bool clearLineofSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);

            if ((TeleportTimer >= 200 && NPC.life > NPC.lifeMax / 4) || (TeleportTimer >= 120 && NPC.life <= NPC.lifeMax / 4))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 27, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Purple, 1f);
                    Main.dust[dust].noGravity = true;
                }
                DarkBeadShotCounter = 0;
                TeleportTimer = 0;

                //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                int x_blockpos = (int)NPC.position.X / 16; // corner not center
                int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                int tp_counter = 0;
                bool endLoop = false;
                if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 9000000f)
                { // far away from target; 4000 pixels = 250 blocks
                    tp_counter = 100;
                    endLoop = false; // always telleport was true for no teleport
                }
                while (!endLoop) // loop always ran full 100 time before I added "flag7 = true" below
                {
                    if (tp_counter >= 100) // run 100 times
                        break; //return;
                    tp_counter++;

                    int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                    int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 62, (target_y_blockpos + tp_radius) - 26);  //  pick random tp point (centered on corner)
                    for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                    { // (tp_x_target,m) is block under its feet I think
                        if ((m < target_y_blockpos - 21 || m > target_y_blockpos + 21 || tp_x_target < target_x_blockpos - 21 || tp_x_target > target_x_blockpos + 21) && (m < y_blockpos - 8 || m > y_blockpos + 8 || tp_x_target < x_blockpos - 8 || tp_x_target > x_blockpos + 8) && !Main.tile[tp_x_target, m].HasTile)
                        { // over 21 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                            bool safe_to_stand = true;
                            bool dark_caster = false; // not a fighter type AI...
                            if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                safe_to_stand = false;
                            else if (Main.tile[tp_x_target, m - 1].LiquidType == LiquidID.Lava) // feet submerged in lava
                                safe_to_stand = false;

                            if (safe_to_stand && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                            { //  3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
                              // safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && // removed safe enviornment && solid below feet

                                NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet			
                                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 5 + (NPC.height / 2));
                                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                NPC.velocity.X = (float)(Math.Cos(rotation) * 1) * -1;
                                NPC.velocity.Y = (float)(Math.Sin(rotation) * 1) * -1;

                                NPC.netUpdate = true;

                                //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                endLoop = true; // end the loop (after testing every lower point :/)
                                TeleportTimer = 0;
                            }
                        } // END over 17 blocks distant from player...
                    } // END traverse y down to edge of radius
                } // END try 100 times
            }
        }

        #endregion
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
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.OolacileSorcererBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 2, 4));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HealingElixir>(), 1, 5, 10));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 3, 6));
            npcLoot.Add(notExpertCondition);
        }

        public override void FindFrame(int currentFrame)
        {
            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -1 && NPC.velocity.X < 1) && (NPC.velocity.Y > -1 && NPC.velocity.Y < 1))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        #region Gore
        public override void OnKill()
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.AbysmalOolacileSorcerer.Defeated"), 150, 150, 150);
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Oolacile Sorcerer Gore 3").Type, 1f);
            }
        }
        #endregion
    }
}