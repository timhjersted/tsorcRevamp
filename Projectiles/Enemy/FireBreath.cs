using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FireBreath : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Fire Breath");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.hostile = true;
            Projectile.height = 20;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 10;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 3000;
            Projectile.width = 28;
        }

        // ---------------------------------------------------------------------------------------
        // Vanilla aiStyle 23 is reimplemented here instead of being allowed to run, for two reasons
        // that could not be fixed from ModProjectile.AI() (which runs AFTER the vanilla AI has
        // already spawned its dust):
        //
        //  1. BLOCKINESS. Vanilla spawns ONE DustID.Torch per tick and then, two times in three,
        //     multiplies its scale by 3.0 and then by 1.5 -- so with NewDust's own +/-20% jitter the
        //     dust reached scale 5.40 (an 8px dust sprite drawn at ~43px). That single number is
        //     why every fire-breath in the mod reads as a column of orange LEGO bricks.
        //  2. NO ROTATION. Vanilla ends with `rotation += 0.3f * (float)direction`, but
        //     Projectile.direction is never assigned for a plain hostile projectile spawned through
        //     Projectile.NewProjectile -- it stays 0, so the increment is 0 and the flame sprite is
        //     frozen for its whole life. This is what made the fire teleport ring look static.
        //
        // Everything else is a faithful port: constant velocity, the 60-tick timeLeft clamp (callers
        // pass 3000), and the same 8-tick warm-up + 0.25/0.5/0.75/1.0 scale ramp.
        //
        // NOTE: this projectile is SHARED. AncientDemon, AncientOolacileDemon, AncientDemonOfTheAbyss,
        // SoulOfCinder, DemonLordApocalypse, Massacre, TaurusKnight, the GreatSerpent (SerpentAI) and
        // the shared fire-teleport burst (tsorcRevampAIs.SpawnFireTeleportBurst) all fire it, so this
        // change is intentionally mod-wide.
        //
        // DustID.Lava (35) was evaluated as the body dust and REJECTED -- see the note on
        // SpawnBreathDust below.
        // ---------------------------------------------------------------------------------------
        public override bool PreAI()
        {
            if (Projectile.timeLeft > 60)
            {
                Projectile.timeLeft = 60;
            }

            if (Projectile.ai[0] > 7f)
            {
                // Vanilla's spawn-in ramp: the first three dust-emitting ticks are quarter, half and
                // three-quarter size so the flame grows out of the mouth rather than popping.
                float ramp = 1f;
                if (Projectile.ai[0] == 8f) ramp = 0.25f;
                else if (Projectile.ai[0] == 9f) ramp = 0.5f;
                else if (Projectile.ai[0] == 10f) ramp = 0.75f;
                Projectile.ai[0] += 1f;
                SpawnBreathDust(ramp);
            }
            else
            {
                Projectile.ai[0] += 1f;
            }

            // The rotation vanilla intended but never got, because `direction` is 0 here. Slightly
            // slower than vanilla's 0.3 rad/tick (which at 60fps is nearly 3 revolutions/second and
            // reads as a strobe on a 38x36 sprite) and randomised per instance so a ring of flames
            // does not spin in lockstep (vfx-shader-tips section 33).
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Main.rand.NextBool() ? 1f : -1f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
            Projectile.rotation += 0.20f * Projectile.localAI[0];

            return false;
        }

        /// <summary>
        /// Three layered dust passes instead of vanilla's single chunky one (VFX_ARSENAL / the
        /// vfx-dust-tips skill's 3-layer rule): a tamed body, a fine fast foreground, and an
        /// occasional trailing ember.
        /// </summary>
        void SpawnBreathDust(float ramp)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 sideways = new(-forward.Y, forward.X);

            // BODY -- vanilla's pass, kept as the recognisable core of the effect but with its
            // maximum scale cut by exactly 25%: the 3.0 * 1.5 = 4.5 multiplier becomes 3.375, so
            // with NewDust's +/-20% jitter the ceiling drops 5.40 -> 4.05 and the floor 3.60 -> 2.70.
            //
            // DustID.Lava (35) was checked as a replacement and is WRONG for a mouth flame: with
            // noGravity it GROWS (+0.03 scale/tick in Dust.UpdateDust, i.e. +1.8 over a 60t life --
            // the exact opposite of what we want here), and without noGravity it is a lava *bubble*
            // that deletes itself the instant it is not inside a liquid (`if (!WetCollision) scale
            // = 0`). It also feeds the ambient lavaBubbles counter. DustID.Torch stays.
            for (int i = 0; i < 2; i++)
            {
                Dust body = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Torch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100);
                if (Main.rand.Next(3) != 0)
                {
                    body.noGravity = true;
                    body.scale *= 3.375f;   // was 3f then *1.5f == 4.5f
                    body.velocity *= 2f;
                }
                else
                {
                    body.scale *= 1.125f;   // was 1.5f
                }
                body.velocity *= 1.2f;
                body.scale *= ramp;
                // The second of the pair is deliberately the smaller, wider-spread one so the two
                // never stack into one bigger blob -- count buys density, scale does not.
                if (i == 1)
                {
                    body.scale *= 0.62f;
                    body.velocity += sideways * Main.rand.NextFloat(-1.4f, 1.4f);
                }
            }

            // FOREGROUND -- 2 small, fast, short-lived motes per tick that outrun the body and break
            // its silhouette up. These are what stop the column reading as a stack of squares.
            for (int i = 0; i < 2; i++)
            {
                Dust fine = Dust.NewDustPerfect(
                    Projectile.Center + sideways * Main.rand.NextFloat(-9f, 9f),
                    DustID.Torch,
                    forward * Main.rand.NextFloat(1.4f, 3.6f) + sideways * Main.rand.NextFloat(-1.1f, 1.1f),
                    120, default, Main.rand.NextFloat(0.5f, 0.95f) * ramp);
                fine.noGravity = true;
            }

            // TRAILING EMBER -- one in three ticks, a cooler orange mote left behind the stream so
            // the breath has a tail instead of ending exactly where the projectile does.
            if (Main.rand.NextBool(3))
            {
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(0f, 12f),
                    DustID.OrangeTorch,
                    -forward * Main.rand.NextFloat(0.3f, 1.2f) + sideways * Main.rand.NextFloat(-0.5f, 0.5f),
                    150, default, Main.rand.NextFloat(0.6f, 1.0f) * ramp);
                ember.noGravity = true;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            //projectile.type = 102; //makes cool explosion dust but also annoying exploding sound :/
            // ai[1] == 1 marks a purely decorative flame (the second, non-damaging fire-teleport
            // ring). Those skip the flamethrower cue so 20 simultaneous flames do not stack into a
            // roar; the damaging ring still announces itself.
            if (Projectile.ai[1] != 1f)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item34 with { Volume = 0.1f, Pitch = -0.2f }, Projectile.Center); //flamethrower
            }
            return true;
        }

    }
}
