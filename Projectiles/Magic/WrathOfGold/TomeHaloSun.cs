using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Wrath of Gold left hold: a small sun that joins a halo orbiting the caster, then detaches
    ///after its orbit time and hunts the nearest enemy with lazy homing. ai[0] = orbit slot (0-5,
    ///spaces the halo). Harmless while orbiting.
    ///</summary>
    class TomeHaloSun : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float OrbitRadius = 52f;
        const float OrbitSpeed = 0.06f;
        const int OrbitTicks = 45;
        const float LaunchSpeed = 9f;

        float SlotAngle => Projectile.ai[0] / 6f * MathHelper.TwoPi;
        bool Detached => Projectile.localAI[0] > OrbitTicks;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.5f;
        }

        public override bool? CanDamage()
        {
            return Detached;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Player player = Main.player[Projectile.owner];

            if (!Detached)
            {
                if (!player.active || player.dead)
                {
                    Projectile.Kill();
                    return;
                }
                float angle = SlotAngle + Projectile.localAI[0] * OrbitSpeed;
                Projectile.Center = player.Center + new Vector2(0f, -50f) + angle.ToRotationVector2() * OrbitRadius;
                Projectile.velocity = Vector2.Zero;

                if (Projectile.localAI[0] >= OrbitTicks)
                {
                    //Detach: hunt the nearest valid enemy, or fly out along the orbit tangent
                    NPC target = FindTarget();
                    Vector2 dir = target != null
                        ? (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX)
                        : (angle + MathHelper.PiOver2).ToRotationVector2();
                    Projectile.velocity = dir * LaunchSpeed;
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = 240;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.4f, Pitch = 0.6f }, Projectile.Center);
                }
            }
            else
            {
                NPC target = FindTarget();
                if (target != null)
                {
                    UsefulFunctions.SmoothHoming(Projectile, target.Center, 0.08f, LaunchSpeed, target.velocity, false);
                }
            }

            //The sun: bright golden core with drifting flame
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 80, default, 1.5f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.15f;
            if (Main.rand.NextBool(3))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, -0.5f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.55f, 0.47f, 0.2f);
        }

        NPC FindTarget()
        {
            NPC best = null;
            float bestDist = 800f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                {
                    continue;
                }
                float dist = Vector2.Distance(npc.Center, Projectile.Center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
