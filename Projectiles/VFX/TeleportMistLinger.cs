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

            // Emit 5 particles per frame so the cloud fills the NPC footprint
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Vector2 velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);

                if (isFire)
                {
                    // Alternate fire and dark smoke particles for a fire-cloud look
                    if (Main.rand.NextBool())
                    {
                        Dust fire = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch, velocity, 180, new Color(255, 100, 20), Main.rand.NextFloat(1.5f, 2.25f));
                        fire.noGravity = true;
                        fire.fadeIn = 0.6f;
                    }
                    else
                    {
                        Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, velocity * 0.5f, 200, new Color(60, 40, 30), Main.rand.NextFloat(1.25f, 1.875f));
                        smoke.noGravity = true;
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
