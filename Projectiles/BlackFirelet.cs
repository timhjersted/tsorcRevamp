using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles
{
    class BlackFirelet : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Black Fire");
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.alpha = 100;
            Projectile.timeLeft = 200;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.knockBack = 4;
        }

        public override void AI()
        {

            for (int i = 0; i < 2; i++)
            {
                int num43 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 2f);
                Main.dust[num43].noGravity = true;
                Dust dust1 = Main.dust[num43];
                dust1.velocity.X *= 0.3f;
                dust1.velocity.Y *= 0.3f;
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Dust dust2 = Main.dust[dust];
                dust2.velocity.X *= 0.3f;
                dust2.velocity.Y *= 0.3f;
            }
            Projectile.ai[1] += 1f;

            if (Projectile.ai[1] >= 20f)
            {
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
                return;
            }
        }

        public override bool OnTileCollide(Vector2 CollideVel)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= 3f)
            {
                Projectile.position += Projectile.velocity;
                Projectile.Kill();
            }
            else
            {
                if (Projectile.velocity.Y > 4f)
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y * 0.8f;
                    }
                }
                else
                {
                    if (Projectile.velocity.Y != CollideVel.Y)
                    {
                        Projectile.velocity.Y = -CollideVel.Y;
                    }
                }
                if (Projectile.velocity.X != CollideVel.X)
                {
                    Projectile.velocity.X = -CollideVel.X;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(ModContent.BuffType<DarkInferno>(), 240);
            }
        }
    }
}
