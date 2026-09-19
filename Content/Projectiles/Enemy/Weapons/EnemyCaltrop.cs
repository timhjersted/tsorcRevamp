using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class EnemyCaltrop : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Items.Weapons.Enemy.EnemyCaltrop));

        private const float Gravity = 0.22f;
        private const float ThrowSpeed = 8f;
        private const int StuckLifetime = 12 * 60;
        private const int SpreadCount = 6;
        private const float SpreadDegrees = 26f;
        private const int FadeOutTicks = 60;

        private bool IsStuck => Projectile.ai[0] == 1f;
        private bool IsFading => Projectile.timeLeft <= FadeOutTicks;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.knockBack = 1.5f;
        }

        /// <summary>Throws the full caltrop fan and returns each spawned projectile slot, so an
        /// encounter can deliberately retire its own persistent hazards when it despawns.</summary>
        public static int[] ThrowSpread(IEntitySource source, Vector2 origin, Vector2 target, int damage, float knockback, int owner)
        {
            Vector2 baseVelocity = UsefulFunctions.BallisticTrajectory(origin, target, ThrowSpeed, Gravity, highAngle: false, fallback: true);
            if (baseVelocity == Vector2.Zero)
            {
                baseVelocity = target - origin;
                if (baseVelocity == Vector2.Zero)
                {
                    baseVelocity = Vector2.UnitX;
                }
                baseVelocity.Normalize();
                baseVelocity *= ThrowSpeed;
            }

            float startAngle = -SpreadDegrees * 0.5f;
            float step = SpreadDegrees / (SpreadCount - 1);
            int[] projectileSlots = new int[SpreadCount];
            for (int i = 0; i < SpreadCount; i++)
            {
                float angle = MathHelper.ToRadians(startAngle + step * i);
                Vector2 velocity = baseVelocity.RotatedBy(angle) * Main.rand.NextFloat(0.92f, 1.08f);
                projectileSlots[i] = Projectile.NewProjectile(
                    source,
                    origin,
                    velocity,
                    ModContent.ProjectileType<EnemyCaltrop>(),
                    damage,
                    knockback,
                    owner);
            }

            return projectileSlots;
        }

        public override void AI()
        {
            if (IsFading)
            {
                // The hazard expires visibly instead of popping out. Disable the collision contract at
                // the FIRST fade tick, so all 60 ticks are cosmetic and safe to walk through.
                Projectile.hostile = false;
                Projectile.damage = 0;
                Projectile.knockBack = 0f;
                Projectile.tileCollide = false;

                float fadeProgress = 1f - Projectile.timeLeft / (float)FadeOutTicks;
                Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, fadeProgress);
                Projectile.scale = MathHelper.Lerp(1f, 0.55f, fadeProgress);

                if (Main.rand.NextBool(6))
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                        DustID.Smoke, 0f, -0.35f, 175, default, 0.45f + fadeProgress * 0.35f);
                    dust.noGravity = true;
                }
                return;
            }

            if (IsStuck)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = Projectile.ai[1];

                if (Main.rand.NextBool(20))
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 160, default(Color), 0.55f);
                }
                return;
            }

            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            Projectile.rotation += Projectile.velocity.X * 0.12f;
        }

        public override bool? CanDamage() => IsFading ? false : null;

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A small final puff completes both the natural 60-tick fade and an explicit
            // encounter cleanup (for example, a Black Ninja party-wipe despawn).
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Smoke, Main.rand.NextFloat(-0.9f, 0.9f), Main.rand.NextFloat(-1.2f, -0.2f),
                    170, default, Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StickToSurface(oldVelocity);
            return false;
        }


        private void StickToSurface(Vector2 oldVelocity)
        {
            if (IsStuck)
            {
                return;
            }

            bool collidedX = Projectile.velocity.X != oldVelocity.X;
            bool collidedY = Projectile.velocity.Y != oldVelocity.Y;

            Projectile.ai[0] = 1f;
            Projectile.ai[1] = Projectile.rotation;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = StuckLifetime;
            Projectile.netUpdate = true;

            if (collidedX)
            {
                Projectile.position.X += MathHelper.Clamp(oldVelocity.X, -1f, 1f) * 5f;
            }
            if (collidedY)
            {
                Projectile.position.Y += MathHelper.Clamp(oldVelocity.Y, -1f, 1f) * 5f;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, PitchVariance = 0.35f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, oldVelocity.X * 0.1f, oldVelocity.Y * 0.1f, 160, default(Color), 0.7f);
            }
        }
    }
}
