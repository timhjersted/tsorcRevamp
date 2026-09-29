using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // One-shot giant AOE detonation for Artorias's health-threshold charge-up nova. Radius is set
    // per-use via ai[0] (500/600/700px across the three HP-threshold triggers) since a single fixed
    // size can't cover all three call sites.
    class ArtoriasChargeNova : ModProjectile
    {
        // One explosion sheet plays in 18t, too brief to sell a blast this size. So it replays BurstCount
        // times, a new one every BurstIntervalTicks, each overlapping the tail of the last: a ~63t rolling
        // detonation. Only the first burst's window deals damage, so the hit timing is unchanged.
        const int BurstTicks = 18;
        const int BurstCount = 6;
        const int BurstIntervalTicks = 9;
        const int Lifetime = (BurstCount - 1) * BurstIntervalTicks + BurstTicks;
        const int HitWindowTicks = BurstTicks;
        // Later bursts draw dimmer so the whole thing decays instead of ending on a full-strength flash.
        const float LastBurstOpacity = 0.55f;

        float Radius => Projectile.ai[0];

        int AgeTicks => Lifetime - Projectile.timeLeft;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)(Radius * 2f);
            Projectile.Center = center;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.25f);

            if (Main.dedServ)
            {
                return;
            }

            // Each follow-up burst gets its own quieter, deeper thump (the first one's sound is the boss's DoNovaBlast).
            int age = AgeTicks;
            bool burstStartsNow = age > 0 && age % BurstIntervalTicks == 0 && age / BurstIntervalTicks < BurstCount;
            if (burstStartsNow)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = -0.45f }, Projectile.Center);
            }

            if (Projectile.timeLeft % 2 == 0)
            {
                Color tint = Main.rand.NextBool(3) ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 40, tint, Main.rand.NextFloat(1.4f, 2.2f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = AgeTicks;

            // Up to two bursts overlap at a time (18t each, 9t apart); each runs its own 0 -> 1 sheet
            // progress and fades over its last 5 ticks, as the single burst used to.
            for (int burst = 0; burst < BurstCount; burst++)
            {
                int burstAge = age - burst * BurstIntervalTicks;
                if (burstAge < 0 || burstAge >= BurstTicks)
                {
                    continue;
                }

                float progress = burstAge / (float)BurstTicks;
                float fade = MathHelper.Clamp((BurstTicks - burstAge) / 5f, 0f, 1f);
                float burstStrength = MathHelper.Lerp(1f, LastBurstOpacity, burst / (float)(BurstCount - 1));
                ArtoriasVFX.DrawDetonation(Projectile.Center, Radius, progress, 0.92f * fade * burstStrength, active: true);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (AgeTicks >= HitWindowTicks)
            {
                return false;
            }

            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= Radius;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                Color tint = Main.rand.NextBool() ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, vel.X, vel.Y, 60, tint, 2.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
