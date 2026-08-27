using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Winter's Embrace icicle: spawns on the vibrating ring around GigasFrostZone's centre, holds
    ///position and shakes in place for VibrateTicks as an anticipation beat, then launches straight
    ///out along its pre-assigned radial angle. Three sets fire in sequence (GigasFrostZone), each
    ///rotated 15° from the last, so the gaps between lanes shift wave to wave instead of leaving one
    ///safe spot for the whole attack. ai[0] = launch angle (radians). ai[1] = vibrate ticks.
    ///</summary>
    class GigasEmbraceIcicle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        const float LaunchSpeed = 8.5f;
        const float VibrateAmount = 2.5f; //px of jitter around the spawn point

        float LaunchAngle => Projectile.ai[0];
        int VibrateTicks => (int)Projectile.ai[1];
        int Timer => (int)Projectile.localAI[0];
        bool Launched => Timer > VibrateTicks;

        //Deterministically set from the already-synced spawn position in OnSpawn, which runs once
        //on every peer — safe without its own network slot, unlike a value computed per-client.
        Vector2 anchor;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.25f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            anchor = Projectile.Center;
            Projectile.rotation = LaunchAngle + MathHelper.PiOver2;
        }

        public override bool? CanDamage()
        {
            return Launched; //harmless while vibrating — that's the telegraph, not the hit
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (!Launched)
            {
                //Held on the ring, shaking in place. `anchor`, not Projectile.Center, is the
                //reference point so the jitter doesn't random-walk away from its ring position
                //over the full 50 ticks.
                Projectile.Center = anchor + Main.rand.NextVector2Circular(VibrateAmount, VibrateAmount);
                Projectile.velocity = Vector2.Zero;
                if (Main.rand.NextBool(2))
                {
                    int glint = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceRod, 0f, 0f, 90, default, 0.9f);
                    Main.dust[glint].noGravity = true;
                    Main.dust[glint].velocity *= 0.15f;
                }
                if (Timer == VibrateTicks)
                {
                    Projectile.Center = anchor; //snap back to true position for a clean launch line
                    Projectile.velocity = LaunchAngle.ToRotationVector2() * LaunchSpeed;
                    Projectile.tileCollide = true;
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.5f }, Projectile.Center);
                }
                return;
            }

            //In flight
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 90, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Chilled, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
