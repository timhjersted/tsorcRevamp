using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace tsorcRevamp.Projectiles.Summon {
    public class NullSprite : ModProjectile {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Null Sprite");
            Main.projFrames[Projectile.type] = 1; //4?
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.Homing[Projectile.type] = true;
        }

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
        }

        public override void AI() {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active) {
                player.ClearBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>());
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.Summon.NullSpriteBuff>())) {
                Projectile.timeLeft = 2;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0, 0.1f);
            AI_062(); //vanilla Imp / Hornet / Tempest AI (behaves like imp)
        }

        private void AI_062() {
            float num32 = 0.05f;
            float num33 = Projectile.width;
            for (int m = 0; m < 1000; m++) {
                if (m != Projectile.whoAmI && Main.projectile[m].active && Main.projectile[m].owner == Projectile.owner && Main.projectile[m].type == Projectile.type && Math.Abs(Projectile.position.X - Main.projectile[m].position.X) + Math.Abs(Projectile.position.Y - Main.projectile[m].position.Y) < num33) {
                    if (Projectile.position.X < Main.projectile[m].position.X) {
                        Projectile.velocity.X -= num32;
                    }
                    else {
                        Projectile.velocity.X += num32;
                    }
                    if (Projectile.position.Y < Main.projectile[m].position.Y) {
                        Projectile.velocity.Y -= num32;
                    }
                    else {
                        Projectile.velocity.Y += num32;
                    }
                }
            }
            Vector2 vector = Projectile.position;
            float num34 = 400f;
            bool flag = false;
            int num35 = -1;
            Projectile.tileCollide = true;

            NPC rightClickTarget = Projectile.OwnerMinionAttackTargetNPC;
            if (rightClickTarget != null && rightClickTarget.CanBeChasedBy(this)) {
                float num3 = Vector2.Distance(rightClickTarget.Center, Projectile.Center);
                if (((Vector2.Distance(Projectile.Center, vector) > num3 && num3 < num34) || !flag) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, rightClickTarget.position, rightClickTarget.width, rightClickTarget.height)) {
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
                        float num5 = Vector2.Distance(nPC2.Center, Projectile.Center);
                        if (((Vector2.Distance(Projectile.Center, vector) > num5 && num5 < num34) || !flag) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, nPC2.position, nPC2.width, nPC2.height)) {
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
            Player player = Main.player[Projectile.owner];
            if (Vector2.Distance(player.Center, Projectile.Center) > (float)num6) {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 1f) {
                Projectile.tileCollide = false;
            }
            if (flag && Projectile.ai[0] == 0f) {
                Vector2 vector4 = vector - Projectile.Center;
                float num7 = vector4.Length();
                vector4.Normalize();
                if (num7 > 200f) {
                    float num11 = 6f;
                    vector4 *= num11;
                    Projectile.velocity.X = (Projectile.velocity.X * 40f + vector4.X) / 41f;
                    Projectile.velocity.Y = (Projectile.velocity.Y * 40f + vector4.Y) / 41f;
                }
                else {
                    if (num7 < 150f) {
                        float num14 = 4f;
                        vector4 *= 0f - num14;
                        Projectile.velocity.X = (Projectile.velocity.X * 40f + vector4.X) / 41f;
                        Projectile.velocity.Y = (Projectile.velocity.Y * 40f + vector4.Y) / 41f;
                    }
                    else {
                        Projectile.velocity *= 0.97f;
                    }
                }
            }
            else {
                if (!Collision.CanHitLine(Projectile.Center, 1, 1, Main.player[Projectile.owner].Center, 1, 1)) {
                    Projectile.ai[0] = 1f;
                }
                float num15 = 6f;
                if (Projectile.ai[0] == 1f) {
                    num15 = 15f;
                }
                Vector2 distToPlayer = player.Center - Projectile.Center;

                Projectile.ai[1] = 3600f;
                Projectile.netUpdate = true;
                int minionOffset = 1;
                for (int i = 0; i < Projectile.whoAmI; i++) {
                    if (Main.projectile[i].active && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].type == Projectile.type) {
                        minionOffset++;
                    }
                }
                distToPlayer.X -= 10 * Main.player[Projectile.owner].direction;
                distToPlayer.X -= minionOffset * 30 * Main.player[Projectile.owner].direction;
                distToPlayer.Y -= 40f;

                float num18 = distToPlayer.Length();
                if (num18 > 200f && num15 < 9f) {
                    num15 = 9f;
                }
                num15 = (int)(num15 * 0.75);

                if (num18 < 100f && Projectile.ai[0] == 1f && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height)) {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                if (num18 > 2000f) {
                    Projectile.position.X = Main.player[Projectile.owner].Center.X - (float)(Projectile.width / 2);
                    Projectile.position.Y = Main.player[Projectile.owner].Center.Y - (float)(Projectile.width / 2);
                }
                if (num18 > 10f) {
                    distToPlayer.Normalize();
                    if (num18 < 50f) {
                        num15 /= 2f;
                    }
                    distToPlayer *= num15;
                    Projectile.velocity = (Projectile.velocity * 20f + distToPlayer) / 21f;
                }
                else {
                    Projectile.direction = Main.player[Projectile.owner].direction;
                    Projectile.velocity *= 0.9f;
                }

            }

            Projectile.rotation = Projectile.velocity.X * 0.05f;
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
                int num19 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 191, 0f, 0f, 100, default, 2f);
                Main.dust[num19].velocity *= 0.3f;
                Main.dust[num19].noGravity = true;
                Main.dust[num19].noLight = true;
            }

            if (Projectile.velocity.X > 0f) {
                Projectile.spriteDirection = (Projectile.direction = -1);
            }
            else if (Projectile.velocity.X < 0f) {
                Projectile.spriteDirection = (Projectile.direction = 1);
            }

            if (Projectile.ai[1] > 0f) {
                Projectile.ai[1] += 1f;
                if (Main.rand.Next(3) == 0) {
                    Projectile.ai[1] += 1f;
                }
            }
            if (Projectile.ai[1] > 180) {
                Projectile.ai[1] = 0f;
            }

            if (Projectile.ai[0] != 0f) {
                return;
            }
            float shotSpeed = 16f;
            int shotType = ModContent.ProjectileType<SummonProjectiles.NullSpriteBeam>();
            if (!flag) {
                return;
            }

            if ((vector - Projectile.Center).X > 0f) {
                Projectile.spriteDirection = (Projectile.direction = -1);
            }
            else if ((vector - Projectile.Center).X < 0f) {
                Projectile.spriteDirection = (Projectile.direction = 1);
            }

            if (Projectile.ai[1] == 0f) {
                Projectile.ai[1] += 1f;
                if (Main.myPlayer == Projectile.owner) {
                    Vector2 vector7 = vector - Projectile.Center;
                    vector7.Normalize();
                    vector7 *= shotSpeed;
                    int num30 = Projectile.NewProjectile(Projectile.Center.X, Projectile.Center.Y, vector7.X, vector7.Y, shotType, Projectile.damage, 0f, Main.myPlayer);
                    Main.projectile[num30].timeLeft = 300;
                    Main.projectile[num30].netUpdate = true;
                    Projectile.netUpdate = true;
                }
            }
        }
    }
}
