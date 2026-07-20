using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///One drain tick of the Necromancer's Soul Siphon: an instant, invisible pulse placed on the
    ///player by the channel. The tether visual lives in the Necromancer's AI — this only carries
    ///the bite. Dies in a few ticks whether it connects or not.
    ///</summary>
    class NecroSiphonPulse : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 4;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            for (int i = 0; i < 3; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 60, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.4f;
            }
        }
    }
}
