using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    public class DevilSickle : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.scale = 0.9f;
            Projectile.alpha = 100;
            Projectile.timeLeft = 500;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
            int dust = Dust.NewDust(
            Projectile.position,
            Projectile.width,
            Projectile.height,
            6,
            Projectile.velocity.X,
            Projectile.velocity.Y,
            90,
            default,
            1.8f
            );
            Main.dust[dust].noGravity = true;
            Projectile.rotation += Projectile.direction * 0.8f;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0f);

            if (Projectile.velocity.X <= 12f && Projectile.velocity.Y <= 12f && Projectile.velocity.X >= -12f && Projectile.velocity.Y >= -12f)
            {
                Projectile.velocity.X *= 1.05f;
                Projectile.velocity.Y *= 1.05f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.OnFire3, 5 * 60); //420 blaze it 
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(BuffID.OnFire, 7 * 60);
            }
        }
    }
}
