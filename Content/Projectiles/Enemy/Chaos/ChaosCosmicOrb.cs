using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Chaos's orbiting hazard, thrown one at a time by Orbital Cosmos. Replaces the vanilla DemonSickle
    ///with bespoke art, but keeps that projectile's aiStyle 18 rhythm by hand: constant speed for the first
    ///<see cref="AccelDelayTicks"/> ticks, then a per-tick <see cref="AccelRate"/> ramp until
    ///<see cref="AccelRampEndTicks"/>, capped at <see cref="TopSpeed"/>. Steers gently toward the nearest
    ///player for the first <see cref="HomingTicks"/>, then commits to whatever heading it has and flies
    ///straight from there.
    ///</summary>
    class ChaosCosmicOrb : ModProjectile
    {
        const int TicksPerFrame = 6;
        const float SpinRate = 0.8f;               // radians/tick, same fast tumble the sickle had
        const int AccelDelayTicks = 30;             // travels at throw speed before ramping
        const int AccelRampEndTicks = 100;          // ramp ends here, same window as vanilla DemonSickle
        const float AccelRate = 1.06f;              // per-tick velocity multiplier during the ramp
        const float TopSpeed = 80f;                 // caps the ramp; was uncapped and reached ~95px/tick
        const int HomingTicks = 120;                // light steering window before it commits to a heading
        const float HomingTurnRate = 0.035f;        // per-tick lerp toward the target direction — light, not a lock
        const int AbyssDebuffTicks = 7 * 60;        // 7 seconds

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.light = 0.2f;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            Projectile.rotation += SpinRate;

            Projectile.ai[0]++;
            if (Projectile.ai[0] >= AccelDelayTicks && Projectile.ai[0] < AccelRampEndTicks)
            {
                Projectile.velocity *= AccelRate;

                if (Projectile.velocity.LengthSquared() > TopSpeed * TopSpeed)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * TopSpeed;
                }
            }

            // Homing decisions are target picks — server (and singleplayer) only, per the MP authority rule.
            // Clients still see the curve: netUpdate resyncs the velocity every 10 ticks while it is steering.
            if (Projectile.ai[1] < HomingTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player homingTarget = Main.player[Player.FindClosest(Projectile.position, Projectile.width, Projectile.height)];

                if (homingTarget.active && !homingTarget.dead)
                {
                    Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    Vector2 wantedDirection = (homingTarget.Center - Projectile.Center).SafeNormalize(currentDirection);
                    Vector2 turnedDirection = Vector2.Lerp(currentDirection, wantedDirection, HomingTurnRate).SafeNormalize(currentDirection);

                    Projectile.velocity = turnedDirection * Projectile.velocity.Length();
                }

                Projectile.ai[1]++;

                if (Projectile.ai[1] % 10 == 0)
                {
                    Projectile.netUpdate = true;
                }
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            Lighting.AddLight(Projectile.Center, 0.5f, 0.15f, 0.85f);

            //Every other tick: enough to trail the orb without burying the arena in dust.
            if (Main.dedServ || Projectile.timeLeft % 2 != 0)
            {
                return;
            }

            int dustType = DustID.Shadowflame;
            if (Main.rand.NextBool(3))
            {
                dustType = DustID.DemonTorch;
            }

            Dust mote = Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.1f, 80, default, Main.rand.NextFloat(1.1f, 1.6f));
            mote.noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Abyss>(), AbyssDebuffTicks, false);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                Vector2 burst = Main.rand.NextVector2Circular(3f, 3f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, burst, 80, default, Main.rand.NextFloat(1.2f, 1.8f));
                mote.noGravity = true;
            }
        }
    }
}
