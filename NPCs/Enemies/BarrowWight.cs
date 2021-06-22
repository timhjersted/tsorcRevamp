using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using static tsorcRevamp.oSpawnHelper;

namespace tsorcRevamp.NPCs.Enemies {
    class BarrowWight : ModNPC {

        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        public override void SetDefaults() {
            npc.width = 58;
            npc.height = 48;
            npc.aiStyle = 22;
            npc.damage = 115;
            npc.defense = 15;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = 470;
            npc.knockBackResist = 0;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 850;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            Main.npcFrameCount[npc.type] = 4;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {

            Player p = spawnInfo.player;
            int playerXTile = (int)(p.Bottom.X + 8f) / 16;
            if (p.townNPCs > 0f || p.ZoneMeteor) return 0;
            if ((oSurface(p) || oUnderSurface(p) || oUnderground(p) || oCavern(p)) && (playerXTile > Main.maxTilesX * 0.2f && playerXTile < Main.maxTilesX * 0.35f || playerXTile > Main.maxTilesX * 0.65f && playerXTile < Main.maxTilesX * 0.8f)) return 0.005f;
            if (!Main.hardMode && p.ZoneDungeon) return .00833f;
            if (!tsorcRevampWorld.SuperHardMode && Main.hardMode && oSky(p)) return 0.0667f;
            if (!tsorcRevampWorld.SuperHardMode && Main.hardMode && p.ZoneDungeon) return 0.033f;
            if (tsorcRevampWorld.SuperHardMode && oSky(p)) return 0.025f;
            if (tsorcRevampWorld.SuperHardMode && p.ZoneDungeon) return 0.008f;
            return 0;
        }

        public override void AI() {
            //the following line makes barrow wights completely break in multiplayer. it has been modified.
            //npc.ai[1] += Main.rand.Next(2, 5) * 0.1f * npc.scale;

            npc.ai[1] += 0.3f;
            if (npc.ai[1] >= 10f) {
                npc.TargetClosest(true);
                npc.netUpdate = true;



                // charge forward code 
                if (Main.rand.Next(2050) == 1) {
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 11) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
                    npc.ai[1] = 1f;
                    npc.netUpdate = true;
                }
                if (chargeDamageFlag == true) {
                    npc.damage = 115;
                    chargeDamage++;
                }
                if (chargeDamage >= 115) {
                    chargeDamageFlag = false;
                    npc.damage = 115;
                    chargeDamage = 0;
                }




            }
            if (npc.justHit) {
                npc.ai[2] = 0f;
            }
            if (npc.ai[2] >= 0f) {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (npc.position.X > npc.ai[0] - (float)num258 && npc.position.X < npc.ai[0] + (float)num258) {
                    flag26 = true;
                }
                else {
                    if ((npc.velocity.X < 0f && npc.direction > 0) || (npc.velocity.X > 0f && npc.direction < 0)) {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (npc.position.Y > npc.ai[1] - (float)num258 && npc.position.Y < npc.ai[1] + (float)num258) {
                    flag27 = true;
                }
                if (flag26 && flag27) {
                    npc.ai[2] += 1f;
                    if (npc.ai[2] >= 60f) {
                        npc.ai[2] = -200f;
                        npc.direction *= -1;
                        npc.velocity.X = npc.velocity.X * -1f;
                        npc.collideX = false;
                    }
                }
                else {
                    npc.ai[0] = npc.position.X;
                    npc.ai[1] = npc.position.Y;
                    npc.ai[2] = 0f;
                }
                npc.TargetClosest(true);
            }
            else {
                npc.ai[2] += 1f;
                if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2)) {
                    npc.direction = -1;
                }
                else {
                    npc.direction = 1;
                }
            }
            if (npc.position.Y > Main.player[npc.target].position.Y) {
                npc.velocity.Y -= .05f;
            }
            if (npc.position.Y < Main.player[npc.target].position.Y) {
                npc.velocity.Y += .05f;
            }
            if (npc.collideX) {
                npc.velocity.X = npc.oldVelocity.X * -0.4f;
                if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 1f) {
                    npc.velocity.X = 1f;
                }
                if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -1f) {
                    npc.velocity.X = -1f;
                }
            }
            if (npc.collideY) {
                npc.velocity.Y = npc.oldVelocity.Y * -0.25f;
                if (npc.velocity.Y > 0f && npc.velocity.Y < 1f) {
                    npc.velocity.Y = 1f;
                }
                if (npc.velocity.Y < 0f && npc.velocity.Y > -1f) {
                    npc.velocity.Y = -1f;
                }
            }
            float topSpeed = .5f;
            if (npc.direction == -1 && npc.velocity.X > -topSpeed) {
                npc.velocity.X = npc.velocity.X - 0.1f;
                if (npc.velocity.X > topSpeed) {
                    npc.velocity.X = npc.velocity.X - 0.1f;
                }
                else {
                    if (npc.velocity.X > 0f) {
                        npc.velocity.X = npc.velocity.X + 0.05f;
                    }
                }
                if (npc.velocity.X < -topSpeed) {
                    npc.velocity.X = -topSpeed;
                }
            }
            else {
                if (npc.direction == 1 && npc.velocity.X < topSpeed) {
                    npc.velocity.X = npc.velocity.X + 0.1f;
                    if (npc.velocity.X < -topSpeed) {
                        npc.velocity.X = npc.velocity.X + 0.1f;
                    }
                    else {
                        if (npc.velocity.X < 0f) {
                            npc.velocity.X = npc.velocity.X - 0.05f;
                        }
                    }
                    if (npc.velocity.X > topSpeed) {
                        npc.velocity.X = topSpeed;
                    }
                }
            }
            if (npc.directionY == -1 && npc.velocity.Y > -2.5) {
                npc.velocity.Y = npc.velocity.Y - 0.04f;
                if (npc.velocity.Y > 2.5) {
                    npc.velocity.Y = npc.velocity.Y - 0.05f;
                }
                else {
                    if (npc.velocity.Y > 0f) {
                        npc.velocity.Y = npc.velocity.Y + 0.03f;
                    }
                }
                if (npc.velocity.Y < -2.5) {
                    npc.velocity.Y = -2.5f;
                }
            }
            else {
                if (npc.directionY == 1 && npc.velocity.Y < 2.5) {
                    npc.velocity.Y = npc.velocity.Y + 0.04f;
                    if (npc.velocity.Y < -2.5) {
                        npc.velocity.Y = npc.velocity.Y + 0.05f;
                    }
                    else {
                        if (npc.velocity.Y < 0f) {
                            npc.velocity.Y = npc.velocity.Y - 0.03f;
                        }
                    }
                    if (npc.velocity.Y > 2.5) {
                        npc.velocity.Y = 2.5f;
                    }
                }
            }
            Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0.25f);
            return;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.BrokenArmor, 1800);
                target.AddBuff(BuffID.Invisibility, 3600);
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000);
            }
        }

        #region Frames
        public override void FindFrame(int currentFrame) {
            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if (npc.velocity.X < 0) {
                npc.spriteDirection = -1;
            }
            else {
                npc.spriteDirection = 1;
            }
            npc.rotation = npc.velocity.X * 0.08f;
            npc.frameCounter += 1.0;
            if (npc.frameCounter >= 4.0) {
                npc.frame.Y = npc.frame.Y + num;
                npc.frameCounter = 0.0;
            }
            if (npc.frame.Y >= num * Main.npcFrameCount[npc.type]) {
                npc.frame.Y = 0;
            }
            if (npc.ai[3] == 0) {
                npc.alpha = 0;
            }
            else {
                npc.alpha = 200;
            }
        }
        #endregion

        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Barrow Wight Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Barrow Wight Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Barrow Wight Gore 2"), 1f);

                Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.3f, 0.3f, 200, default, 1f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 3f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(npc.position, npc.width, npc.height, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 4f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 2f);
                Dust.NewDust(npc.position, npc.height, npc.width, 45, 0.2f, 0.2f, 200, default, 4f);
            }
        }

        public override void NPCLoot() {
            if (Main.rand.Next(5) == 0) { Item.NewItem(npc.getRect(), ItemID.ShinePotion); }
            if (Main.rand.Next(10) == 0) { Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion); }
            if (Main.rand.Next(25) == 0) { Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion); }
            if (Main.rand.Next(25) == 0) { Item.NewItem(npc.getRect(), ItemID.RegenerationPotion); }
            if (Main.rand.Next(25) == 0) { Item.NewItem(npc.getRect(), ItemID.SpelunkerPotion); }
            if (Main.rand.Next(5) == 0) { Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>()); }
            if (Main.rand.Next(50) == 0) { Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>()); }
        }

        /* what the hell IS this? i cant find anything about it
         public int MagicDefenseValue() 
            { 
                return 5;
            } 
          
         */
    }
}
