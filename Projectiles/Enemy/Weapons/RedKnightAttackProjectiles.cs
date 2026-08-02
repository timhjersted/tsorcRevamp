using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    internal enum KnightStandardMode
    {
        RedKnight,
        GreatCenter,
        GreatLeft,
        GreatRight
    }

    internal enum RedKnightBurstKind
    {
        SpearImpact,
        StandardImpact,
        BombExplosion
    }

    public class RedKnightLungeHitbox : ModProjectile
    {
        const float SourceOverlap = 22f;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Math.Max(1, (int)Projectile.ai[0]);
            Projectile.height = Math.Max(1, (int)Projectile.ai[1]);
            Projectile.timeLeft = Math.Max(1, (int)Projectile.ai[2]);
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (!globalProjectile.TryGetSourceNPC(out NPC sourceNPC) || !sourceNPC.active)
            {
                Projectile.Kill();
                return;
            }

            int direction = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.Center = sourceNPC.Center
                + new Vector2(direction * (Projectile.width * 0.5f - SourceOverlap), -4f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (globalProjectile.TryGetSourceNPC(out NPC sourceNPC)
                && sourceNPC.ModNPC is IHumanoidMeleeHitEffects hitEffects)
            {
                hitEffects.OnHumanoidMeleeHit(target);
            }
        }
    }

    public class RedKnightStandard : ModProjectile
    {
        Vector2 startPosition;
        bool initialized;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/BlackKnightSpear";

        KnightStandardMode Mode => (KnightStandardMode)(int)Projectile.ai[2];
        int FlightTicks => Mode == KnightStandardMode.RedKnight ? 24 : 30;
        int ChargeTicks => Mode switch
        {
            KnightStandardMode.RedKnight => 60,
            KnightStandardMode.GreatCenter => 135,
            _ => 75
        };
        Vector2 GroundPoint => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        Vector2 PlantedCenter => GroundPoint - new Vector2(0f, 20f);

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 62;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.scale = 0.8f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
        {
            int age = (int)Projectile.localAI[0];
            bool physicalSpear = Mode == KnightStandardMode.RedKnight || Mode == KnightStandardMode.GreatCenter;
            return physicalSpear && age < FlightTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = (PlantedCenter - startPosition).SafeNormalize(Vector2.UnitY);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - direction * 24f, Projectile.Center + direction * 24f,
                7f, ref collisionPoint);
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                startPosition = Projectile.Center;
            }

            int age = (int)Projectile.localAI[0]++;
            if (age < FlightTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, (age + 1f) / FlightTicks);
                Projectile.Center = Vector2.Lerp(startPosition, PlantedCenter, progress);
                Vector2 direction = PlantedCenter - startPosition;
                Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
                return;
            }

            Projectile.Center = PlantedCenter;
            Projectile.rotation = MathHelper.Pi;
            int plantedAge = age - FlightTicks;
            if (plantedAge == 0)
            {
                PlaySound(SoundID.Dig with { Volume = 0.65f, Pitch = -0.15f }, GroundPoint);
                SpawnBurst(RedKnightBurstKind.StandardImpact, GroundPoint, Mode == KnightStandardMode.RedKnight ? 0.75f : 1f);
                EmitPlantDust();
            }
            if (plantedAge == ChargeTicks)
            {
                FireWaves();
            }
            if (plantedAge > ChargeTicks + 35)
            {
                Projectile.Kill();
            }
        }

        void FireWaves()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (Mode)
                {
                    case KnightStandardMode.GreatLeft:
                        SpawnWave(1);
                        break;
                    case KnightStandardMode.GreatRight:
                        SpawnWave(-1);
                        break;
                    default:
                        SpawnWave(-1);
                        SpawnWave(1);
                        break;
                }
            }
            PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.4f }, GroundPoint);
            SpawnBurst(RedKnightBurstKind.StandardImpact, GroundPoint,
                Mode == KnightStandardMode.RedKnight ? 0.9f : 1.2f);
        }

        void EmitPlantDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                int dustType = i < 4 ? DustID.Stone : DustID.Torch;
                Dust dust = Dust.NewDustPerfect(GroundPoint + new Vector2(Main.rand.NextFloat(-7f, 7f), -2f),
                    dustType, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-2.8f, -0.7f)),
                    100, default, Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = dustType == DustID.Torch;
            }
        }

        void SpawnWave(int direction)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GroundPoint - new Vector2(0f, 9f),
                new Vector2(direction * (Mode == KnightStandardMode.RedKnight ? 6f : 7f), 0f),
                ModContent.ProjectileType<RedKnightGroundWave>(), Projectile.damage, 2.5f,
                Main.myPlayer, ai0: Mode == KnightStandardMode.RedKnight ? 210f : 260f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = (int)Projectile.localAI[0];
            if (age < FlightTicks)
            {
                Vector2 direction = (PlantedCenter - startPosition).SafeNormalize(Vector2.UnitY);
                RedKnightVFX.DrawSpearWake(Projectile.Center - direction * 26f, direction.ToRotation(),
                    new Vector2(78f, 17f), 0.55f, empowered: Mode != KnightStandardMode.RedKnight);
            }
            else
            {
                float progress = MathHelper.Clamp((age - FlightTicks) / (float)Math.Max(1, ChargeTicks), 0f, 1f);
                RedKnightVFX.DrawStandardCharge(GroundPoint, progress, Mode);
            }
            return true;
        }

        static void SpawnBurst(RedKnightBurstKind kind, Vector2 position, float scale)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(new EntitySource_Misc("RedKnightVFX"), position, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer, (float)kind, scale);
            }
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }
    }

    public class RedKnightGroundWave : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 48;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.localAI[0] += Math.Abs(Projectile.velocity.X);
            if (Projectile.localAI[0] >= Math.Max(64f, Projectile.ai[0]))
            {
                Projectile.Kill();
                return;
            }

            float groundY = PuppetGroundDustWave.FindGroundY(Projectile.Center.X, Projectile.Center.Y + 9f);
            Projectile.Center = new Vector2(Projectile.Center.X, groundY - Projectile.height * 0.5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.7f, 0.08f, 0.02f));

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f),
                    DustID.Torch, new Vector2(-Projectile.velocity.X * 0.08f, Main.rand.NextFloat(-2.4f, -0.8f)),
                    100, new Color(255, 70, 30), Main.rand.NextFloat(0.7f, 1.1f));
                ember.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            RedKnightVFX.DrawGroundWave(Projectile.Center, Projectile.velocity, new Vector2(54f, 20f), opacity);
            return false;
        }
    }

    public class RedKnightDelayedBomb : ModProjectile
    {
        const int FlightTicks = 36;
        const int PlantedTicks = 84;
        Vector2 startPosition;
        bool initialized;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyFirebomb";

        Vector2 GroundPoint => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        Vector2 RestingCenter => GroundPoint - new Vector2(0f, 9f);

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightTicks + PlantedTicks + 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                startPosition = Projectile.Center;
            }

            int age = (int)Projectile.localAI[0]++;
            if (age < FlightTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, (age + 1f) / FlightTicks);
                Projectile.Center = Vector2.Lerp(startPosition, RestingCenter, progress);
                Projectile.rotation += 0.24f;
                return;
            }

            Projectile.Center = RestingCenter;
            Projectile.rotation = 0f;
            int plantedAge = age - FlightTicks;
            if (plantedAge == 0)
            {
                PlaySound(SoundID.Dig with { Volume = 0.5f }, Projectile.Center);
            }
            if (plantedAge >= PlantedTicks)
            {
                Projectile.hostile = true;
                Projectile.Resize(120, 120);
                Projectile.Center = GroundPoint - new Vector2(0f, 40f);
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 2);
                Projectile.netUpdate = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.12f, 0.02f));
        }

        public override bool? CanDamage()
        {
            return Projectile.localAI[0] >= FlightTicks + PlantedTicks;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = (int)Projectile.localAI[0];
            float fuseProgress = age < FlightTicks ? 0f : MathHelper.Clamp((age - FlightTicks) / (float)PlantedTicks, 0f, 1f);
            Vector2 fusePoint = Projectile.Center + new Vector2(3f, -8f).RotatedBy(Projectile.rotation);
            RedKnightVFX.DrawBombFuse(fusePoint, fuseProgress, planted: age >= FlightTicks);
            return age < FlightTicks + PlantedTicks;
        }

        public override void OnKill(int timeLeft)
        {
            PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.25f }, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                    (float)RedKnightBurstKind.BombExplosion, 1f);
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 28; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(5.5f, 5.5f);
                    Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, velocity, 80,
                        new Color(255, 65, 25), Main.rand.NextFloat(1f, 1.8f));
                    ember.noGravity = true;
                }
            }
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }
    }

    public class RedKnightLightningLane : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        int Age => (int)Projectile.localAI[0];
        int TelegraphTicks => Math.Max(1, (int)Projectile.ai[0]);
        int ActiveTicks => Math.Max(1, (int)Projectile.ai[1]);
        float Length => Math.Max(64f, Projectile.ai[2]);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Age >= TelegraphTicks + ActiveTicks + 12)
            {
                Projectile.Kill();
            }
        }

        public override bool? CanDamage()
        {
            return Age >= TelegraphTicks && Age < TelegraphTicks + ActiveTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + direction * Length, 12f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Age / (float)TelegraphTicks, 0f, 1f);
            bool active = Age >= TelegraphTicks && Age < TelegraphTicks + ActiveTicks;
            float fade = Age < TelegraphTicks + ActiveTicks ? 1f
                : 1f - (Age - TelegraphTicks - ActiveTicks) / 12f;
            RedKnightVFX.DrawLightningLane(Projectile.Center, Projectile.velocity, Length, progress, active, fade);
            return false;
        }
    }

    public class CrimsonDominionController : ModProjectile
    {
        const int BuildTicks = 30;
        const int JudgmentTicks = 360;
        const int CollapseTicks = 60;
        const int RingTicks = 40;
        const float Radius = 420f;
        const float LaneWidth = 18f;
        const float InnerRadius = 62f;

        public override string Texture => "Terraria/Images/MagicPixel";

        int Age => (int)Projectile.localAI[0];
        int RotationDirection => Projectile.ai[0] < 0f ? -1 : 1;
        float BaseRotation => Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 540;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, new Vector3(0.45f, 0.03f, 0.01f));
        }

        bool InJudgment(out int beat, out int phase)
        {
            int local = Age - BuildTicks;
            beat = local >= 0 ? local / 60 : 0;
            phase = local >= 0 ? local % 60 : 0;
            return local >= 0 && local < JudgmentTicks;
        }

        bool RingActive => Age >= BuildTicks + JudgmentTicks + CollapseTicks
            && Age < BuildTicks + JudgmentTicks + CollapseTicks + RingTicks;

        public override bool? CanDamage()
        {
            if (InJudgment(out _, out int phase))
            {
                return phase >= 45 && phase < 55;
            }
            return RingActive;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (InJudgment(out int beat, out int phase) && phase >= 45 && phase < 55)
            {
                float angle = BaseRotation + RotationDirection * beat * MathHelper.Pi / 6f;
                Vector2 direction = angle.ToRotationVector2();
                float collisionPoint = 0f;
                bool forward = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    Projectile.Center + direction * InnerRadius, Projectile.Center + direction * Radius,
                    LaneWidth, ref collisionPoint);
                bool backward = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    Projectile.Center - direction * InnerRadius, Projectile.Center - direction * Radius,
                    LaneWidth, ref collisionPoint);
                return forward || backward;
            }

            if (RingActive)
            {
                float progress = (Age - BuildTicks - JudgmentTicks - CollapseTicks) / (float)RingTicks;
                float radius = MathHelper.Lerp(70f, Radius, progress);
                return RectangleIntersectsRing(targetHitbox, Projectile.Center, radius, 16f);
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            RedKnightVFX.DrawCrimsonDominion(Projectile.Center, Age, BaseRotation, RotationDirection);
            return false;
        }

        static bool RectangleIntersectsRing(Rectangle rectangle, Vector2 center, float radius, float width)
        {
            float closestX = MathHelper.Clamp(center.X, rectangle.Left, rectangle.Right);
            float closestY = MathHelper.Clamp(center.Y, rectangle.Top, rectangle.Bottom);
            float minimumDistance = Vector2.Distance(center, new Vector2(closestX, closestY));

            float maximumDistance = 0f;
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.TopLeft()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.TopRight()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.BottomLeft()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.BottomRight()));

            float inner = Math.Max(0f, radius - width * 0.5f);
            float outer = radius + width * 0.5f;
            return minimumDistance <= outer && maximumDistance >= inner;
        }
    }

    public class RedKnightVFXBurst : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.timeLeft = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / 24f;
            RedKnightVFX.DrawBurst((RedKnightBurstKind)(int)Projectile.ai[0], Projectile.Center,
                progress, Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1]);
            return false;
        }
    }
}
