using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // The "delayed homing bolt": flies dead straight on its initial heading, then over a short
    // curve window bends onto a heading toward the player's CURRENT position (not where they were
    // at launch) and continues on from there - looks like it's going to miss, then bends.
    //
    // ai[0] = ticks of straight travel before the curve begins. ai[1] = duration of the curve
    // window itself. Both are set per-spawn (not fixed constants) so multi-orb patterns can
    // stagger, synchronize, or otherwise control WHEN each individual orb bends.
    class HomingAbyssOrb : ModProjectile
    {
        const float Speed = 8.5f;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/HomingAbyssOrb";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.7f;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            Animate();

            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];

            Projectile.localAI[0]++;
            float elapsed = Projectile.localAI[0];

            if (elapsed > straightTicks && elapsed <= straightTicks + curveTicks)
            {
                Player target = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];
                if (target.active && !target.dead)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Speed;
                    float curveT = (elapsed - straightTicks) / curveTicks;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.06f, 0.30f, curveT));
                }
            }

            Projectile.rotation += 0.15f;

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                bool white = Main.rand.NextBool(5);
                Color tint = white ? Color.White : new Color(190, 90, 255);
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    -Projectile.velocity * 0.1f, 110, tint, 0.9f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float straightTicks = Projectile.ai[0];
            float curveTicks = Projectile.ai[1] <= 0 ? 16f : Projectile.ai[1];
            float elapsed = Projectile.localAI[0];
            float warning = straightTicks > 0f
                ? MathHelper.Clamp((elapsed - (straightTicks - 10f)) / 10f, 0f, 1f)
                : 1f;
            float steering = MathHelper.Clamp((elapsed - straightTicks) / curveTicks, 0f, 1f);
            float state = System.Math.Max(warning * (1f - steering), steering);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (steering > 0.05f && Projectile.oldPos[6] != Vector2.Zero)
            {
                Vector2 oldStart = Projectile.oldPos[6] + Projectile.Size * 0.5f;
                Vector2 oldEnd = Projectile.oldPos[2] + Projectile.Size * 0.5f;
                Vector2 oldDelta = oldEnd - oldStart;
                if (oldDelta.LengthSquared() > 4f)
                {
                    ArtoriasVFX.DrawProjectileTrail(Vector2.Lerp(oldStart, oldEnd, 0.5f),
                        oldDelta.ToRotation(), new Vector2(oldDelta.Length() + 18f, 24f),
                        state, 0.48f);
                }
            }
            ArtoriasVFX.DrawProjectileTrail(Projectile.Center - direction * 38f, direction.ToRotation(),
                new Vector2(88f, 28f), state, 0.72f);
            ArtoriasVFX.DrawOrb(Projectile.Center, new Vector2(48f, 48f), Projectile.rotation, state, 0.94f);
            if (warning > 0f && steering <= 0f)
            {
                ArtoriasVFX.DrawTransitionFlash(Projectile.Center, Vector2.One * 62f,
                    warning, 0.72f * warning);
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
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, tint, 1f);
                d.noGravity = true;
            }
        }
    }
}
