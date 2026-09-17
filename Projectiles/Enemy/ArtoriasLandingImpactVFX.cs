using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Short-lived ground fracture for Artorias's physical landings. ai[0] = footprint width, ai[1] = eruption
    /// height (px). Spawned with damage 0 it is cosmetic (Ground Pound, Flip Slash); with damage > 0 (Jump Slash) it is
    /// also a hitbox covering exactly the two eruption columns it draws.</summary>
    class ArtoriasLandingImpactVFX : ModProjectile
    {
        const int Lifetime = 18;
        // The visual starts fading at progress 0.58 (see PreDraw); damage stops there so fading wisps never hit.
        const float DamageStartProgress = 0.05f;
        const float DamageEndProgress = 0.58f;
        // The eruption shader's rise mask reveals a pixel once Progress >= (height fraction from the bottom) * 0.72.
        const float EruptionRiseProgress = 0.72f;
        // The eruption's solid core spans |uv.x - 0.5| < 0.39 of its quad (ArtoriasAbyssEruption.fx exactCore).
        const float EruptionCoreHalfWidth = 0.39f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

        public override bool? CanDamage()
        {
            if (Projectile.damage <= 0)
            {
                return false;
            }

            float progress = Progress;
            return progress >= DamageStartProgress && progress <= DamageEndProgress;
        }

        // Two rising rectangles: the tall centre column and the wide low one PreDraw layers over it. Each is its quad's
        // core width, growing up from the quad's bottom edge as the shader's rise mask reveals it.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float width = Projectile.ai[0];
            float height = Projectile.ai[1];
            float floorY = Projectile.Center.Y - 5f;
            float riseFraction = MathHelper.Clamp(Progress / EruptionRiseProgress, 0f, 1f);

            // Tall column: quad centre floor - 0.45h, size 0.62w x 1.12h.
            float tallQuadBottom = floorY - height * 0.45f + height * 0.56f;
            float tallHalfWidth = width * 0.62f * EruptionCoreHalfWidth;
            float tallHeight = height * 1.12f * riseFraction;
            Rectangle tallColumn = new Rectangle((int)(Projectile.Center.X - tallHalfWidth), (int)(tallQuadBottom - tallHeight),
                (int)(tallHalfWidth * 2f), (int)tallHeight);

            // Wide column: quad centre floor - 0.30h, size 0.92w x 0.72h.
            float wideQuadBottom = floorY - height * 0.30f + height * 0.36f;
            float wideHalfWidth = width * 0.92f * EruptionCoreHalfWidth;
            float wideHeight = height * 0.72f * riseFraction;
            Rectangle wideColumn = new Rectangle((int)(Projectile.Center.X - wideHalfWidth), (int)(wideQuadBottom - wideHeight),
                (int)(wideHalfWidth * 2f), (int)wideHeight);

            return tallColumn.Intersects(targetHitbox) || wideColumn.Intersects(targetHitbox);
        }

        public override void AI()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            // Motes scatter across the whole footprint, so a 3x impact still reads as one burst, not a thin puff.
            float halfFootprint = Projectile.ai[0] * 0.44f;
            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            int type = Main.rand.NextBool(5) ? DustID.SilverFlame
                : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.PurpleTorch;
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.8f, 3.8f),
                Main.rand.NextFloat(-6.4f, -1.2f));
            Dust dust = Dust.NewDustPerfect(floor + new Vector2(Main.rand.NextFloat(-halfFootprint, halfFootprint), 0f),
                type, velocity, 90, new Color(150, 46, 218), Main.rand.NextFloat(0.72f, 1.18f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = Progress;
            float fade = 1f - MathHelper.Clamp((progress - 0.58f) / 0.42f, 0f, 1f);
            Vector2 floor = Projectile.Center - new Vector2(0f, 5f);
            // Rift thickness is half the eruption height: 34px at the default 68 height, so cosmetic callers look unchanged.
            ArtoriasVFX.DrawGroundRift(floor, new Vector2(Projectile.ai[0], Projectile.ai[1] * 0.5f), progress, 0.70f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, Projectile.ai[1] * 0.45f),
                new Vector2(Projectile.ai[0] * 0.62f, Projectile.ai[1] * 1.12f), progress, 0.72f * fade);
            ArtoriasVFX.DrawEruption(floor - new Vector2(0f, Projectile.ai[1] * 0.30f),
                new Vector2(Projectile.ai[0] * 0.92f, Projectile.ai[1] * 0.72f),
                progress, 0.42f * fade);
            return false;
        }
    }
}
