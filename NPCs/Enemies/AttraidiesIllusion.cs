using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using Terraria.GameContent.ItemDropRules;
using tsorcRevamp.Items.Potions;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Enemies
{
    class AttraidiesIllusion : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GoblinSorcerer];
            NPCDebuffImmunityData debuffData = new NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[] {
                    BuffID.Poisoned,
                    BuffID.OnFire,
                    BuffID.Confused
            //npc.buffImmune[BuffID.Paralyzed] = true; ???
                }
            };
            NPCID.Sets.DebuffImmunitySets.Add(Type, debuffData);
        }

        public override void SetDefaults()
        {
            AnimationType = NPCID.GoblinSorcerer;
            NPC.npcSlots = 5;
            NPC.lifeMax = 400;
            NPC.damage = 0;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.3f;
            NPC.value = 6000;
            NPC.defense = 10;
            NPC.height = 44;
            NPC.width = 28;
            NPC.HitSound = SoundID.NPCHit48;
            NPC.DeathSound = SoundID.NPCDeath58;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.AttraidiesIllusionBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.ZoneDungeon && NPC.CountNPCS(ModContent.NPCType<AttraidiesIllusion>()) < 1 && NPC.CountNPCS(ModContent.NPCType<AttraidiesManifestation>()) < 1
                && NPC.CountNPCS(ModContent.NPCType<JungleWyvernJuvenile.JungleWyvernJuvenileHead>()) < 1 && NPC.CountNPCS(ModContent.NPCType<DungeonMage>()) < 1)
            {
                if (!Main.hardMode) { chance = .02f; }
                else chance = .011f;

            }

            if (spawnInfo.Player.ZoneUnderworldHeight && !Main.hardMode && !NPC.AnyNPCs(ModContent.NPCType<AttraidiesIllusion>()))
            {
                chance = .033f;
            }

            if (spawnInfo.Player.ZoneJungle && Main.hardMode)
            {
                chance = .00625f;
            }

            return chance;
        }


        #region AI

        int illusiontimer = 0;
        public override void AI()
        {

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (NPC.life > NPC.lifeMax / 4 * 3)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 120, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= NPC.lifeMax / 10 * 4)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 180, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }

            if (NPC.life < NPC.lifeMax * .66f && NPC.life > NPC.lifeMax * .33f)
            {
                illusiontimer++;
                if (illusiontimer > 40 && illusiontimer < 100)
                {
                    NPC.alpha += 1;
                }
                if (illusiontimer == 100)
                {
                    NPC.alpha = 50;
                    illusiontimer = 0;
                }
            }


            if (NPC.life <= NPC.lifeMax * .33f)
            {
                illusiontimer++;
                if (illusiontimer == 0)
                {
                    NPC.alpha = 200;
                }
                if (illusiontimer < 30)
                {
                    NPC.alpha -= 4;
                }
                if (illusiontimer >= 51 && illusiontimer <= 61)
                {
                    NPC.alpha = 150;
                }
                if (illusiontimer >= 62 && illusiontimer <= 66)
                {
                    NPC.alpha = 40;
                }
                if (illusiontimer >= 67 && illusiontimer <= 75)
                {
                    NPC.alpha = 200;
                }
                if (illusiontimer >= 76 && illusiontimer <= 110)
                {
                    NPC.alpha = 150;
                }
                if (illusiontimer == 111)
                {
                    illusiontimer = 0;
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                if (NPC.ai[0] >= 12 && NPC.ai[2] < 5) //2 was 12
                {
                    float num48 = 6f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20) / 2; //was 10/5
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30) / 2;
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)System.Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 7;
                        int type = ModContent.ProjectileType<TheOracle>();
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 150;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        NPC.ai[0] = 0;
                        NPC.ai[2]++;
                    }
                }
                if (!Main.dedServ && (Main.rand.NextBool(360)))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh2") with { Volume = 1.1f });
                }
            }

            if (NPC.ai[1] >= 35)
            {
                NPC.velocity.X *= 0.37f;
                NPC.velocity.Y *= 0.17f;
            }

            if ((NPC.ai[1] >= 280 && NPC.life > NPC.lifeMax / 4 * 3) || (NPC.ai[1] >= 200 && NPC.life <= NPC.lifeMax / 4 * 3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 55, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = true;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                //npc.ai[1] = 0;
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                {
                    NPC.TargetClosest(true);
                }
                if (Main.player[NPC.target].dead)
                {
                    NPC.position.X = 0;
                    NPC.position.Y = 0;
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                        return;
                    }
                }
                else
                {



                    //end of W1k's Death code


                    //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                    int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                    int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                    int x_blockpos = (int)NPC.position.X / 16; // corner not center
                    int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                    int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 9000000f)
                    { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always telleport was true for no teleport
                    }
                    while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
                    {
                        if (tp_counter >= 100) // run 100 times
                            break; //return;
                        tp_counter++;

                        int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 62, (target_y_blockpos + tp_radius) - 24);  //  pick random tp point (centered on corner)
                        for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                        { // (tp_x_target,m) is block under its feet I think
                            if ((m < target_y_blockpos - 15 || m > target_y_blockpos + 15 || tp_x_target < target_x_blockpos - 15 || tp_x_target > target_x_blockpos + 15) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].HasTile)
                            { // over 13 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].LiquidType == LiquidID.Lava) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].TileType] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                                { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile

                                    NPC.TargetClosest(true);
                                    NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                    NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet
                                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                    NPC.velocity.X = (float)(Math.Cos(rotation) * 6) * -1;
                                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 6) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    NPC.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    NPC.ai[1] = 0;
                                }
                            } // END over 13 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }



            //beginning of Omnir's Ultima Weapon projectile code


            NPC.ai[3]++;

            if (NPC.ai[3] >= 60) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.NextBool(4)) //1 in 4 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 2f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 620 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 12;
                        int type = ModContent.ProjectileType<ScrewAttack>();
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 540;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                        NPC.ai[3] = 0; ;
                    }
                }

                if (Main.rand.NextBool(45)) //1 in 45 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, NPCID.JungleBat, 0);
                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, NPCID.JungleBat, 0);
                    Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    NPC.active = true;

                }
            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 600)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }

        }

        #endregion


        public override void FindFrame(int frameHeight)
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
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
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


        public override void OnKill()
        {
            for (int j = 0; j < 30; j++)
            {
                int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 16, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 200, Color.Salmon, 2.5f);
                Main.dust[dust].noGravity = true;
            }

            Player player = Main.player[NPC.target];
            UsefulFunctions.BroadcastText("The Attraidies Illusion has been vanquished...", 190, 140, 150);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot) 
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 55));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 35, 1, 1, 4));
            npcLoot.Add(new CommonDrop(ItemID.MagicPowerPotion, 35, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 75));
            npcLoot.Add(new CommonDrop(ItemID.IronskinPotion, 35, 1, 1, 2));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 30, 1, 1, 9));
            npcLoot.Add(new CommonDrop(ItemID.GoldenKey, 10, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Magic.AquamarineRing>(), 20));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 10, 2, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HealingElixir>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 6));
        }
    }
}