using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // "Boomerang Crescent": arcs out in a wide curling path (continuously rotating its own
    // velocity), then switches to homing back toward the OWNER NPC's CURRENT position - a second,
    // separately-timed threat on the return leg for anyone who doesn't reposition after the first
    // pass. Reuses AbyssSlash's crescent sprite (same texture, different ModProjectile/behavior).
    //
    // ai[0] = curve direction (+1 curls one way, -1 the other). ai[1] = owner NPC's whoAmI (so the
    // return leg can re-target wherever the boss currently is, not just where it was at launch).
    class BoomerangCrescent : ModProjectile
    {
        const float Speed = 7f;
        const float CurveRatePerTick = 0.05f;
        const int OutboundArcTicks = 45;
        const float CatchDistance = 50f;
        const float ReturnHomingRate = 0.10f;

        float CurveDir => Projectile.ai[0];
        int OwnerIndex => (int)Projectile.ai[1];

        bool _returning;
        int _turnFlashTimer;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSlash";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.scale = 0.55f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.65f;
            Projectile.timeLeft = 280;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.15f, 0.85f));
            Animate();

            Projectile.localAI[0]++;
            float elapsed = Projectile.localAI[0];

            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;

            if (!_returning)
            {
                // Continuously rotating velocity traces the wide curling "out past the player" arc.
                Projectile.velocity = Projectile.velocity.RotatedBy(CurveRatePerTick * CurveDir);
                if (elapsed >= OutboundArcTicks)
                {
                    _returning = true;
                    _turnFlashTimer = 12;
                }
            }
            else if (owner != null && owner.active)
            {
                Vector2 toOwner = owner.Center - Projectile.Center;
                Vector2 desired = toOwner.SafeNormalize(Vector2.UnitY) * Speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, ReturnHomingRate);

                if (toOwner.Length() < CatchDistance)
                {
                    Projectile.Kill();
                }
            }

            Projectile.rotation += 0.18f * CurveDir;

            if (_turnFlashTimer > 0)
            {
                _turnFlashTimer--;
            }

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.1f, 100, tint, 1.1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            ArtoriasVFX.DrawProjectileTrail(Projectile.Center - direction * 42f, direction.ToRotation(),
                new Vector2(96f, 32f), _returning ? 1f : 0.24f, 0.74f);
            ArtoriasVFX.DrawCrescent(Projectile.Center, Projectile.rotation,
                new Vector2(72f, 64f), _returning, 0.96f);
            if (_turnFlashTimer > 0)
            {
                float progress = 1f - _turnFlashTimer / 12f;
                ArtoriasVFX.DrawTransitionFlash(Projectile.Center, Vector2.One * 84f, progress, 0.82f);
            }
            return false;
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 140, 255, 220);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, tint, 1f);
                d.noGravity = true;
            }
        }
    }
}
