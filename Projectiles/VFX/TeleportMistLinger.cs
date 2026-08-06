using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    // Stationary dust emitter spawned at teleport exit and entry positions.
    // ai[0]: 0 = grey smoke, 1 = fire
    // ai[1]: scatter radius in pixels (sized to the NPC)
    // timeLeft: set externally to 60 (1s) after spawning
    class TeleportMistLinger : ModProjectile
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
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            // Keep alive for the set duration; timeLeft ticks down automatically.
            if (Main.dedServ)
                return;

            bool isFire = Projectile.ai[0] == 1f;
            float radius = Projectile.ai[1];

            // Emit particles per frame so the cloud fills the NPC footprint. The fire style runs a
            // higher count at a smaller scale than the grey style: the old fire cloud was 5 motes
            // per frame at up to scale 2.25, which at 8px per dust is ~18px blocks and read as a
            // pile of orange squares. Count buys density; scale does not (see vfx-dust-tips).
            int emitCount = isFire ? 7 : 5;
            for (int i = 0; i < emitCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);

                if (isFire)
                {
                    // Alternate fire and dark smoke particles for a fire-cloud look
                    if (Main.rand.NextBool())
                    {
                        // 1.50-2.25 -> 1.05-1.70 (max cut by ~25%). No fadeIn: fadeIn is a GROW-TO
                        // target, and the old `fadeIn = 0.6f` was below the spawn scale, so it was
                        // silently a no-op on every one of these.
                        Dust fire = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch, velocity, 180, new Color(255, 100, 20), Main.rand.NextFloat(1.05f, 1.7f));
                        fire.noGravity = true;
                    }
                    else
                    {
                        // 1.25-1.875 -> 0.95-1.40, and it now genuinely billows: spawn small with
                        // fadeIn ABOVE the spawn scale so the mote grows before it decays.
                        Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, velocity * 0.5f, 200, new Color(60, 40, 30), Main.rand.NextFloat(0.6f, 0.9f));
                        smoke.noGravity = true;
                        smoke.fadeIn = Main.rand.NextFloat(0.95f, 1.4f);
                    }

                    // FOREGROUND: one tiny fast ember every other mote. These are the sharp detail
                    // layer the cloud was missing entirely, and they are what makes the remaining
                    // big motes read as fire rather than as texture.
                    if (Main.rand.NextBool())
                    {
                        Vector2 outward = offset.SafeNormalize(Main.rand.NextVector2Unit());
                        Dust ember = Dust.NewDustPerfect(Projectile.Center + offset * 0.7f,
                            Main.rand.NextBool(4) ? DustID.OrangeTorch : DustID.Torch,
                            outward * Main.rand.NextFloat(1.6f, 4.2f) - Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.1f),
                            120, default, Main.rand.NextFloat(0.4f, 0.8f));
                        ember.noGravity = true;
                    }
                }
                else
                {
                    Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, velocity * 0.4f, 180, new Color(130, 130, 130), Main.rand.NextFloat(1.5f, 2.5f));
                    smoke.noGravity = true;
                    smoke.fadeIn = 0.5f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
