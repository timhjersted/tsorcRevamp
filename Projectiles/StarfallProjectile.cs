using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class StarfallProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.8f;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;

            if (Projectile.position.Y > Main.player[Projectile.owner].position.Y - 50f) Projectile.tileCollide = true;

            UsefulFunctions.HomeOnEnemy(Projectile, 240, 15f, false, 1.25f, true);


            int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 92, Projectile.velocity.X, Projectile.velocity.Y, 128, default, 1.2f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.3f;

        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            for (int i = 0; i < 4; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width * 2, Projectile.height * 2, 92, 0, 0, 50, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
