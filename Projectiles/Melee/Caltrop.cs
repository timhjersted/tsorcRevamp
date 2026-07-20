using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee
{
    public class Caltrop : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/Caltrop";

        private const float Gravity = 0.22f;
        private const float ThrowSpeed = 8f;
        private const int StuckLifetime = 12 * 60;
        private const int SpreadCount = 6;
        private const float SpreadDegrees = 26f;

        private bool IsStuck => Projectile.ai[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.knockBack = 1.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public static void ThrowSpread(IEntitySource source, Vector2 origin, Vector2 target, int damage, float knockback, int owner)
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
            for (int i = 0; i < SpreadCount; i++)
            {
                float angle = MathHelper.ToRadians(startAngle + step * i);
                Vector2 velocity = baseVelocity.RotatedBy(angle) * Main.rand.NextFloat(0.92f, 1.08f);
                Projectile.NewProjectile(
                    source,
                    origin,
                    velocity,
                    ModContent.ProjectileType<Caltrop>(),
                    damage,
                    knockback,
                    owner);
            }
        }

        public override void AI()
        {
            if (IsStuck)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = Projectile.ai[1];
                return;
            }

            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            Projectile.rotation += Projectile.velocity.X * 0.12f;
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

            Projectile.ai[0] = 1f;
            Projectile.ai[1] = Projectile.rotation;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = StuckLifetime;
            Projectile.netUpdate = true;

            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.position.X += oldVelocity.X > 0f ? -2f : 2f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.position.Y += oldVelocity.Y > 0f ? -2f : 2f;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, PitchVariance = 0.35f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, oldVelocity.X * 0.1f, oldVelocity.Y * 0.1f, 160, default(Color), 0.7f);
            }
        }
    }
}
