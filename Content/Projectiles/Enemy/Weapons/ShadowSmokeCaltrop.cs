using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    // Shadow Ninja's smoke bomb, tossed mid-backflip. Harmless itself: it bursts on a tile, on passing within
    // BurstProximity of a player, or at the end of its flight, and leaves CloudsPerCaltrop ShadowSmokeClouds.
    // Three per backflip = 12 clouds.
    public class ShadowSmokeCaltrop : ModProjectile
    {
        // Deliberate texture share: it is the same caltrop the Shadow Ninja already throws, just a smoke-filled one.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Items.Weapons.Enemy.EnemyCaltrop));

        public const int CloudsPerCaltrop = 4;
        public const float ThrowSpeed = 7.5f;
        private const float Gravity = 0.22f;
        private const float MaxFallSpeed = 14f;
        private const float BurstProximity = 28f;
        private const int FlightTicks = 90;
        // The four clouds sit in a diamond this far from the burst point, so a cluster covers ~130px.
        private const float CloudSpacing = 30f;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightTicks;
        }

        /// <summary>Server: lobs one smoke caltrop from <paramref name="origin"/> to land on <paramref name="target"/>.</summary>
        public static void Throw(IEntitySource source, Vector2 origin, Vector2 target)
        {
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(origin, target, ThrowSpeed, Gravity, highAngle: false, fallback: true);
            if (velocity == Vector2.Zero)
            {
                velocity = (target - origin).SafeNormalize(Vector2.UnitX) * ThrowSpeed;
            }

            Projectile.NewProjectile(source, origin, velocity, ModContent.ProjectileType<ShadowSmokeCaltrop>(), 0, 0f, Main.myPlayer);
        }

        public override void AI()
        {
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > MaxFallSpeed)
            {
                Projectile.velocity.Y = MaxFallSpeed;
            }

            Projectile.rotation += Projectile.velocity.X * 0.12f;

            // Fuse trail: a thin grey wisp every other tick.
            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Dust wisp = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity * 0.15f, 150, new Color(60, 58, 70), 0.9f);
                wisp.noGravity = true;
            }

            // Burst on a near-miss too, so a caltrop thrown at a jumping player doesn't sail past harmlessly.
            // Server-decided: the kill is synced and OnKill spawns the clouds there.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool near = player.active && !player.dead
                    && Vector2.DistanceSquared(player.Center, Projectile.Center) <= BurstProximity * BurstProximity;
                if (near)
                {
                    Projectile.Kill();
                    return;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Diamond above the burst point: the bottom cloud sits on the impact, the rest rise off it.
                Vector2[] offsets =
                {
                    new Vector2(0f, -4f),
                    new Vector2(-CloudSpacing, -CloudSpacing * 0.7f),
                    new Vector2(CloudSpacing, -CloudSpacing * 0.7f),
                    new Vector2(0f, -CloudSpacing * 1.5f),
                };

                for (int i = 0; i < CloudsPerCaltrop; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center + offsets[i], Vector2.Zero,
                        ModContent.ProjectileType<ShadowSmokeCloud>(), 0, 0f, Main.myPlayer);
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.4f }, Projectile.Center);

            // Burst: ~40 dark smoke puffs thrown outward plus a few violet sparks.
            for (int i = 0; i < 34; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(4f, 3f);
                Dust smoke = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, burstVelocity, 110, new Color(40, 38, 48), Main.rand.NextFloat(1.4f, 2.2f));
                smoke.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Circular(5f, 5f);
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, sparkVelocity, 80, default, 1.1f);
                spark.noGravity = true;
            }
        }
    }

    // One puff of a smoke-bomb cluster. Fades in over 15t, holds 4s, thins out over 45t (harmless while thinning).
    // A player inside the radius gets Darkness and Slow refreshed to DebuffTicks (4s) every tick, so both last 4s
    // after stepping out. Applied by each client to its own player, which vanilla then syncs.
    public class ShadowSmokeCloud : ModProjectile
    {
        // Dust + shader only; the shared transparent placeholder, same as ToxicGasNovaFog.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public const int DebuffTicks = 4 * 60;
        private const float Radius = 34f;
        private const int FadeInTicks = 15;
        private const int HoldTicks = 4 * 60;
        private const int FadeOutTicks = 45;
        private const int HarmfulTicks = FadeInTicks + HoldTicks;
        private const int TotalTicks = HarmfulTicks + FadeOutTicks;
        private const float Opacity = 0.82f;

        private int Age => TotalTicks - Projectile.timeLeft;

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.damage = 0;
            Projectile.timeLeft = TotalTicks;
        }

        public override void AI()
        {
            // Slow rise and sway so a cluster churns instead of sitting as stamped discs.
            Projectile.position.Y -= 0.06f;
            Projectile.position.X += (float)System.Math.Sin(Projectile.whoAmI * 0.9f + Main.GlobalTimeWrappedHourly * 0.7f) * 0.08f;

            bool harmful = Age < HarmfulTicks;
            if (harmful && !Main.dedServ)
            {
                Player local = Main.LocalPlayer;
                if (local.active && !local.dead && IsInsideCloud(local.Hitbox))
                {
                    local.AddBuff(BuffID.Darkness, DebuffTicks);
                    local.AddBuff(BuffID.Slow, DebuffTicks);
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            // One rising puff every 3rd tick per cloud; 12 clouds = 4 dust/tick for a whole backflip's worth.
            if ((Main.GameUpdateCount + Projectile.whoAmI) % 3 == 0)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
                Vector2 drift = new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(-0.45f, -0.1f));
                Dust puff = Dust.NewDustPerfect(spot, DustID.Smoke, drift, 120, new Color(40, 38, 48), Main.rand.NextFloat(1.2f, 1.8f));
                puff.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Map onto the shader's hold curve: rise to full by FadeInTicks, hold, then 0.72 -> 1.0 over the fade-out.
            float shaderProgress;
            if (Age < FadeInTicks)
            {
                shaderProgress = MathHelper.Lerp(0f, 0.12f, Age / (float)FadeInTicks);
            }
            else if (Age < HarmfulTicks)
            {
                float holdFraction = (Age - FadeInTicks) / (float)HoldTicks;
                shaderProgress = MathHelper.Lerp(0.12f, 0.72f, holdFraction);
            }
            else
            {
                float fadeFraction = MathHelper.Clamp((Age - HarmfulTicks) / (float)FadeOutTicks, 0f, 1f);
                shaderProgress = MathHelper.Lerp(0.72f, 1f, fadeFraction);
            }

            float rotation = Projectile.whoAmI * 2.3f;
            EnemyVFX.DrawShadowSmokeField(Projectile.Center, Vector2.One * Radius * 2f, shaderProgress, rotation, Opacity);
            return false;
        }

        public override bool? CanDamage() => false;

        private bool IsInsideCloud(Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }
    }
}
