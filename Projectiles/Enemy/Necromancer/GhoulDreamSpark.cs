using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///The Spellbound Ghoul's Dream Spark: a slow purple wisp bolt with a faint drift toward its
    ///target — easy to sidestep alone, dangerous layered over the Necromancer's pressure.
    ///</summary>
    class GhoulDreamSpark : ModProjectile
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
            Projectile.timeLeft = 160;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (target != null && !target.dead)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, 0.04f, 7f, target.velocity, false);
            }

            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 90, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.25f;
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.45f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, vel.X, vel.Y, 70, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
