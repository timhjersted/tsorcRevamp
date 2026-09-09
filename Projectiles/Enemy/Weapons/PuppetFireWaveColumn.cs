using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// One rising "lick" of Greatfire Crescent's fire wave: several staggered FireBreath.png copies
    /// stacked up a vertical column, lighting up bottom-to-top as the wave climbs and fading back out
    /// the same way, each with its own slow rotation wobble so the column reads as licking flame
    /// rather than a static stamp. Decorative alongside Crescent; the slam variant damages only
    /// inside its visible, rising flames.
    /// </summary>
    public class PuppetFireWaveColumn : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/FireBreath";

        // ai[0] = full column height (px), ai[1] = signed horizontal fan-out at the column's top (px)
        private float ColumnHeight => Projectile.ai[0];
        private float FanSpread => Projectile.ai[1];
        // ai[2] opts into the compact, damaging landing eruption and stores its visual delay.
        // Crescent leaves ai[2] at zero and remains decorative.
        public const int SlamMode = 1;
        public static float EncodeSlamDelay(int ticks) => SlamMode + System.Math.Max(0, ticks);
        private bool IsSlam => Projectile.ai[2] >= SlamMode;
        private int SpawnDelay => IsSlam ? System.Math.Max(0, (int)Projectile.ai[2] - SlamMode) : 0;

        private const int LickCount = 6;
        private const int RiseTicks = 18;   // time for the wave to climb the full column
        private const int LingerTicks = 10; // hold near full height before dissipating
        private const int FadeTicks = 6;    // each FireBreath sprite gets a complete six-tick fade
        private const int MaxLickDelayTicks = 9;
        // Include the upper lick's stagger in the projectile lifetime. Without this allowance the
        // top sprites were still partly opaque when the projectile expired and vanished abruptly.
        private const int TotalTicks = RiseTicks + LingerTicks + FadeTicks + MaxLickDelayTicks;

        private float[] _lickRotation;
        private float[] _lickRotSpeed;
        private float[] _lickPhaseOffset; // 0..1 of RiseTicks — staggers bottom-to-top ignition
        private float[] _lickXJitter;
        private float[] _lickScale;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = TotalTicks + SpawnDelay;
            _lickRotation = new float[LickCount];
            _lickRotSpeed = new float[LickCount];
            _lickPhaseOffset = new float[LickCount];
            _lickXJitter = new float[LickCount];
            _lickScale = new float[LickCount];

            for (int i = 0; i < LickCount; i++)
            {
                _lickRotation[i] = Main.rand.NextFloat(MathHelper.TwoPi);
                _lickRotSpeed[i] = Main.rand.NextFloat(-0.09f, 0.09f);
                _lickPhaseOffset[i] = i / (float)LickCount * 0.55f;
                _lickXJitter[i] = Main.rand.NextFloat(-1f, 1f);
                _lickScale[i] = Main.rand.NextFloat(0.75f, 1.15f);
            }
        }

        public override void AI()
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            Projectile.hostile = IsSlam && elapsed >= 0;
            if (elapsed < 0)
                return;
            if (Main.dedServ)
                return;

            for (int i = 0; i < LickCount; i++)
            {
                _lickRotation[i] += _lickRotSpeed[i];
            }

            if (Main.rand.NextBool(3))
            {
                Lighting.AddLight(Projectile.Center + new Vector2(0f, -ColumnHeight * 0.4f), 0.7f, 0.28f, 0.05f);
            }

            if (IsSlam && Projectile.timeLeft > FadeTicks && Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-9f, 9f), -4f),
                    DustID.Torch, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-3.5f, -1.5f)),
                    80, default, 1.1f);
                ember.noGravity = true;
            }
        }

        public override bool? CanDamage()
            => IsSlam && TotalTicks - Projectile.timeLeft >= 0 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (!IsSlam || elapsed < 0)
                return false;

            // Match each flame's staggered rise; never hit the empty space above the eruption.
            for (int i = 0; i < LickCount; i++)
            {
                float localTick = elapsed - i / (float)LickCount * 0.55f * RiseTicks;
                if (localTick < 3f || localTick > RiseTicks + LingerTicks)
                    continue;

                float progress = i / (float)(LickCount - 1);
                Vector2 center = Projectile.Center + new Vector2(FanSpread * progress * progress,
                    -progress * ColumnHeight * MathHelper.Clamp(localTick / RiseTicks, 0f, 1f));
                if (new Rectangle((int)center.X - 10, (int)center.Y - 10, 20, 20).Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (elapsed < 0)
                return false;
            float fanDirection = FanSpread >= 0f ? 1f : -1f;
            float fanMagnitude = System.Math.Abs(FanSpread);

            for (int i = 0; i < LickCount; i++)
            {
                float lickProgress = i / (float)(LickCount - 1); // 0 = base of column, 1 = top

                // Each lick runs its own local clock, offset so the wave visibly climbs the column
                // bottom-to-top instead of every flame appearing and vanishing in lockstep.
                float localTick = elapsed - _lickPhaseOffset[i] * RiseTicks;
                if (localTick < 0f)
                {
                    continue;
                }

                float appear = MathHelper.Clamp(localTick / 6f, 0f, 1f); // quick fade-in
                float dissipate = MathHelper.Clamp((localTick - (RiseTicks + LingerTicks)) / FadeTicks, 0f, 1f);
                float alpha = appear * (1f - dissipate);
                if (alpha <= 0.02f)
                {
                    continue;
                }

                float riseFraction = MathHelper.Clamp(localTick / RiseTicks, 0f, 1f);
                float height = -lickProgress * ColumnHeight * riseFraction;
                float fanX = fanDirection * fanMagnitude * lickProgress * lickProgress + _lickXJitter[i] * 3f;
                Vector2 drawPosition = Projectile.Center + new Vector2(fanX, height) - Main.screenPosition;

                // Base of the column reads hot yellow-orange; the licks that reach the top of the
                // wave have cooled toward a deeper ember red.
                Color color = Color.Lerp(new Color(255, 200, 60), new Color(200, 30, 10), lickProgress) * alpha;
                float scale = _lickScale[i] * MathHelper.Lerp(0.7f, 1.3f, lickProgress) * (1f - dissipate * 0.3f);
                if (IsSlam)
                    scale *= 0.8f;

                Main.EntitySpriteDraw(texture, drawPosition, null, color, _lickRotation[i],
                    origin, scale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
