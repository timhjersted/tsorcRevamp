using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.NPCs.Enemies {
    class MindflayerIllusion : ModNPC {

        public override void SetDefaults() {
            Main.npcFrameCount[npc.type] = 3;
            npc.npcSlots = 5;
            animationType = 29;
            npc.aiStyle = 0;
            npc.damage = 25;
            npc.defense = 12;
            npc.height = 44;
            npc.timeLeft = 22500;
            npc.lifeMax = 1000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.value = 500;
            npc.width = 28;
            npc.knockBackResist = 0.3f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
        }



        float customAi1;


        #region AI
        public override void AI() {



            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (npc.life > 200) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X, npc.velocity.Y, 210, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 200) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 140, Color.Red, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.Server) {
                if (npc.ai[0] >= 12 && npc.ai[2] < 5) {
                    float num48 = 1f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 10) / 5;
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 10) / 5;
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 8;//(int) (14f * npc.scale);
                        int type = ModContent.ProjectileType<Projectiles.Enemy.TheOracle>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 200;
                        Main.projectile[num54].aiStyle = 4;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
                        npc.ai[0] = 0;
                        npc.ai[2]++;
                    }


                }

                if (Main.rand.Next(90) == 0) //1 in 90 chance boss will use attack when it flies down on top of you
                        {
                    float num48 = 2f;
                    Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 520 + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 13;//(int) (14f * npc.scale);
                        int type = ModContent.ProjectileType<Projectiles.Enemy.AntiGravityBlast>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, damage, 0f, Main.myPlayer, Main.player[npc.target].whoAmI);
                        Main.projectile[num54].timeLeft = 500;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                        npc.ai[3] = 0; ;
                    }
                }


            }

            if (npc.ai[1] >= 30) {
                npc.velocity.X *= 0.27f;
                npc.velocity.Y *= 0.37f;
            }

            if ((npc.ai[1] >= 200 && npc.life > 100) || (npc.ai[1] >= 120 && npc.life <= 100)) {
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++) {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 55, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = true;
                }
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active) {
                    npc.TargetClosest(true);
                }
                if (Main.player[npc.target].dead) {
                    npc.position.X = 0;
                    npc.position.Y = 0;
                    if (npc.timeLeft > 10) {
                        npc.timeLeft = 10;
                        return;
                    }
                }
                else {





                    //end of W1k's Death code
                    //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                    int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
                    int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
                    int x_blockpos = (int)npc.position.X / 16; // corner not center
                    int y_blockpos = (int)npc.position.Y / 16; // corner not center
                    int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 9000000f) { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always teleport was true for no teleport
                    }
                    while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
                    {
                        if (tp_counter >= 100) // run 100 times
                            break; //return;
                        tp_counter++;

                        int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        int tp_y_target = Main.rand.Next(target_y_blockpos - tp_radius, target_y_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                        { // (tp_x_target,m) is block under its feet I think
                            if ((m < target_y_blockpos - 12 || m > target_y_blockpos + 12 || tp_x_target < target_x_blockpos - 12 || tp_x_target > target_x_blockpos + 12) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].active()) { // over 15 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1)) { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile

                                    npc.TargetClosest(true);
                                    npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                                    npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                                    npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    npc.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    npc.ai[1] = 0;
                                    npc.ai[3]++;
                                }
                            } // END over 4 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }


            //end region teleportation




            npc.ai[3]++;  // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 600) {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }
        }

        #endregion

        public override void FindFrame(int currentFrame) {

            if ((npc.velocity.X > -9 && npc.velocity.X < 9) && (npc.velocity.Y > -9 && npc.velocity.Y < 9)) {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
                if (npc.position.X > Main.player[npc.target].position.X) {
                    npc.spriteDirection = -1;
                }
                else {
                    npc.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2)) {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
            }
            else {
                npc.frameCounter += 1.0;
            }
            if (npc.frameCounter >= 1.0) {
                npc.frame.Y = npc.frame.Y + num;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= num * Main.npcFrameCount[npc.type]) {
                npc.frame.Y = 0;
            }
        }


        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.Heart);
            Item.NewItem(npc.getRect(), ItemID.Heart); //no it cant be a stack of 2. it has to be 2 stacks of 1
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>());
        }

        #region Gore
        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                Main.NewText("Hahahahaha! I'm going to destroy you, Red...", 150, 150, 150);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mindflayer Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores / Mindflayer Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mindflayer Gore 3"), 1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mindflayer Gore 2"), 1);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Mindflayer Gore 3"), 1f);
            }
        }
        #endregion
    }
}
