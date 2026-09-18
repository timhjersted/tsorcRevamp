using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Oolacile's Dark Orb: a slow homing tracker.
    /// ai[0] = index of the player it homes on. ai[1] = 1 selects the Grand Occultist's SEEKER ORB tuning
    /// (slower accel, lower top speed, longer life) so its cast is something you kite away from instead of
    /// a fast shot; 0 keeps the legacy Oolacile Sorcerer behaviour untouched.
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

        private bool SeekerMode => Projectile.ai[1] > 0.5f;
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

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (SeekerMode)
            {
                Projectile.timeLeft = SeekerLifetime;
            }
        }

        public override void AI()
        {
            Projectile.rotation += 0.5f;

            Player target = Main.player[(int)Projectile.ai[0]];

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

        // A real death event. This replaces the old PreKill, which reassigned Projectile.type to vanilla 44
        // so the engine would play THAT projectile's death instead — the orb had no death of its own.
        public override void OnKill(int timeLeft)
        {
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
