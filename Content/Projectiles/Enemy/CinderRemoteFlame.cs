using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Delayed fire brand placed around a player by Soul of Cinder's staff rituals.
    /// ai[1]: 0 constellation, 1 cross, 2 orbit.</summary>
    class CinderRemoteFlame : ModProjectile
    {
        const int HoldTicks = 30;
        const int ConstellationBirthTellTicks = 36;
        const int ConstellationVisibleHoldTicks = 24;
        const int FirelinkBirthTellTicks = 40;
        const int FirelinkVisibleHoldTicks = 20;
        const int AshenBirthTellTicks = 18;
        const int AshenAppearanceStepTicks = 8;
        const int AshenPostAppearancePauseTicks = 36;
        const int AshenCount = 6;
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CultistBossFireBall;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ReleaseTick((int)Projectile.ai[1]) ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 1)
                Projectile.localAI[1] = Projectile.rotation;

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            int mode = (int)Projectile.ai[1];
            float baseAngle = Projectile.localAI[1];
            int birthTick = BirthTick(mode);
            int releaseTick = ReleaseTick(mode);

            if (Projectile.localAI[0] < birthTick)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 255;
                Projectile.scale = 0f;
                SpawnBirthTelegraph(mode, birthTick);
            }
            else if (Projectile.localAI[0] <= releaseTick)
            {
                Projectile.velocity *= 0f;
                float visibleTicks = Projectile.localAI[0] - birthTick + 1f;
                Projectile.alpha = Math.Max(0, 255 - (int)(visibleTicks * 42f));
                Projectile.scale = MathHelper.Lerp(0.20f, 1f, MathHelper.Clamp(visibleTicks / 10f, 0f, 1f));
                ChargeVFX(mode, baseAngle, targetIndex, birthTick, releaseTick);
            }
            else if (Projectile.localAI[0] == releaseTick + 1)
            {
                Projectile.velocity = (Main.player[targetIndex].Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                    * (mode == 0 ? 7.5f : mode == 1 ? 9f : 11f);
                Projectile.tileCollide = true;
                ReleaseVFX(mode, baseAngle);
                SoundEngine.PlaySound(SoundID.Item74 with
                {
                    Volume = 0.35f,
                    Pitch = mode == 0 ? 0.3f : mode == 1 ? -0.15f : 0.1f
                }, Projectile.Center);
            }

            if (Projectile.localAI[0] > releaseTick)
            {
                Projectile.rotation += mode == 0 ? 0.18f : mode == 1 ? 0.14f : 0.12f;
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        -Projectile.velocity * 0.07f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                        90, Color.Gold, 0.8f);
                    dust.noGravity = true;
                }
            }

            Color lightColor = mode == 1 ? new Color(1f, 0.16f, 0.04f) : mode == 2 ? new Color(1f, 0.42f, 0.08f) : new Color(1f, 0.68f, 0.22f);
            Lighting.AddLight(Projectile.Center, lightColor.ToVector3() * Projectile.scale * 0.4f);
        }

        int BirthTick(int mode)
            => mode == 0 ? ConstellationBirthTellTicks + 1
                : mode == 1 ? FirelinkBirthTellTicks + 1
                : mode == 2 ? AshenBirthTellTicks + (int)Projectile.ai[2] * AshenAppearanceStepTicks
                : 1;

        static int ReleaseTick(int mode)
            => mode == 0
                ? ConstellationBirthTellTicks + 1 + ConstellationVisibleHoldTicks
                : mode == 1
                ? FirelinkBirthTellTicks + 1 + FirelinkVisibleHoldTicks
                : mode == 2
                ? AshenBirthTellTicks + (AshenCount - 1) * AshenAppearanceStepTicks + AshenPostAppearancePauseTicks
                : HoldTicks;

        void SpawnBirthTelegraph(int mode, int birthTick)
        {
            if (Main.dedServ)
                return;

            int tellTicks = mode == 0 ? ConstellationBirthTellTicks
                : mode == 1 ? FirelinkBirthTellTicks
                : AshenBirthTellTicks;
            float tellElapsed = Projectile.localAI[0] - (birthTick - tellTicks);
            if (tellElapsed < 0f)
                return;

            float progress = MathHelper.Clamp(tellElapsed / tellTicks, 0f, 1f);
            int count = 1 + (int)(progress * 2f);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(20f, 20f) * MathHelper.Lerp(1f, 0.25f, progress);
                bool redMote = mode == 1 && (i + (int)Projectile.localAI[0]) % 2 == 0;
                bool orangeMote = mode == 0 && (i + (int)Projectile.localAI[0]) % 3 == 0;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + offset,
                    redMote ? DustID.RedTorch : orangeMote ? DustID.OrangeTorch : DustID.GoldFlame,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(0.8f, 2.4f, progress),
                    90, redMote ? Color.OrangeRed : orangeMote ? Color.Orange : Color.Gold,
                    Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        void ChargeVFX(int mode, float baseAngle, int targetIndex, int birthTick, int releaseTick)
        {
            float charge = MathHelper.Clamp((Projectile.localAI[0] - birthTick) / Math.Max(1f, releaseTick - birthTick), 0f, 1f);
            if (mode == 0) // Constellation brands: inward-curling gold sparks and late white glints.
            {
                Projectile.rotation = baseAngle + Projectile.localAI[0] * 0.15f;
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    float spiral = baseAngle + Projectile.localAI[0] * 0.24f;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + spiral.ToRotationVector2() * (4f + charge * 5f), DustID.OrangeTorch,
                        -spiral.ToRotationVector2() * (0.8f + charge), 80, Color.Orange, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
                if (charge > 0.72f && ((int)Projectile.localAI[0]) % 4 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.6f, 0.6f),
                        120, Color.LightGoldenrodYellow, 0.5f);
                    glint.noGravity = true;
                }
            }
            else if (mode == 1) // Cardinal brands: red-orange sparks draw a compact cross through each mark.
            {
                Projectile.rotation = baseAngle;
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    Vector2 axis = baseAngle.ToRotationVector2();
                    Vector2 crossAxis = axis.RotatedBy(MathHelper.PiOver2);
                    bool perpendicular = ((int)Projectile.localAI[0] / 2) % 2 == 0;
                    Vector2 direction = perpendicular ? crossAxis : axis;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - direction * 7f, perpendicular ? DustID.RedTorch : DustID.OrangeTorch,
                        direction * (1.35f + charge * 1.3f), 80, Color.OrangeRed, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
            }
            else // Ashen Orbit: an increasingly tight, clockwise trail around the player.
            {
                float angle = baseAngle + (Projectile.localAI[0] - birthTick) * 0.018f;
                Projectile.Center = Main.player[targetIndex].Center + angle.ToRotationVector2()
                    * MathHelper.Lerp(440f, 360f, charge);
                Projectile.rotation = angle + MathHelper.PiOver2;
                if (Main.rand.NextBool(2))
                {
                    Vector2 tangent = (angle + MathHelper.PiOver2).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - tangent * 3f, DustID.OrangeTorch, tangent * (1.1f + charge),
                        80, Color.Orange, 0.52f + charge * 0.28f);
                    dust.noGravity = true;
                }
                if (charge > 0.7f && ((int)Projectile.localAI[0]) % 3 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.55f, 0.55f),
                        110, Color.LightGoldenrodYellow, 0.46f);
                    glint.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 70; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool glint = i % 5 == 0;
                Dust dust = Dust.NewDustPerfect(Projectile.Center,
                    glint ? DustID.AncientLight : DustID.GoldFlame,
                    direction * Main.rand.NextFloat(glint ? 5f : 1.8f, glint ? 10f : 6.8f),
                    glint ? 70 : 100,
                    glint ? Color.LightGoldenrodYellow : Color.Gold,
                    Main.rand.NextFloat(glint ? 0.4f : 0.65f, glint ? 0.8f : 1.35f));
                dust.noGravity = true;
            }
        }

        void ReleaseVFX(int mode, float baseAngle)
        {
            int sparks = mode == 2 ? 6 : 4;
            for (int i = 0; i < sparks; i++)
            {
                float angle = mode == 1
                    ? baseAngle + (i - (sparks - 1) * 0.5f) * 0.22f
                    : MathHelper.TwoPi * i / sparks + baseAngle;
                Vector2 velocity = angle.ToRotationVector2() * (1.6f + Main.rand.NextFloat(1.2f));
                int dustType = mode == 1 && i % 2 == 0 ? DustID.RedTorch : DustID.OrangeTorch;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 70, Color.OrangeRed, 0.75f);
                dust.noGravity = true;
            }

            if (mode == 0 || mode == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(2.2f, 2.2f),
                        90, Color.LightGoldenrodYellow, 0.65f);
                    glint.noGravity = true;
                }
            }
        }
    }
}
