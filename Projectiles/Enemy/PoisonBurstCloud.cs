using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Eland's death burst: modeled on Okiku's PoisonSmog (Projectiles/Enemy/Okiku/PoisonSmog.cs) — same
    // drifting/accelerating movement and launch speed — but at double the size, and without Okiku's
    // Tipsy debuff (Poisoned only).
    public class PoisonBurstCloud : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults()
        {
            Projectile.width = 32;  // double PoisonSmog's 16
            Projectile.height = 32; // double PoisonSmog's 16
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Projectile.rotation += 0.1f;
            // Was DustID 6 (Torch - a FIRE dust) tinted green at scale 3.0 with zero velocity: huge,
            // static, and reading as a fire puff rather than poison. Now smaller, actually poison,
            // and given drift so the cloud churns.
            if (Main.rand.NextBool(3))
            {
                Dust drip = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    DustID.Poisoned, Main.rand.NextVector2Circular(0.35f, 0.35f) + new Vector2(0f, -0.18f),
                    100, default, Main.rand.NextFloat(0.85f, 1.35f));
                drip.noGravity = true;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            // Same speed/accel curve as PoisonSmog: drifts outward, accelerating until it caps at 4.
            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Was a hardcoded 0.45f progress with active:false, so it never faded out and rendered at
            // telegraph brightness despite being a real damaging cloud. Real progress over its 600t
            // life now drives the shader's hold-then-fade curve.
            float progress = 1f - Projectile.timeLeft / 600f;
            // Smaller (36 -> 26) so the gas hugs the sprite instead of ballooning past it, and rotated
            // by the projectile's own spin so the shader's noise churns with the cloud rather than
            // sitting frozen behind a rotating sprite.
            EnemyVFX.DrawElandToxicField(Projectile.Center, Vector2.One * 26f, progress, true,
                Projectile.rotation);
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 12 * 60, false);
        }
    }
}
