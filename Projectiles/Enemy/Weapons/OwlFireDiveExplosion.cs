using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Four-tile fire blast made from the same FireBreath sprite used by Ancient Oolacile Demon.
    /// Two irregular waves of flame lobes travel away from the center in every direction while
    /// torch, gold, and smoke dust fill the gaps. The projectile owns the circular damage area.
    /// </summary>
    public class OwlFireDiveExplosion : ModProjectile
    {
        private const int Lifetime = 60;
        private const float MaximumRadius = 64f;
        private const int PrimaryLobeCount = 24;
        private const int SecondaryLobeCount = 18;
        private const int TotalLobeCount = PrimaryLobeCount + SecondaryLobeCount;
        private const int SecondaryWaveDelay = 9;
        public const int ImpactDamage = 18;

        private Vector2[] _lobeDirections;
        private float[] _lobeTravel;
        private float[] _lobeScale;
        private float[] _lobeRotation;
        private float[] _lobeSpin;
        private float[] _lobeTwist;
        private int[] _lobeDelay;
        private int[] _lobeDuration;

        private int Elapsed => Lifetime - Projectile.timeLeft;
        private float DamageRadius => MaximumRadius * MathHelper.SmoothStep(0f, 1f,
            MathHelper.Clamp(Elapsed / 8f, 0f, 1f));

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = (int)(MaximumRadius * 2f);
            Projectile.height = (int)(MaximumRadius * 2f);
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;

            InitializeFlameLobes();
            SoundEngine.PlaySound(SoundID.NPCDeath2 with
            {
                Volume = 0.4f,
                PitchVariance = 0.06f
            }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item34 with
            {
                Volume = 0.3f,
                Pitch = -0.18f,
                PitchVariance = 0.08f
            }, Projectile.Center);
            SpawnFlameBreathParticles(54, 24, 12, 1f);
        }

        private void InitializeFlameLobes()
        {
            _lobeDirections = new Vector2[TotalLobeCount];
            _lobeTravel = new float[TotalLobeCount];
            _lobeScale = new float[TotalLobeCount];
            _lobeRotation = new float[TotalLobeCount];
            _lobeSpin = new float[TotalLobeCount];
            _lobeTwist = new float[TotalLobeCount];
            _lobeDelay = new int[TotalLobeCount];
            _lobeDuration = new int[TotalLobeCount];

            for (int i = 0; i < TotalLobeCount; i++)
            {
                bool secondary = i >= PrimaryLobeCount;
                int localIndex = secondary ? i - PrimaryLobeCount : i;
                int waveCount = secondary ? SecondaryLobeCount : PrimaryLobeCount;
                float angle = MathHelper.TwoPi * localIndex / waveCount
                    + (secondary ? MathHelper.Pi / waveCount : 0f)
                    + Main.rand.NextFloat(-0.13f, 0.13f);
                _lobeDirections[i] = angle.ToRotationVector2();
                _lobeTravel[i] = secondary
                    ? Main.rand.NextFloat(42f, 62f)
                    : Main.rand.NextFloat(56f, 76f);
                _lobeScale[i] = secondary
                    ? Main.rand.NextFloat(0.48f, 0.82f)
                    : Main.rand.NextFloat(0.58f, 1f);
                _lobeRotation[i] = Main.rand.NextFloat(MathHelper.TwoPi);
                _lobeSpin[i] = Main.rand.NextFloat(-0.18f, 0.18f);
                _lobeTwist[i] = Main.rand.NextFloat(-0.24f, 0.24f);
                _lobeDelay[i] = (secondary ? SecondaryWaveDelay : 0) + Main.rand.Next(0, 4);
                _lobeDuration[i] = Main.rand.Next(24, 35);
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            float lightFade = 1f - Elapsed / (float)Lifetime;
            Lighting.AddLight(Projectile.Center, 1f * lightFade, 0.38f * lightFade, 0.06f * lightFade);

            if (Main.dedServ)
                return;

            if (Elapsed == SecondaryWaveDelay)
                SpawnFlameBreathParticles(38, 18, 8, 0.78f);

            // A few live motes keep the space between the moving breath sprites filled without
            // rebuilding the old circular rim.
            if (Elapsed < 38)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                    Vector2 tangent = new Vector2(-direction.Y, direction.X);
                    Dust flame = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(4f, DamageRadius),
                        Main.rand.NextBool(4) ? DustID.GoldFlame : DustID.Torch,
                        direction * Main.rand.NextFloat(1.2f, 3.8f)
                            + tangent * Main.rand.NextFloat(-0.8f, 0.8f),
                        80,
                        default,
                        Main.rand.NextFloat(0.65f, 1.3f));
                    flame.noGravity = true;
                }
            }
        }

        public override bool? CanDamage() => DamageRadius > 6f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearestPoint = new Vector2(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, nearestPoint) <= DamageRadius * DamageRadius;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (_lobeDirections == null)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(
                "tsorcRevamp/Projectiles/Enemy/FireBreath").Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < TotalLobeCount; i++)
            {
                int localTick = Elapsed - _lobeDelay[i];
                if (localTick < 0 || localTick > _lobeDuration[i])
                    continue;

                float progress = localTick / (float)_lobeDuration[i];
                float outward = 1f - (1f - progress) * (1f - progress);
                float appear = MathHelper.Clamp(localTick / 4f, 0f, 1f);
                float fade = 1f - MathHelper.SmoothStep(0f, 1f,
                    MathHelper.Clamp((progress - 0.5f) / 0.5f, 0f, 1f));
                float opacity = appear * fade;
                if (opacity <= 0.01f)
                    continue;

                Vector2 direction = _lobeDirections[i].RotatedBy(_lobeTwist[i] * progress);
                Vector2 drawPosition = Projectile.Center
                    + direction * MathHelper.Lerp(3f, _lobeTravel[i], outward)
                    - Main.screenPosition;
                Color color = Color.Lerp(
                    new Color(255, 225, 95),
                    new Color(235, 55, 12),
                    progress) * opacity;
                float scale = _lobeScale[i] * MathHelper.Lerp(0.62f, 1f, appear)
                    * MathHelper.Lerp(1f, 0.72f, progress);

                Main.EntitySpriteDraw(texture, drawPosition, null, color,
                    _lobeRotation[i] + _lobeSpin[i] * localTick,
                    origin, scale, SpriteEffects.None, 0);
            }

            return false;
        }

        private void SpawnFlameBreathParticles(
            int bodyCount, int emberCount, int smokeCount, float velocityScale)
        {
            // Torch dust forms the dense breath body. Random angles, speed, and tangent offsets make
            // it an expanding volume of fire rather than an evenly spaced ring.
            for (int i = 0; i < bodyCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 tangent = new Vector2(-direction.Y, direction.X);
                Dust body = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Torch,
                    direction * Main.rand.NextFloat(2.2f, 7.5f) * velocityScale
                        + tangent * Main.rand.NextFloat(-1.3f, 1.3f),
                    75,
                    new Color(255, 135, 32),
                    Main.rand.NextFloat(1.05f, 2.25f) * velocityScale);
                body.noGravity = true;
            }

            for (int i = 0; i < emberCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(2f, 12f),
                    DustID.GoldFlame,
                    direction * Main.rand.NextFloat(6f, 11f) * velocityScale,
                    55,
                    new Color(255, 220, 75),
                    Main.rand.NextFloat(0.55f, 1.05f));
                ember.noGravity = true;
            }

            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 18f),
                    DustID.Smoke,
                    direction * Main.rand.NextFloat(0.7f, 2.4f) - Vector2.UnitY * 0.55f,
                    155,
                    new Color(58, 39, 28),
                    Main.rand.NextFloat(0.7f, 1.25f));
                smoke.fadeIn = Main.rand.NextFloat(1.05f, 1.45f);
                smoke.noLight = true;
            }
        }
    }
}
