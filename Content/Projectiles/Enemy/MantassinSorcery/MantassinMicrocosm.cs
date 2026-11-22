using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.MantassinSorcery
{
    // Fleeting Microcosm, after the Elden Ring finger sorcery (user's reference shot): a ~400px disc whose black interior
    // is full of stars, with a white rim, a gold haze and an X of lens-flare spikes. ER's own text: it pulses once in a
    // single wave, then disappears in a burst.
    // Timeline (ticks from spawn): 1..FormTicks the void opens inside a gold guide ring drawn at the FULL hit radius from
    //   tick 1; then PulseTicks of damage (the whole disc, once per player via their i-frames); then CollapseTicks of a
    //   harmless implode-and-burst.
    // Collision is the drawn disc: the target hitbox's closest point within HitRadius of the centre.
    // Stands where it was cast (the caster's facing is locked and it stands still through the cast).
    // MP: the timeline runs off a local age that starts on the same tick everywhere; nothing here is decided.
    public class MantassinMicrocosm : ModProjectile
    {
        public const float HitRadius = 200f;
        public const int FormTicks = 45;
        private const int PulseTicks = 8;
        private const int CollapseTicks = 15;
        private const float FlareLengthScale = 1.6f; // spike length as a multiple of the disc diameter

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private bool Pulsing => Age > FormTicks && Age <= FormTicks + PulseTicks;

        /// <summary>True until the pulse (a poise break on the caster cancels it while it forms).</summary>
        public bool IsForming => Age <= FormTicks;

        public override void SetStaticDefaults()
        {
            // The disc and its spikes reach far past the tiny hitbox's own bounds.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
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
            Projectile.timeLeft = FormTicks + PulseTicks + CollapseTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            if (!Pulsing)
            {
                return false;
            }
            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= HitRadius * HitRadius;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, EnemyVFX.MantassinGold.ToVector3() * 0.5f);

            if (Main.dedServ)
            {
                return;
            }

            if (Age == 1)
            {
                SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.7f, Pitch = -0.3f }, Projectile.Center);
            }

            if (Age <= FormTicks)
            {
                // Stars spiralling inward from just outside the hit radius, plus gold motes riding the guide ring —
                // the dust marks exactly the radius the pulse will hit.
                Vector2 outward = Main.rand.NextVector2Unit();
                Vector2 tangent = new Vector2(-outward.Y, outward.X);
                Dust star = Dust.NewDustPerfect(Projectile.Center + outward * (HitRadius + 50f), DustID.TintableDustLighted,
                    -outward * 5f + tangent * 2.5f, 40, EnemyVFX.MantassinCore, 0.9f);
                star.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    Vector2 ringPoint = Main.rand.NextVector2Unit() * HitRadius;
                    Dust ring = Dust.NewDustPerfect(Projectile.Center + ringPoint, DustID.GoldFlame,
                        Vector2.Zero, 80, default, 0.8f);
                    ring.noGravity = true;
                }
                return;
            }

            if (Age == FormTicks + 1)
            {
                // The pulse: the rim flashes and one wave of light runs out to the ring.
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.6f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
                UsefulFunctions.ScreenShake(Projectile.Center, 5f, 12, distanceFalloff: 900f);
                for (int i = 0; i < 60; i++)
                {
                    Vector2 outward = (MathHelper.TwoPi * i / 60f).ToRotationVector2();
                    Dust wave = Dust.NewDustPerfect(Projectile.Center + outward * HitRadius * 0.85f, DustID.TintableDustLighted,
                        outward * 3f, 30, EnemyVFX.MantassinCore, 1.2f);
                    wave.noGravity = true;
                }
                return;
            }

            if (Age == FormTicks + PulseTicks + 1)
            {
                // The collapse: star specks and gold sparks burst out of the imploding void. Harmless.
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                for (int i = 0; i < 50; i++)
                {
                    Dust speck = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(9f, 9f), 30, EnemyVFX.MantassinCore, 1.0f);
                    speck.noGravity = true;
                }
                for (int i = 0; i < 24; i++)
                {
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
                        Main.rand.NextVector2Circular(7f, 7f), 30, default, 1.1f);
                    spark.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float open;
            float flash = 0f;
            float opacity = 1f;

            if (Age <= FormTicks)
            {
                // Ease-out: opens fast then settles, so the black void reads early.
                float form = Age / (float)FormTicks;
                open = 1f - (1f - form) * (1f - form);
                opacity = MathHelper.Clamp(Age / 8f, 0f, 1f);
            }
            else if (Age <= FormTicks + PulseTicks)
            {
                open = 1f;
                flash = 1f - (Age - FormTicks - 1) / (float)PulseTicks;
            }
            else
            {
                // Implode: the void shrinks back to a point and everything fades.
                float collapse = MathHelper.Clamp((Age - FormTicks - PulseTicks) / (float)CollapseTicks, 0f, 1f);
                open = 1f - collapse;
                opacity = 1f - collapse * collapse;
            }

            float flareRotation = MathHelper.PiOver4 + Age * 0.008f;
            float flareLength = HitRadius * 2f * FlareLengthScale * Math.Max(open, 0.05f);
            EnemyVFX.DrawMantassinMicrocosm(Projectile.Center, HitRadius, open, flash, opacity, flareRotation, flareLength);
            return false;
        }
    }
}
