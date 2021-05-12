using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses {
    /*
    class Skeletron : GlobalNPC {
        public override bool PreAI(NPC npc) { //todo
            if ((npc.type == NPCID.SkeletronHand) && ModContent.GetInstance<tsorcRevampConfig>().RenameSkeletron) {
                npc.GivenName = "Gravelord Nito";
            }
            return true;
        }
    }
    */
    public class GravelordNito : ModNPC {

        public override bool Autoload(ref string name) => false;
        public override void SetDefaults() {
            npc.width = 80;
            npc.height = 102;
            npc.aiStyle = 11;
            npc.damage = 50;
            npc.defense = 12;
            npc.lifeMax = 4400;
            npc.HitSound = SoundID.NPCHit2;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 80000;
            npc.knockBackResist = 0f;
            npc.boss = true;
            npc.npcSlots = 6f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
        }

        public override bool CheckDead() {
            if (Main.netMode == NetmodeID.SinglePlayer) {
                NPC.downedBoss3 = true;
            }
            else if (Main.netMode == NetmodeID.Server) {
                NPC.downedBoss3 = true;
                NetMessage.SendData(MessageID.WorldData);
            }
            return true;
        }

        public override void AI() {

            npc.defense = npc.defDefense;
            if (npc.ai[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient) {
                npc.TargetClosest();
                npc.ai[0] = 1f;

                int num602 = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)npc.position.Y + npc.height / 2, 36, npc.whoAmI);
                Main.npc[num602].ai[0] = -1f;
                Main.npc[num602].ai[1] = npc.whoAmI;
                Main.npc[num602].target = npc.target;
                Main.npc[num602].netUpdate = true;
                num602 = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)npc.position.Y + npc.height / 2, 36, npc.whoAmI);
                Main.npc[num602].ai[0] = 1f;
                Main.npc[num602].ai[1] = npc.whoAmI;
                Main.npc[num602].ai[3] = 150f;
                Main.npc[num602].target = npc.target;
                Main.npc[num602].netUpdate = true;

            }
            if (Main.player[npc.target].dead || Math.Abs(npc.position.X - Main.player[npc.target].position.X) > 2000f || Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f) {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || Math.Abs(npc.position.X - Main.player[npc.target].position.X) > 2000f || Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 2000f) {
                    npc.ai[1] = 3f;
                }
            }
            if (Main.dayTime && npc.ai[1] != 3f && npc.ai[1] != 2f) {
                npc.ai[1] = 2f;
                Main.PlaySound(SoundID.Roar, (int)npc.position.X, (int)npc.position.Y, 0);
            }
            int num613 = 0;
            if (Main.expertMode) {
                for (int num624 = 0; num624 < 200; num624++) {
                    if (Main.npc[num624].active && Main.npc[num624].type == NPCID.SkeletronHand) {
                        num613++;
                    }
                }
                npc.defense += num613 * 25;
                if ((num613 < 2 || (double)npc.life < (double)npc.lifeMax * 0.75) && npc.ai[1] == 0f) {
                    float num635 = 80f;
                    if (num613 == 0) {
                        num635 /= 2f;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] % num635 == 0f) {
                        Vector2 center23 = npc.Center;
                        float num642 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - center23.X;
                        float num643 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - center23.Y;
                        float num645 = (float)Math.Sqrt(num642 * num642 + num643 * num643);
                        if (Collision.CanHit(center23, 1, 1, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height)) {
                            float num646 = 3f;
                            if (num613 == 0) {
                                num646 += 2f;
                            }
                            float num647 = Main.player[npc.target].position.X + (float)Main.player[npc.target].width * 0.5f - center23.X + (float)Main.rand.Next(-20, 21);
                            float num648 = Main.player[npc.target].position.Y + (float)Main.player[npc.target].height * 0.5f - center23.Y + (float)Main.rand.Next(-20, 21);
                            float num649 = (float)Math.Sqrt(num647 * num647 + num648 * num648);
                            num649 = num646 / num649;
                            num647 *= num649;
                            num648 *= num649;
                            Vector2 vector68 = new Vector2(num647 * 1f + (float)Main.rand.Next(-50, 51) * 0.01f, num648 * 1f + (float)Main.rand.Next(-50, 51) * 0.01f);
                            vector68.Normalize();
                            vector68 *= num646;
                            vector68 += npc.velocity;
                            num647 = vector68.X;
                            num648 = vector68.Y;
                            int num650 = 17;
                            int num651 = 270;
                            center23 += vector68 * 5f;
                            int num652 = Projectile.NewProjectile(center23.X, center23.Y, num647, num648, num651, num650, 0f, Main.myPlayer, -1f);
                            Main.projectile[num652].timeLeft = 300;
                        }
                    }
                }
            }
            if (npc.ai[1] == 0f) {
                npc.damage = npc.defDamage;
                npc.ai[2] += 1f;
                if (npc.ai[2] >= 800f) {
                    npc.ai[2] = 0f;
                    npc.ai[1] = 1f;
                    npc.TargetClosest();
                    npc.netUpdate = true;
                }
                npc.rotation = npc.velocity.X / 15f;
                float num653 = 0.02f;
                float num654 = 2f;
                float num656 = 0.05f;
                float num657 = 8f;
                if (Main.expertMode) {
                    num653 = 0.03f;
                    num654 = 4f;
                    num656 = 0.07f;
                    num657 = 9.5f;
                }
                if (npc.position.Y > Main.player[npc.target].position.Y - 250f) {
                    if (npc.velocity.Y > 0f) {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y -= num653;
                    if (npc.velocity.Y > num654) {
                        npc.velocity.Y = num654;
                    }
                }
                else if (npc.position.Y < Main.player[npc.target].position.Y - 250f) {
                    if (npc.velocity.Y < 0f) {
                        npc.velocity.Y *= 0.98f;
                    }
                    npc.velocity.Y += num653;
                    if (npc.velocity.Y < 0f - num654) {
                        npc.velocity.Y = 0f - num654;
                    }
                }
                if (npc.position.X + (float)(npc.width / 2) > Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2)) {
                    if (npc.velocity.X > 0f) {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X -= num656;
                    if (npc.velocity.X > num657) {
                        npc.velocity.X = num657;
                    }
                }
                if (npc.position.X + (float)(npc.width / 2) < Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2)) {
                    if (npc.velocity.X < 0f) {
                        npc.velocity.X *= 0.98f;
                    }
                    npc.velocity.X += num656;
                    if (npc.velocity.X < 0f - num657) {
                        npc.velocity.X = 0f - num657;
                    }
                }
            }
            else if (npc.ai[1] == 1f) {
                npc.defense -= 10;
                npc.ai[2] += 1f;
                if (npc.ai[2] == 2f) {
                    Main.PlaySound(SoundID.Roar, (int)npc.position.X, (int)npc.position.Y, 0);
                }
                if (npc.ai[2] >= 400f) {
                    npc.ai[2] = 0f;
                    npc.ai[1] = 0f;
                }
                npc.rotation += (float)npc.direction * 0.3f;
                Vector2 vector79 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num658 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector79.X;
                float num659 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector79.Y;
                float num660 = (float)Math.Sqrt(num658 * num658 + num659 * num659);
                float num661 = 1.5f;
                if (Main.expertMode) {
                    npc.damage = (int)((double)npc.defDamage * 1.3);
                    num661 = 4f;
                    if (num660 > 150f) {
                        num661 *= 1.05f;
                    }
                    if (num660 > 200f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 250f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 300f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 350f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 400f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 450f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 500f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 550f) {
                        num661 *= 1.1f;
                    }
                    if (num660 > 600f) {
                        num661 *= 1.1f;
                    }
                    switch (num613) {
                        case 0:
                            num661 *= 1.2f;
                            break;
                        case 1:
                            num661 *= 1.1f;
                            break;
                    }
                }
                num660 = num661 / num660;
                npc.velocity.X = num658 * num660;
                npc.velocity.Y = num659 * num660;
            }
            else if (npc.ai[1] == 2f) {
                npc.damage = 1000;
                npc.defense = 9999;
                npc.rotation += (float)npc.direction * 0.3f;
                Vector2 vector90 = new Vector2(npc.position.X + (float)npc.width * 0.5f, npc.position.Y + (float)npc.height * 0.5f);
                float num662 = Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) - vector90.X;
                float num663 = Main.player[npc.target].position.Y + (float)(Main.player[npc.target].height / 2) - vector90.Y;
                float num664 = (float)Math.Sqrt(num662 * num662 + num663 * num663);
                num664 = 8f / num664;
                npc.velocity.X = num662 * num664;
                npc.velocity.Y = num663 * num664;
            }
            else if (npc.ai[1] == 3f) {
                npc.velocity.Y += 0.1f;
                if (npc.velocity.Y < 0f) {
                    npc.velocity.Y *= 0.95f;
                }
                npc.velocity.X *= 0.95f;
                if (npc.timeLeft > 50) {
                    npc.timeLeft = 50;
                }
            }
            if (npc.ai[1] != 2f && npc.ai[1] != 3f && (num613 != 0 || !Main.expertMode)) {
                int num665 = Dust.NewDust(new Vector2(npc.position.X + (float)(npc.width / 2) - 15f - npc.velocity.X * 5f, npc.position.Y + (float)npc.height - 2f), 30, 10, 5, (0f - npc.velocity.X) * 0.2f, 3f, 0, default, 2f);
                Main.dust[num665].noGravity = true;
                Main.dust[num665].velocity.X *= 1.3f;
                Main.dust[num665].velocity.X += npc.velocity.X * 0.4f;
                Main.dust[num665].velocity.Y += 2f + npc.velocity.Y;
                for (int num667 = 0; num667 < 2; num667++) {
                    num665 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y + 120f), npc.width, 60, 5, npc.velocity.X, npc.velocity.Y, 0, default(Color), 2f);
                    Main.dust[num665].noGravity = true;
                    Dust dust34 = Main.dust[num665];
                    Dust dust81 = dust34;
                    dust81.velocity -= npc.velocity;
                    Main.dust[num665].velocity.Y += 5f;
                }
            }


        }
    }
}
