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

            // The visible beam: head at the front of the 96px damage body, a 170px light streak behind it (it
            // trails past the hitbox on purpose - the tail is harmless glow). Thins and fades after its live window.
            if (!Main.dedServ)
            {
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                float opacity = 1f;
                if (Projectile.ai[0] >= 40)
                {
                    opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0, 1);
                }

                Vector2 head = Projectile.Center + direction * 52f;
                DarkmoonVisuals.BeamStrip(head, direction, BeamLength, BeamWidth, opacity);
                Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.65f, 1f) * opacity);
            }
        }

        const float BeamLength = 170f;
        const float BeamWidth = 14f;
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
        // Drawn entirely by the VFX strips added in AI.
        public override bool PreDraw(ref Color lightColor) => false;
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 180);
        public override void OnKill(int timeLeft) => DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 40, 12);
    }
    public class DarkmoonExplosion : ModProjectile
    {
        public const int Radius = 160, VisualTicks = 60, DamageStart = 6, DamageEndExclusive = 21;

        // The blast is three overlapping copies of the explosion shader, PulseSpacing ticks apart, each turned and
        // sized differently so they never stack into one flat disc; each pulse also fires a white light burst.
        // Damage timing is unchanged (DamageStart..DamageEndExclusive); the later pulses are visual only.
        const int PulseCount = 5, PulseSpacing = 7;
        static readonly float[] PulseScale = { 1f, 0.78f, 1.14f, 0.9f, 1.3f };
        static readonly float[] PulseOpacity = { 1f, 0.85f, 0.7f, 0.6f, 0.45f };
        static readonly float[] PulseTurn = { 0f, 1.1f, -0.8f, 2.3f, -2.0f };
        const int Lifetime = VisualTicks + (PulseCount - 1) * PulseSpacing;
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
                // Each pulse: a white light burst, strongest first. Later pulses add a bright chime.
                bool pulseTick = age % PulseSpacing == 0 && age / PulseSpacing < PulseCount;
                if (pulseTick)
                {
                    int pulse = age / PulseSpacing;
                    float strength = 1f - pulse * 0.15f;
                    DarkmoonVisuals.LightBurst(Projectile.Center, 16, Radius * strength, strength);
                    if (pulse > 0)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.45f, Pitch = 0.4f + pulse * 0.15f }, Projectile.Center);
                    }
                }

                if (age >= 21 && age < 49) DarkmoonVisuals.Motes(Projectile.Center, Color.Silver, 3, 160);

                // White flash for the first pulses, cooling to moonlight blue as it fades.
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
            // Later pulses draw first so the primary blast stays on top.
            for (int pulse = PulseCount - 1; pulse >= 0; pulse--)
            {
                float pulseAge = Projectile.ai[0] - pulse * PulseSpacing;
                if (pulseAge < 0 || pulseAge > VisualTicks)
                {
                    continue;
                }

                float progress = MathHelper.Clamp(pulseAge / VisualTicks, 0, 1);
                float rotation = Projectile.ai[2] + PulseTurn[pulse];
                DarkmoonVisuals.DrawExplosion(Projectile.Center, progress, rotation, PulseScale[pulse], PulseOpacity[pulse]);
            }

            return false;
        }
    }
}
