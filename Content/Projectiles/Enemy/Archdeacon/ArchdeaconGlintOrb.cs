using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;

namespace tsorcRevamp.Content.Projectiles.Enemy.Archdeacon
{
    // The Archdeacon's glintstone orb, two modes (ai[0]):
    //   Great: released slowly from the staff tip, speeds up toward where it was aimed. Sheds a ring of resting
    //          ArchdeaconGlintStars every RingIntervalTicks, each ring rotated half a step so the gaps move, then bursts
    //          into a final, larger ring. ai[1] = rings * 100 + stars per ring (the caster raises both below 50% life).
    //   Burst: lobbed to a point; it decelerates to a stop there over TravelTicks, flashes for FlashTicks, then bursts.
    // Art: ArchdeaconGlintOrb.png is a copy of BlueExplosion (52x260, 5 frames of 52x52; frame 0 is the full sphere, 1-4 the burst), drawn additive.
    // Death: every ending (scheduled burst or hitting a wall) plays the 4 burst frames over 12t, harmless, then removes it.
    // MP: the schedule runs off a local age that starts on the same tick everywhere; only the server spawns stars.
    public class ArchdeaconGlintOrb : ModProjectile
    {
        public enum Mode
        {
            Great = 0,
            Burst = 1,
        }

        public const int TravelTicks = 40;
        public const int FlashTicks = 30;
        private const int FrameCount = 5;
        private const int BurstFrameTicks = 3;
        private const int DyingTicks = (FrameCount - 1) * BurstFrameTicks;
        private const int FirstRingAge = 30;
        private const int RingIntervalTicks = 24;
        private const float GreatAcceleration = 0.05f;
        private const float GreatTopSpeed = 4f;
        private const int RingStarHangTicks = 16;
        private const float RingStarAcceleration = 0.2f;
        private const int BurstStarCount = 10;
        private const int BurstStarHangTicks = 6;
        private const float BurstStarAcceleration = 0.22f;

        private Mode OrbMode => (Mode)(int)Projectile.ai[0];
        private int RingCount => (int)Projectile.ai[1] / 100;
        private int StarsPerRing => (int)Projectile.ai[1] % 100;

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // 0 = alive; otherwise the age at which the burst began.
        private int DyingSince
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private float _burstDeceleration;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            if (DyingSince > 0)
            {
                return false;
            }

            return null;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            ArchdeaconGlintStar.ApplyHitDebuffs(target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BeginBurst();
            return false;
        }

        public override void AI()
        {
            Age++;

            if (DyingSince > 0)
            {
                Projectile.velocity = Vector2.Zero;
                int dyingAge = Age - DyingSince;
                Projectile.frame = Math.Min(1 + dyingAge / BurstFrameTicks, FrameCount - 1);

                if (dyingAge >= DyingTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (Age == 1)
            {
                if (OrbMode == Mode.Burst)
                {
                    Projectile.Resize(24, 24);
                    // Spawned at v0 = 2d / TravelTicks; losing v0 / TravelTicks a tick stops it on the point.
                    _burstDeceleration = Projectile.velocity.Length() / TravelTicks;
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
                }
            }

            if (OrbMode == Mode.Great)
            {
                UpdateGreat();
            }
            else
            {
                UpdateBurst();
            }

            Lighting.AddLight(Projectile.Center, ArchdeaconGlintStar.GlintColor.ToVector3() * 0.9f);

            if (!Main.dedServ)
            {
                SwirlRibbons();
            }
        }

        private void UpdateGreat()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = Math.Min(Projectile.velocity.Length() + GreatAcceleration, GreatTopSpeed);
            Projectile.velocity = direction * speed;

            int sinceFirstRing = Age - FirstRingAge;
            bool ringBeat = sinceFirstRing >= 0 && sinceFirstRing % RingIntervalTicks == 0;
            int ringIndex = sinceFirstRing / RingIntervalTicks;

            if (ringBeat && ringIndex < RingCount)
            {
                // Odd rings are rotated half a step, so the gaps the last ring left are now filled.
                float stepRadians = MathHelper.TwoPi / StarsPerRing;
                float offsetRadians = 0f;
                if (ringIndex % 2 == 1)
                {
                    offsetRadians = stepRadians * 0.5f;
                }

                SpawnRing(StarsPerRing, offsetRadians, RingStarHangTicks, RingStarAcceleration);
            }

            bool finalBeat = ringBeat && ringIndex == RingCount;
            if (finalBeat)
            {
                BeginBurst();
            }
        }

        private void UpdateBurst()
        {
            float speed = Math.Max(Projectile.velocity.Length() - _burstDeceleration, 0f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * speed;

            int flashAge = Age - TravelTicks;
            bool flashing = flashAge > 0;

            if (flashing && !Main.dedServ)
            {
                // Converging sparkles: the burst is coming, get clear of the point or roll it.
                Vector2 offset = Main.rand.NextVector2CircularEdge(70f, 70f);
                Dust converge = Dust.NewDustPerfect(Projectile.Center + offset, DustID.TintableDustLighted, -offset * 0.06f, 40,
                    ArchdeaconGlintStar.GlintCore, 1f);
                converge.noGravity = true;

                if (flashAge == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
            }

            if (flashAge >= FlashTicks)
            {
                BeginBurst();
            }
        }

        // The orb's final ring (Great: stars per ring + 4; Burst: BurstStarCount) and the burst animation.
        private void BeginBurst()
        {
            if (DyingSince > 0)
            {
                return;
            }

            DyingSince = Age;

            if (OrbMode == Mode.Great)
            {
                SpawnRing(StarsPerRing + 4, 0f, RingStarHangTicks, RingStarAcceleration);
            }
            else
            {
                SpawnRing(BurstStarCount, 0f, BurstStarHangTicks, BurstStarAcceleration);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
            }
        }

        // Resting stars around the orb pointing outward; the server spawns them, they accelerate out from rest.
        // Unaimed, so only open air matters: a slot inside a wall (orb hugging terrain) is skipped.
        private void SpawnRing(int count, float offsetRadians, int hangTicks, float acceleration)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float angle = offsetRadians + MathHelper.TwoPi * i / count;
                Vector2 outward = angle.ToRotationVector2();
                Vector2 point = Projectile.Center + outward * 20f;
                if (!ArchdeaconGlintStar.SpotIsOpen(point))
                {
                    continue;
                }

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, outward,
                    ModContent.ProjectileType<ArchdeaconGlintStar>(), Projectile.damage, 0f, Main.myPlayer,
                    hangTicks, (float)ArchdeaconGlintStar.Mode.Fixed, acceleration);
            }
        }

        // Three dust ribbons on a tilted, rotating ellipse around the orb (the swirling trails of a great glintstone).
        private void SwirlRibbons()
        {
            float radius = 30f;
            if (OrbMode == Mode.Burst)
            {
                radius = 20f;
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = Age * 0.18f + MathHelper.TwoPi * i / 3f;
                Vector2 onEllipse = new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius * 0.45f).RotatedBy(0.5f + i * 1.05f);
                Dust ribbon = Dust.NewDustPerfect(Projectile.Center + onEllipse, DustID.TintableDustLighted, Projectile.velocity * 0.5f, 50,
                    ArchdeaconGlintStar.GlintColor, 0.9f);
                ribbon.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sheet = TextureAssets.Projectile[Type].Value;
            int frameHeight = sheet.Height / FrameCount;
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, sheet.Width, frameHeight);
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            Color glow = new Color(255, 255, 255, 0);

            float sphereScale = 0.9f;
            if (OrbMode == Mode.Burst)
            {
                sphereScale = 0.55f;
            }

            bool flashing = OrbMode == Mode.Burst && Age > TravelTicks && DyingSince == 0;
            float pulse = 1f + 0.06f * (float)Math.Sin(Age * 0.3f);
            if (flashing)
            {
                pulse = 1f + 0.18f * (float)Math.Sin(Age * 0.9f);
            }

            Main.EntitySpriteDraw(sheet, drawCenter, frame, glow, 0f, frame.Size() * 0.5f, sphereScale * pulse, SpriteEffects.None, 0);

            // White-hot spinning core while it is whole.
            if (DyingSince == 0)
            {
                Texture2D core = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar))).Value;
                Color coreColor = new Color(ArchdeaconGlintStar.GlintCore.R, ArchdeaconGlintStar.GlintCore.G, ArchdeaconGlintStar.GlintCore.B, 0);
                Main.EntitySpriteDraw(core, drawCenter, null, coreColor, Age * 0.12f, core.Size() * 0.5f, sphereScale * pulse * 1.1f, SpriteEffects.None, 0);
            }

            return false;
        }
    }
}
