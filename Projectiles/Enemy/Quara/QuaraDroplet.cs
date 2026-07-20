using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Burst-bubble droplet: arcs under gravity, splashes on tiles. ai[0] = gravity per tick.
    ///</summary>
    class QuaraDroplet : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 150;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 11f)
            {
                Projectile.velocity.Y = 11f;
            }

            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 80, default, 1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Wet, 4 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.3f, Pitch = 0.5f }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, vel.X, vel.Y - 1f, 60, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
