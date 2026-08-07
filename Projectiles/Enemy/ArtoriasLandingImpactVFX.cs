using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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

        public override void AI()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            int type = Main.rand.NextBool(5) ? DustID.SilverFlame
                : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.PurpleTorch;
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.8f, 3.8f),
                Main.rand.NextFloat(-6.4f, -1.2f));
            Dust dust = Dust.NewDustPerfect(floor + new Vector2(Main.rand.NextFloat(-38f, 38f), 0f),
                type, velocity, 90, new Color(150, 46, 218), Main.rand.NextFloat(0.72f, 1.18f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = 1f - MathHelper.Clamp((progress - 0.58f) / 0.42f, 0f, 1f);
            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            ArtoriasVFX.DrawGroundRift(floor, new Vector2(Projectile.ai[0], 34f), progress, 0.70f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, Projectile.ai[1] * 0.45f),
                new Vector2(Projectile.ai[0] * 0.62f, Projectile.ai[1] * 1.12f), progress, 0.72f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, Projectile.ai[1] * 0.30f),
                new Vector2(Projectile.ai[0] * 0.92f, Projectile.ai[1] * 0.72f),
                progress, 0.42f * fade);
            return false;
        }
    }
}
