using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    // One-shot dust burst spawned at fire-style teleport exit/entry during the telegraph.
    // Emits a ring of smoke and fire particles that expand ~600px outward with a slight
    // spiral drift. The projectile itself is invisible; it just fires the particles.
    class AbyssTeleportBlast : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 4;
        }

        public override void AI()
        {
            if (Main.dedServ)
                return;

            if (Projectile.localAI[0] > 0f)
                return;
            Projectile.localAI[0] = 1f;

            // Smoke ring: 35 evenly-spaced particles with a slight spiral twist
            for (int i = 0; i < 30; i++)
            {
                float angle = MathHelper.TwoPi * i / 30f;
                Vector2 dir = angle.ToRotationVector2();
                // Small tangential component creates a spiral/pinwheel visual as the ring expands
                Vector2 tangent = new Vector2(-dir.Y, dir.X) * Main.rand.NextFloat(-0.15f, 0.15f);
                float speed = Main.rand.NextFloat(9f, 13f);
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center + dir * Main.rand.NextFloat(4f, 18f),
                    DustID.Smoke, (dir + tangent) * speed,
                    65, new Color(145, 115, 92), Main.rand.NextFloat(2.0f, 3.2f));
                smoke.noGravity = true;
                smoke.fadeIn = 0.8f;
            }

            // Fire ring: 25 particles staggered between the smoke spokes
            for (int i = 0; i < 25; i++)
            {
                float angle = MathHelper.TwoPi * i / 20f + (MathHelper.TwoPi / 40f);
                Vector2 dir = angle.ToRotationVector2();
                float speed = Main.rand.NextFloat(11f, 15f);
                Dust fire = Dust.NewDustPerfect(
                    Projectile.Center + dir * Main.rand.NextFloat(2.8f, 15f),
                    DustID.PinkTorch, dir * speed,
                    50, default, Main.rand.NextFloat(1.9f, 3.0f));
                fire.noGravity = true;
                fire.fadeIn = 0.9f;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
