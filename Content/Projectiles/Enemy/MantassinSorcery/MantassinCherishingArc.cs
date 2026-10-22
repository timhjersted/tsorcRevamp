using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.MantassinSorcery
{
    // Cherishing Light: one of three curved beams of light orbiting the Mantassin (the user's take on Cherishing Fingers —
    // arcs, not literal fingers). Each is 70 degrees of a 96px circle; three of them leave three 50-degree gaps.
    // ai[0] = owner NPC whoAmI, ai[1] = this arc's angle offset (0, 120, 240 degrees).
    // Timeline: TraceTicks drawing in from one tip (harmless) -> ActiveTicks orbiting at SpinPerTick -> FadeTicks curling
    //   away. The angle is a pure function of the local age, which starts on the same tick everywhere, so every peer
    //   agrees on where the arcs are without syncing it.
    // While active an arc: damages players touching it (annular segment); destroys the local player's projectiles that
    //   enter it; and blocks melee from inside its angle (the NPC asks IsAngleCovered in ModifyHitBy*).
    // Ends early (shattered, harmless shard dust) when the owner is gone or no longer casting it — a poise break kills
    //   the arcs from the server.
    public class MantassinCherishingArc : ModProjectile
    {
        public const int TraceTicks = 40;
        public const int ActiveTicks = 180;
        public const int FadeTicks = 15;
        public const float ArcRadius = 96f;
        private const float BandInner = 80f;
        private const float BandOuter = 112f;
        private const float BlockInner = 66f;
        private const float BlockOuter = 126f;
        public const float HalfAngle = 35f * MathHelper.Pi / 180f;
        private const float SpinPerTick = 1.5f * MathHelper.Pi / 180f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int OwnerIndex => (int)Projectile.ai[0];
        private float AngleOffset => Projectile.ai[1];

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // Client-local block/hit flash, 1 -> 0. Purely visual.
        private float flash;

        private bool Active => Age > TraceTicks && Age <= TraceTicks + ActiveTicks;

        /// <summary>The arc's middle angle (radians) at a given age. Spins only once the trace-in is done.</summary>
        private float MiddleAngle => AngleOffset + Math.Max(0, Age - TraceTicks) * SpinPerTick;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TraceTicks + ActiveTicks + FadeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            if (!Active)
            {
                return false;
            }
            return null;
        }

        /// <summary>True when a point at `angle` (radians, from the owner's centre) falls inside this arc's 70 degrees.</summary>
        public bool CoversAngle(float angle)
        {
            if (!Active)
            {
                return false;
            }
            float difference = MathHelper.WrapAngle(angle - MiddleAngle);
            return Math.Abs(difference) <= HalfAngle;
        }

        public void Flash()
        {
            flash = 1f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            Vector2 offset = closest - Projectile.Center;
            float distance = offset.Length();
            if (distance < BandInner || distance > BandOuter)
            {
                return false;
            }
            return CoversAngle(offset.ToRotation());
        }

        public override void AI()
        {
            Age++;
            flash = Math.Max(0f, flash - 0.08f);

            NPC owner = OwnerIndex >= 0 && OwnerIndex < Main.maxNPCs ? Main.npc[OwnerIndex] : null;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center + MiddleAngle.ToRotationVector2() * ArcRadius, EnemyVFX.MantassinMid.ToVector3() * 0.6f);

            if (!Main.dedServ)
            {
                // Light running along the arc (traced portion only while drawing in).
                float traceFraction = MathHelper.Clamp(Age / (float)TraceTicks, 0f, 1f);
                float along = Main.rand.NextFloat(-HalfAngle, -HalfAngle + 2f * HalfAngle * traceFraction);
                Vector2 point = Projectile.Center + (MiddleAngle + along).ToRotationVector2() * ArcRadius;
                Dust mote = Dust.NewDustPerfect(point, DustID.TintableDustLighted,
                    (MiddleAngle + along + MathHelper.PiOver2).ToRotationVector2() * 0.8f, 50, EnemyVFX.MantassinMid, 0.7f);
                mote.noGravity = true;
            }

            if (Active)
            {
                BlockLocalProjectiles();
            }
        }

        // Each client destroys ITS OWN projectiles that touch an arc (owner == myPlayer), so no client ever kills another
        // player's projectile. Minions, sentries, pets, whips, yoyos, flails, spears and other held melee projectiles
        // are left alone: killing those would delete the player's weapon or summon, not block a shot.
        private void BlockLocalProjectiles()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile shot = Main.projectile[i];
                if (!shot.active || !shot.friendly || shot.hostile || shot.damage <= 0 || shot.owner != Main.myPlayer)
                {
                    continue;
                }

                bool heldOrSummoned = shot.minion || shot.sentry || Main.projPet[shot.type] || shot.ownerHitCheck
                    || ProjectileID.Sets.IsAWhip[shot.type] || shot.aiStyle == ProjAIStyleID.Yoyo
                    || shot.aiStyle == ProjAIStyleID.Flail || shot.aiStyle == ProjAIStyleID.Spear;
                if (heldOrSummoned)
                {
                    continue;
                }

                Vector2 offset = shot.Center - Projectile.Center;
                float distance = offset.Length();
                if (distance < BlockInner || distance > BlockOuter || !CoversAngle(offset.ToRotation()))
                {
                    continue;
                }

                flash = 1f;
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.5f, Pitch = 0.4f }, shot.Center);
                for (int j = 0; j < 8; j++)
                {
                    Dust spark = Dust.NewDustPerfect(shot.Center, DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 30, EnemyVFX.MantassinCore, 0.9f);
                    spark.noGravity = true;
                }
                shot.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Natural end curls away quietly (the fade in PreDraw); an early end — poise break, owner gone — shatters.
            bool endedEarly = Age < TraceTicks + ActiveTicks;
            if (!endedEarly)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCHit5 with { Volume = 0.45f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 18; i++)
            {
                float along = Main.rand.NextFloat(-HalfAngle, HalfAngle);
                Vector2 outward = (MiddleAngle + along).ToRotationVector2();
                Dust shard = Dust.NewDustPerfect(Projectile.Center + outward * ArcRadius, DustID.GemDiamond,
                    outward * Main.rand.NextFloat(1f, 3f), 60, default, 0.9f);
                shard.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float trace = MathHelper.Clamp(Age / (float)TraceTicks, 0f, 1f);
            float opacity = 1f;
            int fadeAge = Age - TraceTicks - ActiveTicks;
            if (fadeAge > 0)
            {
                opacity = 1f - MathHelper.Clamp(fadeAge / (float)FadeTicks, 0f, 1f);
            }
            if (!Active && fadeAge <= 0)
            {
                // Still tracing in: dimmer, so the drawn path reads as a warning before it bites.
                opacity *= 0.55f;
            }

            EnemyVFX.DrawMantassinCrescent(Projectile.Center, ArcRadius, MiddleAngle, trace, flash, opacity);
            return false;
        }
    }
}
