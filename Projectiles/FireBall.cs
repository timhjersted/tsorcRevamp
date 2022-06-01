using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireBall : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = 0;
        }
        public override void AI()
        {

            Color color = new Color();
            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 100, color, 1.25f);
                Main.dust[dust].noGravity = true;
            }
            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(4) == 0)
            {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }
        public override void Kill(int timeLeft)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 174, Projectile.velocity.X, Projectile.velocity.Y, 0, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3.WithVolume(.45f), Projectile.position);
        }
    }
}
