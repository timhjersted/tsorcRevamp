using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>A puff of abyss cloud shed from Artorias's blade during the Piercing Dash opener's flip, or thrown up out of the
    /// floor by Ground Pound's landing eruption. It drifts up and thins out, and is a real hitbox while it does: the flip with
    /// the sword out is an attack. Drawn with the same mantle shader as his other abyss effects. Spawned server-side only;
    /// every machine runs its motion and draw.
    /// ai[0] = ticks to wait, invisible and harmless and held still, before it forms (0 = form at once, moving at its spawn
    /// velocity). ai[1] = upward speed it starts rising at when that wait ends, for a cloud spawned at rest.</summary>
    class ArtoriasCurseCloud : ModProjectile
    {
        const int Lifetime = 75;
        const int EmergeTicks = 6;           // opacity and hitbox ramp up over the first EmergeTicks, so a cloud never pops in solid
        const int FadeTicks = 25;            // opacity and hitbox ramp down over the last FadeTicks; damage stops with it
        const float RiseDrag = 0.985f;       // per tick: the initial upward shove bleeds off into a slow drift
        const float HitRadius = 34f;         // px at full size; the drawn cloud is 2x this across
        const float DrawSize = 96f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        bool WaitingToForm => Projectile.ai[0] > 0f;

        float Life01 => 1f - Projectile.timeLeft / (float)Lifetime;

        // 1 while solid, 0 at either end of the cloud's life: it emerges over EmergeTicks and thins out over FadeTicks.
        float Solidity
        {
            get
            {
                float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
                float fadeIn = MathHelper.Clamp((Lifetime - Projectile.timeLeft) / (float)EmergeTicks, 0f, 1f);
                return MathHelper.Min(fadeIn, fadeOut);
            }
        }

        public override void AI()
        {
            if (WaitingToForm)
            {
                // Hold still with a full lifetime (AI runs, then timeLeft ticks down, so +1 nets zero). The tick the wait
                // ends the cloud starts its rise, so a row of them erupts one after another.
                Projectile.ai[0]--;
                Projectile.timeLeft++;
                if (Projectile.ai[0] <= 0f)
                {
                    Projectile.velocity = new Vector2(0f, -Projectile.ai[1]);
                }

                return;
            }

            Projectile.velocity *= RiseDrag;

            Lighting.AddLight(Projectile.Center, new Vector3(0.36f, 0.12f, 0.56f) * Solidity);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2Circular(HitRadius * 0.7f, HitRadius * 0.7f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PurpleTorch, new Vector2(0f, -0.6f), 110,
                    new Color(150, 46, 218), Main.rand.NextFloat(0.8f, 1.2f) * Solidity);
                mote.noGravity = true;
            }
        }

        public override bool? CanDamage()
        {
            return !WaitingToForm && Solidity > 0.05f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Swells a little as it ages, then shrinks with the fade so the last wisps don't hurt.
            float growth = MathHelper.Lerp(0.8f, 1.15f, Life01);
            float radius = HitRadius * growth * Solidity;
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= radius + targetHitbox.Width * 0.25f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (WaitingToForm)
            {
                return false;
            }

            float growth = MathHelper.Lerp(0.8f, 1.15f, Life01);
            float size = DrawSize * growth;
            ArtoriasVFX.DrawMantle(Projectile.Center, new Vector2(size, size), 0.62f * Solidity, 1.0f, -1f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Dust wisp = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(HitRadius, HitRadius), DustID.ShadowbeamStaff,
                    new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-1.4f, -0.4f)), 120,
                    new Color(120, 40, 200), Main.rand.NextFloat(0.7f, 1f));
                wisp.noGravity = true;
            }
        }
    }
}
