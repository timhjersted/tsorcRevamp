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

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool white = Main.rand.NextBool(5);
                    Color tint = white ? Color.White : (Main.rand.NextBool() ? new Color(190, 90, 255) : Color.DarkViolet);
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                        white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                        -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.18f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        90, tint, Main.rand.NextFloat(0.9f, 1.3f));
                    d.noGravity = true;
                }
            }
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
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, tint, 1f);
                d.noGravity = true;
            }
        }
    }
}
