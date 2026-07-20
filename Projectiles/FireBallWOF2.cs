using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireBallWOF2 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 13;
            Projectile.height = 13;
            Projectile.scale = 1.05f;
            Projectile.timeLeft = 4000;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.aiStyle = 0;
        }
        public override void AI()
        {
            Projectile.rotation++;
            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }

            Color color = new Color();
            for (int d = 0; d < 4; d++)
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 100, color, 1.5f);
                Main.dust[dust].noGravity = true;
            }
            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire, 300);
            }
            if (Main.rand.NextBool(30))
            {
                target.AddBuff(BuffID.OnFire3, 300);
            }
        }
        public override void OnKill(int timeLeft)
        {
            for (int d = 0; d < 18; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 174, Projectile.velocity.X, Projectile.velocity.Y, 0, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f }, Projectile.position);
        }
    }
}
