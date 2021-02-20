using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies {
    class GuardianCorruptor : ModNPC {

        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 3;
        }

        public override void SetDefaults() {
            npc.width = 124;
            npc.height = 124;
            npc.damage = 165;
            npc.defense = 150;
            npc.lifeMax = 30000;
            npc.aiStyle = -1;
            npc.npcSlots = 30;
            npc.value = 18750;
            npc.knockBackResist = 0.5f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.noGravity = true;

        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            return SpawnCondition.Corruption.Chance * 0.5f;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.Weak, 7200, true);
            target.AddBuff(BuffID.BrokenArmor, 180, true);
        }

        public override void FindFrame(int frameHeight) {
            npc.frameCounter++;
            if (npc.frameCounter < 10) {
                npc.frame.Y = 0;
            }
            else if (npc.frameCounter < 20) {
                npc.frame.Y = frameHeight;
            }
            else if (npc.frameCounter < 30) {
                npc.frame.Y = frameHeight * 2;
            }
            else {
                npc.frameCounter = 0;
            }
        }
        public override void AI() {



            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead) {
                npc.TargetClosest();
            }

            float accelRate = 0.02f;
            Vector2 enemyPos = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
            float targetPosX = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2);
            float targetPosY = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2);
            targetPosX = (int)(targetPosX / 8f) * 8;
            targetPosY = (int)(targetPosY / 8f) * 8;
            enemyPos.X = (int)(enemyPos.X / 8f) * 8;
            enemyPos.Y = (int)(enemyPos.Y / 8f) * 8;
            targetPosX -= enemyPos.X;
            targetPosY -= enemyPos.Y;
            float targetDistSegment = (float)Math.Sqrt(targetPosX * targetPosX + targetPosY * targetPosY);
            float targetDistAbsolute = targetDistSegment;
            if (targetDistSegment == 0f) {
                targetPosX = npc.velocity.X;
                targetPosY = npc.velocity.Y;
            }
            else {
                targetDistSegment = 8f / targetDistSegment;
                targetPosX *= targetDistSegment;
                targetPosY *= targetDistSegment;
            }
            npc.ai[0] += 1f;
            if (npc.ai[0] > 0f) {
                npc.velocity.Y += 0.01f;
            }
            else {
                npc.velocity.Y -= 0.023f;
            }
            if (npc.ai[0] < -100f || npc.ai[0] > 100f) {
                npc.velocity.X += 0.023f;
            }
            else {
                npc.velocity.X -= 0.023f;
            }
            if (npc.ai[0] > 200f) {
                npc.ai[0] = -200f;
            }

            if (targetDistAbsolute < 150f) {
                npc.velocity.X += targetPosX * 0.005f;
                npc.velocity.Y += targetPosY * 0.005f;
            }
            if (targetDistAbsolute > 600f) {
                npc.velocity.X += targetPosX * 0.005f;
                npc.velocity.Y += targetPosY * 0.005f;
            }

            if (Main.player[npc.target].dead) {
                targetPosX = (float)npc.direction * 4.2f / 2f;
                targetPosY = -4.2f / 2f;
            }
            if (npc.velocity.X < targetPosX) {
                npc.velocity.X += accelRate;
            }
            else if (npc.velocity.X > targetPosX) {
                npc.velocity.X -= accelRate;

            }
            if (npc.velocity.Y < targetPosY) {
                npc.velocity.Y += accelRate;

            }
            else if (npc.velocity.Y > targetPosY) {
                npc.velocity.Y -= accelRate;

            }

            npc.rotation = (float)Math.Atan2(targetPosY, targetPosX) - 1.57f;


            if (npc.collideX) {
                npc.netUpdate = true;
                npc.velocity.X = npc.oldVelocity.X * (-0.7f);
                if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 2f) {
                    npc.velocity.X = 2f;
                }
                if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -2f) {
                    npc.velocity.X = -2f;
                }
            }
            if (npc.collideY) {
                npc.netUpdate = true;
                npc.velocity.Y = npc.oldVelocity.Y * (-0.7f);
                if (npc.velocity.Y > 0f && (double)npc.velocity.Y < 1.5) {
                    npc.velocity.Y = 2f;
                }
                if (npc.velocity.Y < 0f && (double)npc.velocity.Y > -1.5) {
                    npc.velocity.Y = -2f;
                }
            }
            else if (Main.rand.Next(12) == 0) {
                int eaterDust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + (float)npc.height * 0.25f), npc.width, (int)((float)npc.height * 0.5f), 18, npc.velocity.X, 2f, 75);
                Main.dust[eaterDust].velocity.X *= 0.5f;
                Main.dust[eaterDust].velocity.Y *= 0.1f;
            }

            if (npc.wet) {
                if (npc.velocity.Y > 0f) {
                    npc.velocity.Y *= 0.95f;
                }
                npc.velocity.Y -= 0.3f;
                if (npc.velocity.Y < -2f) {
                    npc.velocity.Y = -2f;
                }
            }


            if (Main.netMode != NetmodeID.MultiplayerClient && !Main.player[npc.target].dead) {
                if (npc.justHit) {
                    npc.localAI[0] = 0f;
                }
                npc.localAI[0] += 1f;
                if (npc.localAI[0] == 180f) {
                    if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)) {
                        int shotCount = 3;
                        Vector2 shotOrigin = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                        float distX = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - shotOrigin.X;
                        float distY = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - shotOrigin.Y;
                        float distAbs = (float)Math.Sqrt(distX * distX + distY * distY);
                        distAbs = 7f / distAbs;
                        
                        for (int i = 0; i < shotCount; i++) {
                            Vector2 shotDirection = new Vector2((distX * distAbs) / 1.5f, (distY * distAbs) / 1.5f);
                            int guardianSpit = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height / 2) + npc.velocity.Y), ModContent.NPCType<ViciousSpit>());
                            Main.npc[guardianSpit].velocity = shotDirection.RotatedBy(MathHelper.ToRadians(12 - (12 * i)));
                        }
                    }
                    npc.localAI[0] = 0f;
                }
            }

            if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit) {
                npc.netUpdate = true;
            }
        }
    }
}
