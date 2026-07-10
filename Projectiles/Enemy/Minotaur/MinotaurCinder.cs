using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Ring of Cinders fragment: an arcing ember thrown out radially by the Minotaur Mage's
    ///desperation burst. ai[0] = gravity per tick. Dies on tiles with a small flare.
    ///</summary>
    class MinotaurCinder : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 180;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 12f)
            {
                Projectile.velocity.Y = 12f;
            }

            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 80, default, 1.3f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
            if (Main.rand.NextBool(4))
            {
                int spark = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, -0.5f, 100, default, 0.8f);
                Main.dust[spark].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.45f, 0.25f, 0.08f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y - 1f, 80, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
