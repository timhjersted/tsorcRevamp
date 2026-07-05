using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Rise -> pause-at-apex -> accelerate-home fireball. Shared by any Owl Father fire-volley
    /// attack that wants the "goes up above the target, waits, then commits" read (at least 3 of
    /// the planned 6 ranged attacks use this trait). Initial velocity (rise direction/speed) is set
    /// by whoever spawns it — this projectile only manages the phase transitions from there.
    ///
    /// Phase + phase-elapsed-ticks live in Projectile.ai[0]/ai[1] (both vanilla-networked) so no
    /// extra sync plumbing is needed. The homing direction is captured directly into
    /// Projectile.velocity at the pause->home transition and re-normalized each tick while only its
    /// magnitude ramps up — so no additional stored state is needed for that either.
    /// </summary>
    public class EnemyGreatFireAxeFireball : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Melee/Axes/AncientFireAxeFireball";

        // Public so a future attack variant can spawn this with different tuning.
        public float RiseHeightAboveTarget = 140f;
        public int ApexPauseTicks = 90;
        public float HomingStartSpeed = 2f;
        public float HomingAcceleration = 0.15f;
        public float HomingMaxSpeed = 9f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 66;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.8f;
            Projectile.alpha = 100;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            int phase = (int)Projectile.ai[0];
            Projectile.ai[1] += 1f;

            Player closest = FindClosestPlayer();

            switch (phase)
            {
                case 0: // Rising — coasts on whatever velocity it was spawned with.
                    if (closest != null && Projectile.Center.Y <= closest.Center.Y - RiseHeightAboveTarget)
                    {
                        Projectile.velocity = Vector2.Zero;
                        Projectile.ai[0] = 1f;
                        Projectile.ai[1] = 0f;
                    }
                    break;

                case 1: // Paused at apex.
                    Projectile.velocity = Vector2.Zero;
                    if (Projectile.ai[1] >= ApexPauseTicks)
                    {
                        Vector2 dir = closest != null ? closest.Center - Projectile.Center : new Vector2(0f, 1f);
                        if (dir == Vector2.Zero) dir = new Vector2(0f, 1f);
                        Projectile.velocity = Vector2.Normalize(dir) * HomingStartSpeed;
                        Projectile.ai[0] = 2f;
                        Projectile.ai[1] = 0f;
                    }
                    break;

                case 2: // Homing — direction was locked the instant the pause ended; only speed ramps.
                    if (Projectile.velocity != Vector2.Zero)
                    {
                        float speed = System.Math.Min(Projectile.velocity.Length() + HomingAcceleration, HomingMaxSpeed);
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * speed;
                    }
                    break;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Projectile.light * 0.4f, Projectile.light * 0.1f, Projectile.light * 1f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                    Projectile.velocity * 0.15f, 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                Dust.NewDustPerfect(Projectile.Center, DustID.Torch, velocity, 100, default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;
            }
        }

        private Player FindClosestPlayer()
        {
            Player closest = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead) continue;
                float d = Vector2.DistanceSquared(p.Center, Projectile.Center);
                if (d < bestDistSq) { bestDistSq = d; closest = p; }
            }
            return closest;
        }
    }
}
