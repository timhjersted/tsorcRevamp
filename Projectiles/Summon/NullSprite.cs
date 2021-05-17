using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace tsorcRevamp.Projectiles.Summon {
    public class NullSprite : ModProjectile {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Null Sprite");
            Main.projFrames[projectile.type] = 1; //4?
            Main.projPet[projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[projectile.type] = true;
            ProjectileID.Sets.Homing[projectile.type] = true;
        }

        public override void SetDefaults() {
            projectile.width = 20;
            projectile.height = 20;
            projectile.tileCollide = false;
            projectile.minion = true;
            projectile.minionSlots = 1f;
            projectile.penetrate = -1;
        }

        public override void AI() {
            Player player = Main.player[projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>())) {
                projectile.timeLeft = 2;
            }

            Lighting.AddLight(projectile.Center, 0.55f, 0, 0.1f);
            AI_062(); //vanilla Imp / Hornet / Tempest AI (behaves like imp)
        }

        private void AI_062() {
            float num32 = 0.05f;
            float num33 = projectile.width;
            for (int m = 0; m < 1000; m++) {
                if (m != projectile.whoAmI && Main.projectile[m].active && Main.projectile[m].owner == projectile.owner && Main.projectile[m].type == projectile.type && Math.Abs(projectile.position.X - Main.projectile[m].position.X) + Math.Abs(projectile.position.Y - Main.projectile[m].position.Y) < num33) {
                    if (projectile.position.X < Main.projectile[m].position.X) {
                        projectile.velocity.X -= num32;
                    }
                    else {
                        projectile.velocity.X += num32;
                    }
                    if (projectile.position.Y < Main.projectile[m].position.Y) {
                        projectile.velocity.Y -= num32;
                    }
                    else {
                        projectile.velocity.Y += num32;
                    }
                }
            }
            Vector2 vector = projectile.position;
            float num34 = 400f;
            bool flag = false;
            int num35 = -1;
            projectile.tileCollide = true;

            NPC rightClickTarget = projectile.OwnerMinionAttackTargetNPC;
            if (rightClickTarget != null && rightClickTarget.CanBeChasedBy(this)) {
                float num3 = Vector2.Distance(rightClickTarget.Center, projectile.Center);
                if (((Vector2.Distance(projectile.Center, vector) > num3 && num3 < num34) || !flag) && Collision.CanHitLine(projectile.position, projectile.width, projectile.height, rightClickTarget.position, rightClickTarget.width, rightClickTarget.height)) {
                    num34 = num3;
                    vector = rightClickTarget.Center;
                    flag = true;
                    num35 = rightClickTarget.whoAmI;
                }
            }
            if (!flag) {
                for (int num4 = 0; num4 < 200; num4++) {
                    NPC nPC2 = Main.npc[num4];
                    if (nPC2.CanBeChasedBy(this)) {
                        float num5 = Vector2.Distance(nPC2.Center, projectile.Center);
                        if (((Vector2.Distance(projectile.Center, vector) > num5 && num5 < num34) || !flag) && Collision.CanHitLine(projectile.position, projectile.width, projectile.height, nPC2.position, nPC2.width, nPC2.height)) {
                            num34 = num5;
                            vector = nPC2.Center;
                            flag = true;
                            num35 = num4;
                        }
                    }
                }
            }

            int num6 = 500;
            if (flag) {
                num6 = 1000;
            }
            Player player = Main.player[projectile.owner];
            if (Vector2.Distance(player.Center, projectile.Center) > (float)num6) {
                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
            }
            if (projectile.ai[0] == 1f) {
                projectile.tileCollide = false;
            }
            if (flag && projectile.ai[0] == 0f) {
                Vector2 vector4 = vector - projectile.Center;
                float num7 = vector4.Length();
                vector4.Normalize();
                if (num7 > 200f) {
                    float num11 = 6f;
                    vector4 *= num11;
                    projectile.velocity.X = (projectile.velocity.X * 40f + vector4.X) / 41f;
                    projectile.velocity.Y = (projectile.velocity.Y * 40f + vector4.Y) / 41f;
                }
                else {
                    if (num7 < 150f) {
                        float num14 = 4f;
                        vector4 *= 0f - num14;
                        projectile.velocity.X = (projectile.velocity.X * 40f + vector4.X) / 41f;
                        projectile.velocity.Y = (projectile.velocity.Y * 40f + vector4.Y) / 41f;
                    }
                    else {
                        projectile.velocity *= 0.97f;
                    }
                }
            }
            else {
                if (!Collision.CanHitLine(projectile.Center, 1, 1, Main.player[projectile.owner].Center, 1, 1)) {
                    projectile.ai[0] = 1f;
                }
                float num15 = 6f;
                if (projectile.ai[0] == 1f) {
                    num15 = 15f;
                }
                Vector2 distToPlayer = player.Center - projectile.Center;

                projectile.ai[1] = 3600f;
                projectile.netUpdate = true;
                int minionOffset = 1;
                for (int i = 0; i < projectile.whoAmI; i++) {
                    if (Main.projectile[i].active && Main.projectile[i].owner == projectile.owner && Main.projectile[i].type == projectile.type) {
                        minionOffset++;
                    }
                }
                distToPlayer.X -= 10 * Main.player[projectile.owner].direction;
                distToPlayer.X -= minionOffset * 30 * Main.player[projectile.owner].direction;
                distToPlayer.Y -= 40f;

                float num18 = distToPlayer.Length();
                if (num18 > 200f && num15 < 9f) {
                    num15 = 9f;
                }
                num15 = (int)(num15 * 0.75);

                if (num18 < 100f && projectile.ai[0] == 1f && !Collision.SolidCollision(projectile.position, projectile.width, projectile.height)) {
                    projectile.ai[0] = 0f;
                    projectile.netUpdate = true;
                }
                if (num18 > 2000f) {
                    projectile.position.X = Main.player[projectile.owner].Center.X - (float)(projectile.width / 2);
                    projectile.position.Y = Main.player[projectile.owner].Center.Y - (float)(projectile.width / 2);
                }
                if (num18 > 10f) {
                    distToPlayer.Normalize();
                    if (num18 < 50f) {
                        num15 /= 2f;
                    }
                    distToPlayer *= num15;
                    projectile.velocity = (projectile.velocity * 20f + distToPlayer) / 21f;
                }
                else {
                    projectile.direction = Main.player[projectile.owner].direction;
                    projectile.velocity *= 0.9f;
                }

            }

            projectile.rotation = projectile.velocity.X * 0.05f;
            /*
            projectile.frameCounter++;

            if (projectile.frameCounter >= 16) {
                projectile.frameCounter = 0;
            }
            projectile.frame = projectile.frameCounter / 4;
            if (projectile.ai[1] > 0f && projectile.ai[1] < 16f) {
                projectile.frame += 4;
            }
            */
            if (Main.rand.Next(6) == 0) {
                int num19 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 191, 0f, 0f, 100, default, 2f);
                Main.dust[num19].velocity *= 0.3f;
                Main.dust[num19].noGravity = true;
                Main.dust[num19].noLight = true;
            }

            if (projectile.velocity.X > 0f) {
                projectile.spriteDirection = (projectile.direction = -1);
            }
            else if (projectile.velocity.X < 0f) {
                projectile.spriteDirection = (projectile.direction = 1);
            }

            if (projectile.ai[1] > 0f) {
                projectile.ai[1] += 1f;
                if (Main.rand.Next(3) == 0) {
                    projectile.ai[1] += 1f;
                }
            }
            if (projectile.ai[1] > 180) {
                projectile.ai[1] = 0f;
            }

            if (projectile.ai[0] != 0f) {
                return;
            }
            float shotSpeed = 16f;
            int shotType = ModContent.ProjectileType<SummonProjectiles.NullSpriteBeam>();
            if (!flag) {
                return;
            }

            if ((vector - projectile.Center).X > 0f) {
                projectile.spriteDirection = (projectile.direction = -1);
            }
            else if ((vector - projectile.Center).X < 0f) {
                projectile.spriteDirection = (projectile.direction = 1);
            }

            if (projectile.ai[1] == 0f) {
                projectile.ai[1] += 1f;
                if (Main.myPlayer == projectile.owner) {
                    Vector2 vector7 = vector - projectile.Center;
                    vector7.Normalize();
                    vector7 *= shotSpeed;
                    int num30 = Projectile.NewProjectile(projectile.Center.X, projectile.Center.Y, vector7.X, vector7.Y, shotType, projectile.damage, 0f, Main.myPlayer);
                    Main.projectile[num30].timeLeft = 300;
                    Main.projectile[num30].netUpdate = true;
                    projectile.netUpdate = true;
                }
            }
        }
    }
}
