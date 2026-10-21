using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Oolacile's Dark Orb: a slow homing tracker.
    /// ai[0] = index of the player it homes on. ai[1] = 1 selects the Grand Occultist's SEEKER ORB tuning
    /// (slower accel, lower top speed, longer life) so its cast is something you kite away from instead of
    /// a fast shot; 0 keeps the legacy Oolacile Sorcerer behaviour untouched. ai[1] = 2 selects the Acolyte's ABYSS DROP:
    /// it falls from rest out of the portal ring, and bursts into an upward fan of Dark Beads on a wall or a player.
    /// </summary>
    public class OolacileDarkOrb : ModProjectile
    {
        // Legacy tuning: 0.1 accel to a 10 px/tick cap, 7 seconds of life.
        private const float LegacyAcceleration = 0.1f;
        private const float LegacyTopSpeed = 10f;
        private const int LegacyLifetime = 420;

        // Seeker Orb tuning: half the acceleration and a 6 px/tick cap — under a player's run speed, so it
        // can be outpaced and has to be baited into a wall or simply left behind.
        private const float SeekerAcceleration = 0.05f;
        private const float SeekerTopSpeed = 6f;
        private const int SeekerLifetime = 480;

        private const int FadeTicks = 18;

        // Abyss Drop: falls from rest at 0.25 px/t^2 to 9 px/t, drifting toward the target at most 2 px/t sideways.
        private const float DropGravity = 0.25f;
        private const float DropTopSpeed = 9f;
        private const float DropDriftAcceleration = 0.04f;
        private const float DropMaxDrift = 2f;
        private const int DropLifetime = 240;
        public const int DropBeadCount = 6;
        public const float DropBeadSpeed = 4f;
        public const int DropBeadDamage = 24;

        private bool SeekerMode => Projectile.ai[1] > 0.5f && Projectile.ai[1] < 1.5f;
        private bool DropMode => Projectile.ai[1] > 1.5f;
        private float Acceleration => SeekerMode ? SeekerAcceleration : LegacyAcceleration;
        private float TopSpeed => SeekerMode ? SeekerTopSpeed : LegacyTopSpeed;

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.scale = 2f;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.timeLeft = LegacyLifetime;
            Projectile.DamageType = DamageClass.Magic;
            Main.projFrames[Projectile.type] = 4;
            Projectile.light = 1;
        }

        public override void AI()
        {
            // Every peer sets its mode's lifetime on the first tick: OnSpawn only runs on the spawning machine and timeLeft
            // is not synced, so clients would otherwise keep the legacy 420 ticks. localAI[0] is the "set" flag.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (SeekerMode)
                {
                    Projectile.timeLeft = SeekerLifetime;
                }

                if (DropMode)
                {
                    Projectile.timeLeft = DropLifetime;
                }
            }

            Projectile.rotation += 0.5f;

            Player target = Main.player[(int)Projectile.ai[0]];

            if (DropMode)
            {
                DropAI(target);
            }
            else
            {
                HomingAI(target);
            }

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.Shadowflame, 0, 0, 50, Color.Purple, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);

            // Timing out on screen must not be a pop: shrink and fade over the last 18 ticks, and stop
            // dealing damage while it is visibly dissipating.
            if (Projectile.timeLeft < FadeTicks)
            {
                Projectile.damage = 0;
                Projectile.alpha = (int)(255f * (1f - Projectile.timeLeft / (float)FadeTicks));
                Projectile.scale = 2f * (Projectile.timeLeft / (float)FadeTicks);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
        }

        private void HomingAI(Player target)
        {
            if (target.position.X < Projectile.position.X && Projectile.velocity.X > -TopSpeed)
            {
                Projectile.velocity.X -= Acceleration;
            }
            if (target.position.X > Projectile.position.X && Projectile.velocity.X < TopSpeed)
            {
                Projectile.velocity.X += Acceleration;
            }
            if (target.position.Y < Projectile.position.Y && Projectile.velocity.Y > -TopSpeed)
            {
                Projectile.velocity.Y -= Acceleration;
            }
            if (target.position.Y > Projectile.position.Y && Projectile.velocity.Y < TopSpeed)
            {
                Projectile.velocity.Y += Acceleration;
            }
        }

        private void DropAI(Player target)
        {
            // tileCollide is not synced; every peer derives it from the synced mode, so the orb dies on the floor everywhere.
            Projectile.tileCollide = true;
            Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + DropGravity, DropTopSpeed);

            float driftDirection = System.Math.Sign(target.Center.X - Projectile.Center.X);
            Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X + driftDirection * DropDriftAcceleration, -DropMaxDrift, DropMaxDrift);
        }

        // Server-only: the 6-bead upward fan (180 degrees, 4 px/t) from wherever the drop burst.
        private void SpawnDropBeads()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < DropBeadCount; i++)
            {
                float fanProgress = i / (float)(DropBeadCount - 1);
                float angle = MathHelper.Lerp(-MathHelper.Pi, 0f, fanProgress);
                Vector2 velocity = angle.ToRotationVector2() * DropBeadSpeed;
                Vector2 spawn = Projectile.Center - new Vector2(0f, 8f);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, velocity, ModContent.ProjectileType<OolacileDarkBead>(),
                    DropBeadDamage, 0f, Main.myPlayer);
            }
        }

        // A real death event. This replaces the old PreKill, which reassigned Projectile.type to vanilla 44
        // so the engine would play THAT projectile's death instead — the orb had no death of its own.
        public override void OnKill(int timeLeft)
        {
            // A drop that times out mid-air just fades; one that struck something bursts into beads.
            if (DropMode && timeLeft > 0)
            {
                SpawnDropBeads();
            }

            if (Main.dedServ)
            {
                return;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.4f }, Projectile.Center);
            for (int i = 0; i < 70; i++)
            {
                Vector2 outward = Main.rand.NextVector2Circular(6f, 6f);
                Dust shard = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, outward, 40, Color.Purple,
                    Main.rand.NextFloat(1.2f, 2.2f));
                shard.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.Poisoned, 1200, false);
                target.AddBuff(BuffID.WitheredWeapon, 180, false);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 2400, false);
                target.AddBuff(BuffID.WitheredWeapon, 360, false);
            }
        }
    }
}
