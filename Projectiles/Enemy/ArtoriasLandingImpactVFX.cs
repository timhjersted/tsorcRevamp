using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Short-lived, non-damaging ground fracture for Artorias's physical landings.</summary>
    class ArtoriasLandingImpactVFX : ModProjectile
    {
        const int Lifetime = 18;

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = 1f - MathHelper.Clamp((progress - 0.58f) / 0.42f, 0f, 1f);
            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            ArtoriasVFX.DrawGroundRift(floor, new Vector2(Projectile.ai[0], 34f), progress, 0.70f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, Projectile.ai[1] * 0.45f),
                new Vector2(Projectile.ai[0] * 0.55f, Projectile.ai[1]), progress, 0.48f * fade);
            return false;
        }
    }
}
