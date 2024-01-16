using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic;

namespace tsorcRevamp.Projectiles.Magic
{
    class ApprenticesWandFireball : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.penetrate = 8;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.width = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
            for (int num88 = 0; num88 < 2; num88++)
            {
                int num89 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
                if (Projectile.type == 258 && Main.getGoodWorld)
                {
                    Main.dust[num89].noLight = true;
                }
                Main.dust[num89].noGravity = true;
                Main.dust[num89].velocity.X *= 0.3f;
                Main.dust[num89].velocity.Y *= 0.3f;
            }
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] >= 20f)
            {
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, ApprenticesWand.OnFireDuration * 60);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(in SoundID.Item10, Projectile.position);
            Projectile.ai[0] += 1f;
            int num44 = 5;
            if (Projectile.ai[0] >= (float)num44)
            {
                Projectile.position += Projectile.velocity;
                Projectile.Kill();
            }
            else
            {
                if (Projectile.velocity.Y != oldVelocity.Y)
                {
                    Projectile.velocity.Y = 0f - oldVelocity.Y;
                }
                if (Projectile.velocity.X != oldVelocity.X)
                {
                    Projectile.velocity.X = 0f - oldVelocity.X;
                }
            }
            return false;
        }
    }
}
