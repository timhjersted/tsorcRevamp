using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    class AttraidiesIllusion : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Attraidies Illusion");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.GoblinSorcerer];
        }

        public override void SetDefaults()
        {
            animationType = NPCID.GoblinSorcerer;
            npc.npcSlots = 50;
            npc.lifeMax = 400;
            npc.damage = 13;
            npc.scale = 1f;
            npc.knockBackResist = 0.3f;
            npc.value = 6000;
            npc.defense = 10;
            npc.height = 44;
            npc.width = 28;
            npc.HitSound = SoundID.NPCHit48;
            npc.DeathSound = SoundID.NPCDeath58;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            //npc.buffImmune[BuffID.Paralyzed] = true; ???
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.AttraidiesIllusionBanner>();


        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.player.ZoneDungeon && !NPC.AnyNPCs(mod.NPCType("AttraidiesIllusion")))
            {
                if (!Main.hardMode) { chance = .02f; }
                else chance = .011f;
                
            }

            if (spawnInfo.player.ZoneUnderworldHeight && !Main.hardMode && !NPC.AnyNPCs(mod.NPCType("AttraidiesIllusion"))) {
                chance = .033f;
            }

            if (spawnInfo.player.ZoneJungle && Main.hardMode) {
                chance = .00625f;
            }

            return chance;
        }


        #region AI

        int illusiontimer = 0;
        public override void AI()
        {

            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (npc.life > 300)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X, npc.velocity.Y, 120, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 300)
            {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 180, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }

            if (npc.life < npc.lifeMax * .66f  && npc.life > npc.lifeMax * .33f)
            {
                illusiontimer++;
                if (illusiontimer > 40 && illusiontimer < 100)
                {
                    npc.alpha += 1;
                }
                if (illusiontimer == 100)
                {
                    npc.alpha = 50;
                    illusiontimer = 0;
                }
            }


            if (npc.life <= npc.lifeMax * .33f)
            {
                illusiontimer++;
                if (illusiontimer == 0)
                {
                    npc.alpha = 200;
                }
                if (illusiontimer < 30)
                {
                    npc.alpha -= 4;
                }
                if (illusiontimer >= 51 && illusiontimer <= 61)
                {
                    npc.alpha = 150;
                }
                if (illusiontimer >= 62 && illusiontimer <= 66)
                {
                    npc.alpha = 40;
                }
                if (illusiontimer >= 67 && illusiontimer <= 75)
                {
                    npc.alpha = 200;
                }
                if (illusiontimer >= 76 && illusiontimer <= 110)
                {
                    npc.alpha = 150;
                }
                if (illusiontimer == 111)
                {
                    illusiontimer = 0;
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                if (npc.ai[0] >= 12 && npc.ai[2] < 5) //2 was 12
                {
                    float num48 = 6f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 20) / 2; //was 10/5
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 30) / 2;
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                    {
                        float num51 = (float)System.Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 7;
                        int type = ModContent.ProjectileType<TheOracle>();
                        int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 150;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
                        npc.ai[0] = 0;
                        npc.ai[2]++;
                    }
                }
                if (!Main.dedServ && (Main.rand.Next(360) == 0)) Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EvilLaugh2").WithVolume(1.1f), npc.Center);
            }

            if (npc.ai[1] >= 35)
            {
                npc.velocity.X *= 0.37f;
                npc.velocity.Y *= 0.17f;
            }

            if ((npc.ai[1] >= 280 && npc.life > 300) || (npc.ai[1] >= 140 && npc.life <= 300))
            {
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 55, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = true;
                }
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                npc.ai[2] = 0;
                //npc.ai[1] = 0;
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.TargetClosest(true);
                }
                if (Main.player[npc.target].dead)
                {
                    npc.position.X = 0;
                    npc.position.Y = 0;
                    if (npc.timeLeft > 10)
                    {
                        npc.timeLeft = 10;
                        return;
                    }
                }
                else
                {



                    //end of W1k's Death code


                    //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                    int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
                    int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
                    int x_blockpos = (int)npc.position.X / 16; // corner not center
                    int y_blockpos = (int)npc.position.Y / 16; // corner not center
                    int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 9000000f)
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
                            if ((m < target_y_blockpos - 15 || m > target_y_blockpos + 15 || tp_x_target < target_x_blockpos - 15 || tp_x_target > target_x_blockpos + 15) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].active()) { // over 13 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                                { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile

                                    npc.TargetClosest(true);
                                    npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                                    npc.velocity.X = (float)(Math.Cos(rotation) * 6) * -1;
                                    npc.velocity.Y = (float)(Math.Sin(rotation) * 6) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    npc.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    npc.ai[1] = 0;
                                }
                            } // END over 13 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }



            //beginning of Omnir's Ultima Weapon projectile code


            npc.ai[3]++;

            if (npc.ai[3] >= 60) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.Next(4) == 0) //1 in 4 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 2f;
                    Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 620 + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 12;
                        int type = ModContent.ProjectileType<ScrewAttack>();
                        int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 540;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                        npc.ai[3] = 0; ;
                    }
                }

                if (Main.rand.Next(45) == 0) //1 in 45 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X - 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.CursedSkull, 0);
                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 636 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.CursedSkull, 0);
                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;

                }
            }

            npc.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 600)
            {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }

        }

        #endregion


        public override void FindFrame(int frameHeight)
        {

            if ((npc.velocity.X > -9 && npc.velocity.X < 9) && (npc.velocity.Y > -9 && npc.velocity.Y < 9))
            {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
                if (npc.position.X > Main.player[npc.target].position.X)
                {
                    npc.spriteDirection = -1;
                }
                else
                {
                    npc.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2))
            {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
            }
            else
            {
                npc.frameCounter += 1.0;
            }
            if (npc.frameCounter >= 1.0)
            {
                npc.frame.Y = npc.frame.Y + num;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
            {
                npc.frame.Y = 0;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life <= 0)
            {
                for (int j = 0; j < 50; j++)
                {
                    int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 16, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 200, Color.Salmon, 2.5f);
                    Main.dust[dust].noGravity = true;
                }
                Main.NewText("The Attraidies Illusion has been vanquished...", 190, 140, 150);
            }
        }

        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), mod.ItemType("HealingElixir"));

            if (Main.rand.NextFloat() <= .05f && ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) //drops wand of frost in legacy mode
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WandOfFrost"), 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .05f && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) //drops aquamarine ring in revamped
            {
                Item.NewItem(npc.getRect(), mod.ItemType("AquamarineRing"), 1, false, -1);
            }
            if (Main.rand.NextFloat() <= .3f)
            {
                Item.NewItem(npc.getRect(), ItemID.GoldenKey);
            }
            if (Main.rand.NextFloat() <= .9f)
            {
                Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
            }
            if (Main.rand.NextFloat() <= .4f)
            {
                Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 2);
            }
            if (Main.rand.NextFloat() <= .01f)
            {
                Item.NewItem(npc.getRect(), ItemID.GillsPotion);
            }
            if (Main.rand.NextFloat() <= .1f)
            {
                Item.NewItem(npc.getRect(), ItemID.HunterPotion);
            }
            if (Main.rand.NextFloat() <= .6f)
            {
                Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion);
            }
            if (Main.rand.NextFloat() <= .8f)
            {
                Item.NewItem(npc.getRect(), ItemID.RegenerationPotion);
            }
            if (Main.rand.NextFloat() <= .2f)
            {
                Item.NewItem(npc.getRect(), ItemID.ShinePotion);
            }
        }
    }
}