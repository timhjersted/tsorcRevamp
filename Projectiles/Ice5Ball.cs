﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class Ice5Ball : ModProjectile {

        public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.width = 16;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override void AI() {
            if (Projectile.soundDelay == 0 && Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f) {
                Projectile.soundDelay = 10;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
            }
            Vector2 arg_2675_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
            int arg_2675_1 = Projectile.width;
            int arg_2675_2 = Projectile.height;
            int arg_2675_3 = 15;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = Projectile.position.X + (float)(Projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = Projectile.position.Y + (float)(Projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;
            if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 0f) {
                if (Main.player[Projectile.owner].channel) {
                    float num48 = 12f;
                    Vector2 vector6 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                    float num49 = (float)Main.mouseX + Main.screenPosition.X - vector6.X;
                    float num50 = (float)Main.mouseY + Main.screenPosition.Y - vector6.Y;
                    float num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    if (num51 > num48) {
                        num51 = num48 / num51;
                        num49 *= num51;
                        num50 *= num51;
                        int num52 = (int)(num49 * 1000f);
                        int num53 = (int)(Projectile.velocity.X * 1000f);
                        int num54 = (int)(num50 * 1000f);
                        int num55 = (int)(Projectile.velocity.Y * 1000f);
                        if (num52 != num53 || num54 != num55) {
                            Projectile.netUpdate = true;
                        }
                        Projectile.velocity.X = num49;
                        Projectile.velocity.Y = num50;
                    }
                    else {
                        int num56 = (int)(num49 * 1000f);
                        int num57 = (int)(Projectile.velocity.X * 1000f);
                        int num58 = (int)(num50 * 1000f);
                        int num59 = (int)(Projectile.velocity.Y * 1000f);
                        if (num56 != num57 || num58 != num59) {
                            Projectile.netUpdate = true;
                        }
                        Projectile.velocity.X = num49;
                        Projectile.velocity.Y = num50;
                    }
                }
                else {
                    if (Projectile.ai[0] == 0f) {
                        Projectile.ai[0] = 1f;
                        Projectile.netUpdate = true;
                        float num60 = 12f;
                        Vector2 vector7 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                        float num61 = (float)Main.mouseX + Main.screenPosition.X - vector7.X;
                        float num62 = (float)Main.mouseY + Main.screenPosition.Y - vector7.Y;
                        float num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
                        num63 = num60 / num63;
                        num61 *= num63;
                        num62 *= num63;
                        Projectile.velocity.X = num61;
                        Projectile.velocity.Y = num62;
                        if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f) {
                            Projectile.Kill();
                        }
                    }
                }
            }

            if (Projectile.velocity.X != 0f || Projectile.velocity.Y != 0f) {
                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) - 2.355f;
            }

            if (Projectile.velocity.Y > 16f) {
                Projectile.velocity.Y = 16f;
                return;
            }
        }

        public override void Kill(int timeLeft) {
            if (!Projectile.active) {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width), Projectile.position.Y + (float)(Projectile.height), 0, 5, ModContent.ProjectileType<Ice5Icicle>(), (int)(this.Projectile.damage), 3f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * 4), Projectile.position.Y + (float)(Projectile.height * 2), 0, 5, ModContent.ProjectileType<Ice5Icicle>(), (int)(this.Projectile.damage), 3f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * -2), Projectile.position.Y + (float)(Projectile.height * 2), 0, 5, ModContent.ProjectileType<Ice5Icicle>(), (int)(this.Projectile.damage), 3f, Projectile.owner);
                Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1394_1 = Projectile.width;
                int arg_1394_2 = Projectile.height;
                int arg_1394_3 = 15;
                float arg_1394_4 = 0f;
                float arg_1394_5 = 0f;
                int arg_1394_6 = 100;
                Color newColor = default(Color);
                int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
                Main.dust[num41].noGravity = true;
                Dust expr_13B1 = Main.dust[num41];
                expr_13B1.velocity *= 2f;
                Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1422_1 = Projectile.width;
                int arg_1422_2 = Projectile.height;
                int arg_1422_3 = 15;
                float arg_1422_4 = 0f;
                float arg_1422_5 = 0f;
                int arg_1422_6 = 100;
                Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, default, 1f);
                
            }
            if (Projectile.owner == Main.myPlayer) {
                if (Main.netMode != NetmodeID.SinglePlayer) {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, Projectile.identity, (float)Projectile.owner, 0f, 0f, 0);
                }
                int num98 = -1;
                if (Projectile.aiStyle == 10) {
                    int num99 = (int)(Projectile.position.X + (float)(Projectile.width / 2)) / 16;
                    int num100 = (int)(Projectile.position.Y + (float)(Projectile.width / 2)) / 16;
                    int num101 = 0;
                    int num102 = 2;
                    if (!Main.tile[num99, num100].HasTile) {
                        WorldGen.PlaceTile(num99, num100, num101, false, true, -1, 0);
                        if (Main.tile[num99, num100].HasTile && (int)Main.tile[num99, num100].TileType == num101) {
                            NetMessage.SendData(MessageID.TileChange, -1, -1, null, 1, (float)num99, (float)num100, (float)num101, 0);
                        }
                        else {
                            if (num102 > 0) {
                                num98 = Item.NewItem((int)Projectile.position.X, (int)Projectile.position.Y, Projectile.width, Projectile.height, num102, 1, false, 0);
                            }
                        }
                    }
                    else {
                        if (num102 > 0) {
                            num98 = Item.NewItem((int)Projectile.position.X, (int)Projectile.position.Y, Projectile.width, Projectile.height, num102, 1, false, 0);
                        }
                    }
                }
                if (Main.netMode == NetmodeID.MultiplayerClient && num98 >= 0) {
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, num98, 0f, 0f, 0f, 0);
                }
            }
            Projectile.active = false;
        }

    }
}
