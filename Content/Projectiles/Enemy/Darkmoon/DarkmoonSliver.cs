using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    public class DarkmoonSliver : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 120; Projectile.hostile = true; Projectile.penetrate = 1;
            Projectile.tileCollide = false; Projectile.aiStyle = -1; Projectile.timeLeft = 52; Projectile.extraUpdates = 0;
            Projectile.DamageType = DamageClass.Magic;
        }
        public override bool? CanDamage() => Projectile.ai[0] < 40;
        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[0] == 40) Projectile.netUpdate = true;
            if (Projectile.ai[0] >= 40) Projectile.velocity *= 0.85f;
            if (!Main.dedServ && (int)Projectile.ai[0] % 3 == 0) DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 1, 5);

            // Shed light while flying: 2 additive sparks a tick from random points along the visible streak, drifting
            // out sideways and back, so the shard leaves a glittering wake instead of a hard-edged bar.
            if (!Main.dedServ && Projectile.ai[0] < 40)
            {
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < 2; i++)
                {
                    float along = Main.rand.NextFloat(-90f, 50f);
                    Vector2 sparkPosition = Projectile.Center + direction * along + normal * Main.rand.NextFloat(-5f, 5f);
                    Vector2 sparkVelocity = normal * Main.rand.NextFloat(-1.2f, 1.2f) - direction * Main.rand.NextFloat(0.5f, 2f);
                    global::tsorcRevamp.VFX.VFXSystem.AddParticle(new global::tsorcRevamp.VFX.VFXParticleData
                    {
                        Position = sparkPosition,
                        Velocity = sparkVelocity,
                        StartColor = Color.White,
                        EndColor = new Color(90, 150, 255) * 0f,
                        StartScale = Main.rand.NextFloat(0.25f, 0.5f),
                        EndScale = 0.05f,
                        Rotation = sparkVelocity.ToRotation(),
                        Damping = 0.93f,
                        Lifetime = Main.rand.Next(14, 26),
                        Texture = global::tsorcRevamp.VFX.VFXParticleTexture.Spark,
                        Blend = global::tsorcRevamp.VFX.VFXBlend.Additive,
                    });
                }
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.65f, 1f) * VisibleOpacity);
            }
        }

        // Visible shard: head at the front of the 96px damage body; the 190px spindle trails past the hitbox on purpose
        // (the tail is harmless glow). 44px quad = halo out to ~1.7x the 12px-radius body. Fades over the last 12t.
        const float ShardLength = 190f;
        const float ShardWidth = 44f;

        float VisibleOpacity
        {
            get
            {
                if (Projectile.ai[0] < 40)
                {
                    return 1f;
                }

                return MathHelper.Clamp(Projectile.timeLeft / 12f, 0, 1);
            }
        }
        public override bool? Colliding(Rectangle broadphase, Rectangle target)
        {
            Vector2 axis = Projectile.rotation.ToRotationVector2();
            for (int i = 0; i < 12; i++)
            {
                float u = (i + 0.5f) / 12f, radius = 7f * (float)Math.Pow(4 * u * (1 - u), 2);
                Vector2 position = Projectile.Center + axis * ((u - 0.5f) * 96);
                float contact = 0;
                if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), position - Projectile.velocity - axis * 4, position + axis * 4, Math.Max(1, radius * 2), ref contact)) return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // A 6% flicker (two out-of-step sines) keeps the light alive without strobing.
            float time = Main.GlobalTimeWrappedHourly;
            float flicker = 0.94f + 0.03f * (float)Math.Sin(time * 31f + Projectile.whoAmI) + 0.03f * (float)Math.Sin(time * 47f);
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            Vector2 head = Projectile.Center + direction * 52f;
            DarkmoonVisuals.DrawShard(head, direction, ShardLength, ShardWidth, VisibleOpacity * flicker);
            return false;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 180);
        public override void OnKill(int timeLeft) => DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 40, 12);
    }
    public class DarkmoonExplosion : ModProjectile
    {
        public const int Radius = 160, VisualTicks = 60, DamageStart = 6, DamageEndExclusive = 21;

        // The blast is nine staggered, continuously expanding layers of the explosion shader spaced 3 ticks apart.
        // Inner layers use AlphaBlend to anchor the moonlight-blue core; outer layers use Additive with lower peak
        // opacity so overlapping waves bloom into a smooth explosion of light instead of choppy static discs.
        // Damage timing is unchanged (DamageStart..DamageEndExclusive); later layers are visual only.
        const int LayerCount = 9;
        const int LayerSpacing = 3;
        const int BurstCount = 5;
        const int BurstSpacing = 6;
        static readonly float[] LayerStartScale = { 0.32f, 0.36f, 0.34f, 0.38f, 0.40f, 0.42f, 0.44f, 0.46f, 0.48f };
        static readonly float[] LayerEndScale = { 0.96f, 1.04f, 1.10f, 1.16f, 1.22f, 1.28f, 1.34f, 1.40f, 1.48f };
        static readonly float[] LayerPeakOpacity = { 0.72f, 0.56f, 0.46f, 0.38f, 0.32f, 0.27f, 0.22f, 0.18f, 0.14f };
        static readonly float[] LayerTurn = { 0f, 0.85f, -0.65f, 1.65f, -1.40f, 2.35f, -2.15f, 0.45f, -1.05f };
        static readonly float[] LayerSpin = { 0.22f, -0.26f, 0.30f, -0.24f, 0.28f, -0.32f, 0.25f, -0.20f, 0.18f };
        const int Lifetime = VisualTicks + (LayerCount - 1) * LayerSpacing;
        readonly bool[] hit = new bool[Main.maxPlayers];
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = Radius * 2; Projectile.hostile = true; Projectile.penetrate = -1;
            Projectile.tileCollide = false; Projectile.aiStyle = -1; Projectile.timeLeft = Lifetime; Projectile.extraUpdates = 0;
            Projectile.DamageType = DamageClass.Magic;
        }
        public static bool DamagingAt(int age) => age >= DamageStart && age < DamageEndExclusive;
        public override bool? CanDamage() => DamagingAt((int)Projectile.ai[0]);
        public override bool CanHitPlayer(Player target) => !hit[target.whoAmI];
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => hit[target.whoAmI] = true;
        public override bool? Colliding(Rectangle broadphase, Rectangle target)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, target.TopLeft(), target.BottomRight());
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }
        public override void AI()
        {
            int age = (int)Projectile.ai[0];
            if (!Main.dedServ)
            {
                if (age == 0)
                {
                    DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 80, 48);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 3f, 15, distanceFalloff: 650);
                }
                // Each burst: a white light burst, strongest first. Later bursts add a bright chime.
                bool burstTick = age % BurstSpacing == 0 && age / BurstSpacing < BurstCount;
                if (burstTick)
                {
                    int burst = age / BurstSpacing;
                    float strength = 1f - burst * 0.15f;
                    DarkmoonVisuals.LightBurst(Projectile.Center, 16, Radius * strength, strength);
                    if (burst > 0)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.4f + burst * 0.15f }, Projectile.Center);
                    }
                }

                if (age >= 21 && age < 49) DarkmoonVisuals.Motes(Projectile.Center, Color.Silver, 3, 160);

                // White flash for the first bursts, cooling to moonlight blue as it fades.
                float fade = MathHelper.Clamp(1 - age / (float)Lifetime, 0, 1);
                float flash = MathHelper.Clamp(1 - age / 24f, 0, 1);
                Vector3 light = Vector3.Lerp(new Vector3(0.3f, 0.65f, 1f), new Vector3(1.4f, 1.4f, 1.5f), flash) * fade;
                Lighting.AddLight(Projectile.Center, light);
            }
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 6 || Projectile.ai[0] == 21) Projectile.netUpdate = true;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // Draw inner AlphaBlend layers first (indices 0..1) to anchor the moonlight-blue core, then additive
            // outer waves (indices 2..8) on top so overlapping faded layers bloom smoothly into an explosion of light.
            for (int layer = 0; layer < LayerCount; layer++)
            {
                float layerAge = Projectile.ai[0] - layer * LayerSpacing;
                if (layerAge < 0f || layerAge > VisualTicks)
                {
                    continue;
                }

                float progress = MathHelper.Clamp(layerAge / VisualTicks, 0f, 1f);
                float growCurve = 1f - (float)Math.Pow(1f - progress, 2.3f);
                float currentScale = MathHelper.Lerp(LayerStartScale[layer], LayerEndScale[layer], growCurve);

                float fadeIn = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(progress / 0.14f, 0f, 1f));
                float fadeOut = (float)Math.Pow(1f - progress, 1.35f);
                float currentOpacity = LayerPeakOpacity[layer] * fadeIn * fadeOut;

                float rotation = Projectile.ai[2] + LayerTurn[layer] + LayerSpin[layer] * progress;
                bool useAdditive = layer >= 2;
                DarkmoonVisuals.DrawExplosion(Projectile.Center, progress, rotation, currentScale, currentOpacity, useAdditive);
            }

            return false;
        }
    }
}
