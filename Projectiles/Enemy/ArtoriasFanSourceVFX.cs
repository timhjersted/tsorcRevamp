using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Short-lived, non-damaging release knot that visually joins Artorias's blade to each
    /// crescent fired by Boomerang and Spiral Fan. The projectile is server-spawned with the
    /// hostile shot, while every layer here remains purely decorative.
    /// </summary>
    sealed class ArtoriasFanSourceVFX : ModProjectile
    {
        const int Lifetime = 14;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        float ShotAngle => Projectile.ai[0];
        float SpiralDirection => Projectile.ai[1] < 0f ? -1f : 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 120;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.46f, 0.10f, 0.68f));
            if (Main.dedServ)
                return;

            Vector2 direction = ShotAngle.ToRotationVector2();
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 velocity = direction * Main.rand.NextFloat(0.8f, 2.2f)
                + normal * Main.rand.NextFloat(-1.3f, 1.3f);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + normal * Main.rand.NextFloat(-10f, 10f),
                Main.rand.NextBool(5) ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                velocity, 105,
                Main.rand.NextBool(5) ? new Color(226, 218, 255) : new Color(151, 52, 232),
                Main.rand.NextFloat(0.72f, 1.08f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = 1f - progress;
            Vector2 direction = ShotAngle.ToRotationVector2();

            // A compact rotating knot at the grip, then a torn ribbon that points along the
            // projectile's real launch vector. The wide pieces fade before they can resemble
            // another hostile volume.
            ArtoriasVFX.DrawBoomerangOrbit(Projectile.Center,
                Vector2.One * MathHelper.Lerp(54f, 86f, progress),
                ShotAngle + progress * SpiralDirection * 1.4f,
                returning: true, curveDirection: SpiralDirection, opacity: 0.92f * fade);
            ArtoriasVFX.DrawBoomerangRibbon(
                Projectile.Center + direction * MathHelper.Lerp(14f, 30f, progress),
                ShotAngle + MathHelper.PiOver2, new Vector2(42f, 92f),
                returning: true, curveDirection: SpiralDirection, opacity: 0.96f * fade);
            return false;
        }
    }
}
