using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Void Flame: a crescent of purple fire, shaped like ")" with its bulge leading, thrown off the Witchking's void blade
    /// on the peak of a rising cut. It leaves the blade 4 tiles tall and grows to 16 tiles tall over its first 20 tiles of
    /// travel, then holds that size until it fades. The burning band is 2.5 tiles thick at the middle and 1.5 at the tips.
    /// Homes weakly for its first 75 ticks (max 0.02 rad/tick — a sideways dash outruns it), then flies straight.
    /// Drawn by the WitchkingVoidCrescent shader (EnemyVFX.DrawWitchkingVoidCrescent); the hitbox follows the drawn arc.
    /// Its centre bursts on cave walls (tileCollide, small hitbox); the tall arms deliberately pass through rock.
    /// ai[0] = target player index, chosen by the server at release (nearest valid player).
    /// </summary>
    public class WitchkingVoidFlame : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public const float LaunchSpeed = 8f;
        private const int HomingTicks = 75;
        private const float MaxTurnPerTick = 0.02f;
        private const int LifetimeTicks = 180;
        private const int FadeTicks = 15;

        // Size: 4 tiles tall at launch → 16 tiles tall after 20 tiles of travel. The bulge is 22% of the height; the quad
        // is the bulge plus the band (2.5 tiles at the middle → half-thickness 20px; 1.5 at the tips → 12px).
        private const float LaunchHeight = 4f * 16f;
        private const float FullHeight = 16f * 16f;
        private const float GrowDistance = 20f * 16f;
        private const float BulgeFraction = 0.22f;
        private const float MidHalfThickness = 20f;
        private const float TipHalfThickness = 12f;
        private const int ArcSamples = 11;

        private int Age => (int)Projectile.localAI[0];

        // Distance flown so far (px); drives the growth. Local per machine: speed is constant, so every peer agrees.
        private float Travelled
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private float Height => MathHelper.Lerp(LaunchHeight, FullHeight, MathHelper.Clamp(Travelled / GrowDistance, 0f, 1f));
        private float QuadWidth => Height * BulgeFraction + MidHalfThickness + TipHalfThickness;
        private float Opacity => 1f - Projectile.alpha / 255f;

        public override void SetDefaults()
        {
            // The small box is only the tile-collision core; player hits use the arc (Colliding).
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Travelled += Projectile.velocity.Length();

            // Weak homing: turn toward the target at most MaxTurnPerTick, for the first HomingTicks only.
            int targetIndex = (int)Projectile.ai[0];
            bool targetValid = targetIndex >= 0 && targetIndex < Main.maxPlayers
                && Main.player[targetIndex].active && !Main.player[targetIndex].dead;
            if (Age < HomingTicks && targetValid)
            {
                Player target = Main.player[targetIndex];
                float currentAngle = Projectile.velocity.ToRotation();
                float desiredAngle = (target.Center - Projectile.Center).ToRotation();
                float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -MaxTurnPerTick, MaxTurnPerTick);
                Projectile.velocity = Projectile.velocity.RotatedBy(turn);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            // Fade over the last FadeTicks of an on-screen timeout, never vanish at full opacity.
            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.timeLeft / (float)FadeTicks);
            }

            if (!Main.dedServ)
            {
                // A few embers shed off the arc, and light along it.
                Vector2 ember = ArcPoint(Main.rand.NextFloat(-1f, 1f), out _);
                Dust shed = Dust.NewDustPerfect(ember, Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Shadowflame,
                    -Projectile.velocity * 0.15f, 100, default, 1.2f * Opacity);
                shed.noGravity = true;
                for (int i = 0; i < 3; i++)
                {
                    Lighting.AddLight(ArcPoint(-0.66f + i * 0.66f, out _), 0.45f * Opacity, 0.15f * Opacity, 0.6f * Opacity);
                }
            }
        }

        // A point on the crescent's centre line at arc parameter y (-1 tip, 0 middle, +1 other tip), with the band's
        // half-thickness there. Mirrors the shader: centre line follows sqrt(1 - y^2) from the tips (back) to the bulge
        // (front); the band thins from MidHalfThickness at the middle to TipHalfThickness at the tips.
        private Vector2 ArcPoint(float y, out float halfThickness)
        {
            float arc = MathF.Sqrt(Math.Max(0f, 1f - y * y));
            float width = QuadWidth;
            float backEdge = -width / 2f;
            float centerX = backEdge + TipHalfThickness + (width - MidHalfThickness - TipHalfThickness) * arc;
            halfThickness = TipHalfThickness + (MidHalfThickness - TipHalfThickness) * arc;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 across = forward.RotatedBy(MathHelper.PiOver2);
            return Projectile.Center + forward * centerX + across * (y * Height / 2f);
        }

        // The hit follows the drawn band: circles of the local half-thickness along the centre line.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < ArcSamples; i++)
            {
                float y = -1f + 2f * i / (ArcSamples - 1f);
                Vector2 point = ArcPoint(y, out float halfThickness);
                Vector2 closest = Vector2.Clamp(point, targetHitbox.TopLeft(), targetHitbox.BottomRight());
                if (Vector2.DistanceSquared(point, closest) <= halfThickness * halfThickness)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool? CanDamage() => Projectile.timeLeft > FadeTicks ? null : false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            WitchkingBoss.ApplyVoidHit(target);
        }

        // Bursts along the whole arc, not just its centre, so a wall hit reads as the crescent shattering.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 70; i++)
            {
                Vector2 position = ArcPoint(Main.rand.NextFloat(-1f, 1f), out float halfThickness);
                position += Main.rand.NextVector2Circular(halfThickness, halfThickness);
                int dustType = i % 3 == 0 ? DustID.PurpleTorch : DustID.Shadowflame;
                Dust burst = Dust.NewDustPerfect(position, dustType, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.5f);
                burst.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 quadSize = new Vector2(QuadWidth, Height);
            float phase = (Projectile.identity % 17) * 0.37f;
            EnemyVFX.DrawWitchkingVoidCrescent(Projectile.Center, quadSize, Projectile.rotation, MidHalfThickness, Opacity, phase);
            return false;
        }
    }
}
