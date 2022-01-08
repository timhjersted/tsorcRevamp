using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies {
    class DungeonMage : ModNPC {
        public override void SetDefaults() {
            npc.npcSlots = 10;
            //npc.maxSpawns = 2; todo investigate
            npc.aiStyle = 0;
            npc.damage = 20;
            npc.defense = 10;
            npc.height = 44;
            npc.timeLeft = 22500;
            npc.lifeMax = 130;
            npc.scale = 1;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.lavaImmune = true;
            npc.value = 1500;
            npc.width = 28;
            npc.knockBackResist = 0.3f;
            Main.npcFrameCount[npc.type] = 3;
            animationType = NPCID.GoblinSorcerer;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.DungeonMageBanner>();
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            float chance = 0;
            if (spawnInfo.player.ZoneDungeon) {
                chance += 0.03f;
            }
            return chance;
        }

        public override void AI() {

            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (npc.life > 50) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X, npc.velocity.Y, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 50) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 140, Color.Red, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.Server) {
                if (npc.ai[0] >= 12 && npc.ai[2] < 5) {
                    float num48 = 2f;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    int damage = 18;
                    int type = ModContent.ProjectileType<Projectiles.Enemy.PoisonFlames>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, Main.myPlayer);
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 20);
                    npc.ai[0] = 0;
                    npc.ai[2]++;
                }
            }

            if (npc.ai[1] >= 60) {
                npc.velocity.X *= 0.17f;
                npc.velocity.Y *= 0.17f;
            }

            if ((npc.ai[1] >= 200 && npc.life > 50) || (npc.ai[1] >= 120 && npc.life <= 50)) {
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++) {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = false;
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
                        npc.timeLeft = 0;
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
                    int tp_radius = 35; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 9000000f) { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always teleport, use true for no teleport when far away
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
                            if ((m < target_y_blockpos - 9 || m > target_y_blockpos + 9 || tp_x_target < target_x_blockpos - 9 || tp_x_target > target_x_blockpos + 9) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].active()) { // over 9 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
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
                                    npc.velocity.X = (float)(Math.Cos(rotation) * 4) * -1;
                                    npc.velocity.Y = (float)(Math.Sin(rotation) * 4) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    npc.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    npc.ai[1] = 0;
                                }
                            } // END over 4 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }


            //end region teleportation


            //beginning of Omnir's Ultima Weapon projectile code

            npc.ai[3]++;

            if (npc.ai[3] >= 30) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 4f;
                    Vector2 myPos = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 220 + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - myPos.X) + Main.rand.Next(-20, 15);
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - myPos.Y) + Main.rand.Next(-20, 15);
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
                        float num51 = (float)Math.Sqrt((speedX * speedX) + (speedY * speedY));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 25;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.MiracleSprouter>();
                        int num54 = Projectile.NewProjectile(myPos.X, myPos.Y, speedX, speedY, type, damage, 0f, npc.whoAmI);
                        Main.projectile[num54].timeLeft = 110;
                        Main.projectile[num54].velocity.X = speedX;
                        Main.projectile[num54].velocity.Y = speedY;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                        npc.ai[3] = 0;
                    }
                }
            }

            npc.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 600) {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }
        }

        public override void FindFrame(int frameHeight) {
            if ((npc.velocity.X != 0 || npc.velocity.Y != 0) || (npc.ai[0] >= 12 && npc.ai[2] < 5))
            {
                npc.frame.Y = 1 * frameHeight;
            }
            else 
            {
                npc.frame.Y = 0 * frameHeight;
            }
        }

        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);

            if (Main.rand.Next(2) == 0) {
                Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
            }

            if (Main.rand.NextFloat() <= .07f) {
                Item.NewItem(npc.getRect(), ItemID.SpellTome);
            }

            if (Main.rand.NextFloat() <= .3f) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>());
            }
        }


        public override void HitEffect(int hitDirection, double damage) {
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
            if (npc.life <= 0) {
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 1"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
            }
        }
    }
}
