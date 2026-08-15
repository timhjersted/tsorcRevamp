using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// One half of a HeavyPounce ground slam: a gust of displaced air that fans outward along the floor
    /// from the impact point. Spawned as a mirrored PAIR (one per side) by tsorcRevampAIs.LandHeavyPounce.
    /// </summary>
    /// <remarks>
    /// Deliberately a travelling projectile rather than an instant radial hit, because the whole point is
    /// that it can be **dodged through**: it is a thin, fast-moving front with a gap under and over it, so
    /// jumping it, dashing through it, or simply being outside its lane all work. An instant AoE at the
    /// landing point would be unreactable and would make the slam feel like a damage tax rather than a
    /// move you read.
    ///
    /// `ai[0]` = travel direction (-1 / +1). `ai[1]` = fan index, so the two gusts of a pair rise at
    /// slightly different rates and it does not read as one mirrored stamp.
    /// </remarks>
    public class HeavyPounceShockwave : ModProjectile
    {
        public const int LifetimeTicks = 46;
        private const float TravelSpeed = 7.2f;
        private const float GrowthPerTick = 1.9f;
        private const int StaggerTicks = 60; // 1s, per design — shorter than Stagger's own 2s default

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        private float Direction => Projectile.ai[0] >= 0f ? 1f : -1f;
        private float FanPhase => Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;   // it rides the floor it was born on; terrain steps must not eat it
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];

            // Accelerates outward and grows taller as it goes, so the safe window to jump it is early and
            // closes — reading it late should not be free.
            Projectile.velocity = new Vector2(Direction * TravelSpeed, 0f);
            Projectile.height = (int)MathHelper.Clamp(34f + age * GrowthPerTick, 34f, 96f);

            if (Main.dedServ)
            {
                return;
            }

            float fade = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);
            int dustCount = (int)MathHelper.Lerp(3f, 7f, fade);
            for (int i = 0; i < dustCount; i++)
            {
                // Fanned upward and outward: a low, raked crescent rather than a vertical wall, so the
                // shape itself communicates that there is room above it.
                float rise = Main.rand.NextFloat(0f, Projectile.height * 0.9f);
                float rake = rise * 0.35f * Direction;
                Vector2 position = Projectile.Bottom + new Vector2(rake + Main.rand.NextFloat(-8f, 8f), -rise);
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(1.4f, 3.2f),
                    -Main.rand.NextFloat(0.2f, 1.1f) - FanPhase * 0.25f);

                int dustType = Main.rand.NextBool(3) ? DustID.Smoke : DustID.Cloud;
                Dust dust = Dust.NewDustPerfect(position, dustType, velocity, 170,
                    new Color(28, 22, 34), Main.rand.NextFloat(1.1f, 2.0f) * fade);
                dust.noGravity = true;
                dust.fadeIn = 0.7f;

                if (Main.rand.NextBool(4))
                {
                    Dust grit = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), 0f),
                        DustID.Stone, new Vector2(Direction * Main.rand.NextFloat(2f, 4f), -Main.rand.NextFloat(1f, 3f)),
                        140, default, Main.rand.NextFloat(0.8f, 1.3f) * fade);
                    grit.noGravity = false;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Stagger.Apply(target, StaggerTicks);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
