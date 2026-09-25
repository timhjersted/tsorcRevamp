using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// One rising "lick" of Greatfire Crescent's fire wave: several staggered FireBreath.png copies
    /// stacked up a vertical column, lighting up bottom-to-top as the wave climbs and fading back out
    /// the same way, each with its own slow rotation wobble so the column reads as licking flame
    /// rather than a static stamp. Spawned with damage > 0 (server-side), only the large upper licks
    /// hurt, each inside its own drawn size; the small base licks are always harmless.
    /// </summary>
    public class PuppetFireWaveColumn : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FireBreath));

        // ai[0] = full column height (px), ai[1] = signed horizontal fan-out at the column's top (px)
        private float ColumnHeight => Projectile.ai[0];
        private float FanSpread => Projectile.ai[1];
        // ai[2] opts into the compact landing eruption (0.8x scale, embers) and stores its delay.
        // Whether a column hurts is decided by its damage, not its mode — see Damaging.
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

        // Licks at or above this fraction of the column (the top 3 of 6) are the large ones — drawn
        // 1.06-1.3x base scale — and the only ones that deal damage.
        private const float LargeLickProgress = 0.6f;
        // Hurtbox side as a fraction of the drawn FireBreath size (38x36 * scale); the sprite's soft,
        // transparent edges shouldn't count as flame.
        private const float LickHitboxFraction = 0.6f;
        private const float LickTextureSize = 37f;

        private float[] _lickRotation;
        private float[] _lickRotSpeed;
        private float[] _lickPhaseOffset; // 0..1 of RiseTicks — staggers bottom-to-top ignition
        private float[] _lickXJitter;
        private float[] _lickScale;

        private bool Damaging => Projectile.damage > 0;

        // Enemy columns hurt players. The player's Ancient Fire Axe subclasses this with false so the
        // same licks hurt NPCs instead. Type-based, so every machine agrees without syncing the flags.
        protected virtual bool HurtsPlayers => true;

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
        }

        // Lick layout, built on first use from a seed every peer shares (identity is synced).
        // OnSpawn only runs on the spawning machine, so server-spawned columns previously reached
        // multiplayer clients with null arrays; the random scales also feed the hurtboxes now.
        private void EnsureLicks()
        {
            if (_lickScale != null)
            {
                return;
            }

            Terraria.Utilities.UnifiedRandom random = new Terraria.Utilities.UnifiedRandom(Projectile.identity);
            _lickRotation = new float[LickCount];
            _lickRotSpeed = new float[LickCount];
            _lickPhaseOffset = new float[LickCount];
            _lickXJitter = new float[LickCount];
            _lickScale = new float[LickCount];

            for (int i = 0; i < LickCount; i++)
            {
                _lickRotation[i] = random.NextFloat(MathHelper.TwoPi);
                _lickRotSpeed[i] = random.NextFloat(-0.09f, 0.09f);
                _lickPhaseOffset[i] = i / (float)LickCount * 0.55f;
                _lickXJitter[i] = random.NextFloat(-1f, 1f);
                _lickScale[i] = random.NextFloat(0.75f, 1.15f);
            }
        }

        // Drawn scale of one lick. Shared by PreDraw and Colliding so a hurtbox is exactly as big as
        // the flame it belongs to.
        private float LickDrawScale(int lickIndex, float dissipate)
        {
            float lickProgress = lickIndex / (float)(LickCount - 1);
            float scale = _lickScale[lickIndex] * MathHelper.Lerp(0.7f, 1.3f, lickProgress) * (1f - dissipate * 0.3f);

            if (IsSlam)
            {
                scale *= 0.8f;
            }

            return scale;
        }

        public override void AI()
        {
            EnsureLicks();
            int elapsed = TotalTicks - Projectile.timeLeft;
            bool live = Damaging && elapsed >= 0;

            if (HurtsPlayers)
            {
                Projectile.hostile = live;
            }
            else
            {
                Projectile.friendly = live;
            }

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
            => Damaging && TotalTicks - Projectile.timeLeft >= 0 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int elapsed = TotalTicks - Projectile.timeLeft;
            if (!Damaging || elapsed < 0)
                return false;

            EnsureLicks();

            // Only the large upper licks hurt, each inside its own drawn size and following its
            // staggered rise (same position maths as PreDraw, minus the 3px cosmetic jitter). A lick
            // is live from 3 ticks after it ignites until it starts to dissipate.
            for (int i = 0; i < LickCount; i++)
            {
                float progress = i / (float)(LickCount - 1);
                if (progress < LargeLickProgress)
                    continue;

                float localTick = elapsed - _lickPhaseOffset[i] * RiseTicks;
                if (localTick < 3f || localTick > RiseTicks + LingerTicks)
                    continue;

                float riseFraction = MathHelper.Clamp(localTick / RiseTicks, 0f, 1f);
                Vector2 center = Projectile.Center + new Vector2(FanSpread * progress * progress,
                    -progress * ColumnHeight * riseFraction);
                int hitboxSide = (int)(LickTextureSize * LickDrawScale(i, 0f) * LickHitboxFraction);
                Rectangle lickHitbox = new Rectangle(
                    (int)center.X - hitboxSide / 2, (int)center.Y - hitboxSide / 2, hitboxSide, hitboxSide);

                if (lickHitbox.Intersects(targetHitbox))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnsureLicks();
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
                float scale = LickDrawScale(i, dissipate);

                Main.EntitySpriteDraw(texture, drawPosition, null, color, _lickRotation[i],
                    origin, scale, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
