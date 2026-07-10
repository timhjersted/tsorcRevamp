using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Small ice shard (vanilla ice-spike texture): the shrapnel of the Ice Gigas kit. Thrown by giant
    ///hailstone impacts, shattering statues, prison pillars and the stagger ice-shed.
    ///ai[0] = gravity per tick (0 = straight).
    ///</summary>
    class GigasIceShard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 180;
            Projectile.scale = 0.9f;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 14f)
            {
                Projectile.velocity.Y = 14f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, 0f, 0f, 100, default, 0.9f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f, Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Ice, vel.X, vel.Y, 60, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
