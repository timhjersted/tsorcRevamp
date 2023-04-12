using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles
{
    class Freezethrower : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.alpha = 255;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true;
            Projectile.penetrate = 13;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.MaxUpdates = 2;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if ((Main.rand.Next(5)) == 0)
            {
                target.AddBuff(BuffID.Frozen, 120);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(BuffID.Frozen, 2 * 60);
            }
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 80)
            {
                Projectile.timeLeft = 80;
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
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 1; i++)
                    {
                        int num155 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 76, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
                        if (Main.rand.NextBool(3))
                        {
                            Main.dust[num155].noGravity = true;
                            Main.dust[num155].scale *= 3f;
                            Main.dust[num155].velocity *= 2f;
                        }
                        Main.dust[num155].scale *= 1.5f;
                        Main.dust[num155].velocity *= 1.2f;
                        Main.dust[num155].scale *= num152;
                    }
                }
            }
            else
            {
                Projectile.ai[0] += 1f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            return;
        }
    }
}
