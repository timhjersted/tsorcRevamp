using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ShadowBall : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 15;
            Projectile.penetrate = 4;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true;
            Projectile.width = 15;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.alpha = 50;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 52, Projectile.velocity.X * 0, -4, 100, default, 2.5f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft)
        {
            for (int d = 0; d < 25; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 52, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default(Color), 2.5f);
                Main.dust[dust].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.45f }, Projectile.position);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 52, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default(Color), 2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
