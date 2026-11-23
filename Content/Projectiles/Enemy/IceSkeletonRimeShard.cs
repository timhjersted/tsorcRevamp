using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Lance of Rime: the ice spear-head the Ice Skeleton forms on its spear tip and thrusts free.
    /// Sprite: 31x31, cut from the spear's own ice head; the point is the TOP-LEFT corner, so rotation is
    /// velocity + 3pi/4. Straight 12px/t, no gravity or homing. Dies on tiles and players with an ice burst and a
    /// glass-break sound; on timeout it fades over the last 12t. No children.
    /// </summary>
    public class IceSkeletonRimeShard : ModProjectile
    {
        public const float Speed = 12f;
        const int Lifetime = 90;
        const int FadeTicks = 12;
        // The sprite's point is at its top-left corner (-135 degrees in texture space).
        const float PointLeadsOffset = MathHelper.Pi * 0.75f;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PointLeadsOffset;

            // Fade over the last 12t of a timeout, so a shard that flies off never vanishes at full opacity.
            int remaining = Projectile.timeLeft;
            Projectile.Opacity = 1f;
            if (remaining < FadeTicks)
            {
                Projectile.Opacity = remaining / (float)FadeTicks;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.32f, 0.58f, 0.78f) * Projectile.Opacity);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 sideways = new Vector2(-forward.Y, forward.X);
            Vector2 tailBase = Projectile.Center - forward * 8f;
            float opacity = Projectile.Opacity;

            // Midground frost wake: 3 motes/tick interpolated over the 12px step (~4px spacing, no dotted gaps).
            // Max scale with +/-20% jitter: 1.2 * 1.35 = 1.62.
            for (int i = 0; i < 3; i++)
            {
                float stepFraction = (i + Main.rand.NextFloat()) / 3f;
                Vector2 along = tailBase - Projectile.velocity * stepFraction;
                Vector2 spread = sideways * Main.rand.NextFloat(-4.5f, 4.5f);
                int dustType = DustID.IceTorch;
                if (i == 0)
                {
                    dustType = DustID.Frost;
                }

                Vector2 vel = -Projectile.velocity * Main.rand.NextFloat(0.10f, 0.24f)
                    + sideways * Main.rand.NextFloat(-0.6f, 0.6f);
                Dust wake = Dust.NewDustPerfect(along + spread, dustType, vel, 45, default, Main.rand.NextFloat(0.95f, 1.35f) * opacity);
                wake.noGravity = true;
            }

            // Foreground crystalline glints: bright white-hot core spark every tick + crystal shard every 2 ticks.
            Vector2 sparkPos = Projectile.Center - forward * Main.rand.NextFloat(0f, 10f)
                + sideways * Main.rand.NextFloat(-3.5f, 3.5f);
            Vector2 sparkVel = -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.16f)
                + sideways * Main.rand.NextFloat(-1.1f, 1.1f);
            Dust glint = Dust.NewDustPerfect(sparkPos, DustID.IceTorch, sparkVel, 0, Color.White, Main.rand.NextFloat(0.55f, 0.90f) * opacity);
            glint.noGravity = true;

            if (Projectile.timeLeft % 2 == 0)
            {
                Vector2 chipVel = -Projectile.velocity * 0.12f + sideways * Main.rand.NextFloat(-1.5f, 1.5f);
                Dust chip = Dust.NewDustPerfect(tailBase, DustID.BlueCrystalShard, chipVel, 20, default, Main.rand.NextFloat(0.6f, 0.9f) * opacity);
                chip.noGravity = true;

                // Background cold vapor: spawns small and billows via fadeIn so a lingering frost ribbon trails behind.
                Vector2 mistVel = -Projectile.velocity * 0.05f + new Vector2(0f, Main.rand.NextFloat(-0.5f, 0.2f));
                Dust mist = Dust.NewDustPerfect(tailBase, DustID.Cloud, mistVel, 155, new Color(175, 225, 255), Main.rand.NextFloat(0.55f, 0.75f) * opacity);
                mist.fadeIn = Main.rand.NextFloat(1.25f, 1.55f);
                mist.noGravity = true;
            }

            // During timeout fade-out, shed gentle falling frost flakes so dissipation reads intentionally.
            if (remaining < FadeTicks)
            {
                Dust flake = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.Snow, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(0.3f, 1.2f)),
                    60, default, Main.rand.NextFloat(0.75f, 1.1f));
                flake.noGravity = false;
            }
        }

        // Hostile shots are not consumed by a player hit on their own; the shared global relays the kill in MP.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A timed-out shard has already faded; only an impact shatters.
            bool impact = timeLeft > 0;
            if (!impact)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);

            // 3-layer 70-mote shatter burst: 38 midground frost body, 20 fast foreground crystal/white sparks,
            // 12 slow billowing vapor clouds that outlive the flash.
            for (int i = 0; i < 38; i++)
            {
                int dustType = DustID.IceTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.Frost;
                }

                Vector2 dir = Main.rand.NextVector2Unit();
                Vector2 velocity = dir * Main.rand.NextFloat(1.8f, 5.8f);
                Dust body = Dust.NewDustPerfect(Projectile.Center + dir * Main.rand.NextFloat(0f, 6f),
                    dustType, velocity, 40, default, Main.rand.NextFloat(0.95f, 1.5f));
                body.noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                int dustType = DustID.BlueCrystalShard;
                Color tint = default;
                bool weightless = false;
                if (i % 2 == 0)
                {
                    dustType = DustID.IceTorch;
                    tint = Color.White;
                    weightless = true;
                }

                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4.5f, 9.2f);
                Dust spark = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 0, tint, Main.rand.NextFloat(0.55f, 0.95f));
                spark.noGravity = weightless;
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.6f, 2.2f);
                Dust mist = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.Cloud, velocity, 150, new Color(175, 225, 255), Main.rand.NextFloat(0.55f, 0.8f));
                mist.fadeIn = Main.rand.NextFloat(1.3f, 1.7f);
                mist.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Ice glows a little in the dark: lift the light toward a cold white.
            Color light = Color.Lerp(lightColor, new Color(200, 230, 255), 0.5f) * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, drawPosition, null, light, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}
