using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Heart of Winter left tap: one of the three crown icicles that form overhead and lance at the
    ///cursor in sequence. Spawn velocity = the locked aim direction (unit length); ai[0] = fire
    ///delay in ticks. The player version of the boss's crown.
    ///</summary>
    class TomeIcicle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        const float FireSpeed = 17f;

        int FireDelay => (int)Projectile.ai[0];
        bool Fired => Projectile.localAI[0] > FireDelay;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.3f;
        }

        public override bool? CanDamage()
        {
            return Fired;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Fired)
            {
                //Forming: crystallizing glints; the aim direction is already locked in velocity (unit length, so it barely drifts)
                if (Main.rand.NextBool(2))
                {
                    int glint = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceRod, 0f, 0f, 90, default, 0.9f);
                    Main.dust[glint].noGravity = true;
                    Main.dust[glint].velocity *= 0.2f;
                }
                if (Projectile.localAI[0] >= FireDelay)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * FireSpeed;
                    Projectile.tileCollide = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.Center);
                }
                return;
            }

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 100, default, 1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
