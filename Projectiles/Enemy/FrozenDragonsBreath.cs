using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    public class FrozenDragonsBreath : ModProjectile
    {



        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.timeLeft = 3600;
            Projectile.hostile = true;
            Projectile.penetrate = 1; //was 3
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

            AIType = 4;
        }



        public override void AI()
        {
            if (Projectile.timeLeft > 60)


            {
                Projectile.timeLeft = 60;
            }
            if (Projectile.ai[0] > 7f)
            {
                float num152 = 1f;
                if (Projectile.ai[0] == 8f)
                {
                    num152 = 0.25f;
                }
                else
                {
                    if (Projectile.ai[0] == 9f)
                    {
                        num152 = 0.5f;
                    }
                    else
                    {
                        if (Projectile.ai[0] == 10f)
                        {
                            num152 = 0.75f;
                        }
                    }
                }
                Projectile.ai[0] += 1f;
                int num153 = 6;
                if (Projectile.type == ModContent.ProjectileType<FrozenDragonsBreath>())
                {
                    num153 = 76;
                }
                if (num153 == 6 || Main.rand.NextBool(2))
                {
                    for (int num154 = 0; num154 < 1; num154++)
                    {
                        int num155 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, num153, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                        if (Main.rand.Next(3) != 0 || (num153 == 76 && Main.rand.NextBool(3)))
                        {
                            Main.dust[num155].noGravity = true;
                            Main.dust[num155].scale *= 2f; //was 3 but 1.5 too small
                            Dust expr_6767_cp_0 = Main.dust[num155];
                            expr_6767_cp_0.velocity.X = expr_6767_cp_0.velocity.X * 2f;
                            Dust expr_6785_cp_0 = Main.dust[num155];
                            expr_6785_cp_0.velocity.Y = expr_6785_cp_0.velocity.Y * 2f;
                        }
                        Main.dust[num155].scale *= 1.5f; //was 1 but dust too small
                        Dust expr_67BC_cp_0 = Main.dust[num155];
                        expr_67BC_cp_0.velocity.X = expr_67BC_cp_0.velocity.X * 1.2f;
                        Dust expr_67DA_cp_0 = Main.dust[num155];
                        expr_67DA_cp_0.velocity.Y = expr_67DA_cp_0.velocity.Y * 1.2f;
                        Main.dust[num155].scale *= num152;
                        if (num153 == 75)
                        {
                            Main.dust[num155].velocity += Projectile.velocity;
                            if (!Main.dust[num155].noGravity)
                            {
                                Main.dust[num155].velocity *= 0.5f;
                            }
                        }
                    }
                }
            }
            else
            {
                Projectile.ai[0] += 1f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.NextBool(6))
            {
                target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 18000, false); //may lose -100 max HP after taking enough hits. It had 100% trigger before. I think that was the problem.
            }
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.Frozen, 10, false);
            }
            else
            {
                target.AddBuff(BuffID.Frozen, 20, false);
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

    }

}