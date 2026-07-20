using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas solar ember: small burning fragment thrown out by a solar boulder detonation.
    ///Bounces off the ground up to three times, losing energy each bounce. ai[0] = gravity per tick.
    ///</summary>
    class GigasSolarEmber : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
            Projectile.light = 0.4f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 14f)
            {
                Projectile.velocity.Y = 14f;
            }

            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 80, default, 1.3f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.4f, 0.15f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 3)
            {
                return true; //third bounce spent — die
            }
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.8f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.7f;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f, Pitch = 0.5f }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, -1f, 80, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
