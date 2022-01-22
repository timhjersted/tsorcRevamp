using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class SmallFlameJet : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ThrowingAxe";

        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.height = 10;
            projectile.penetrate = -1;
            projectile.width = 10;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void AI()
        {
            projectile.alpha = 255;

            if (Main.rand.Next(4) == 0)
            {
                int z = Dust.NewDust(projectile.position, projectile.width, projectile.height, 6, 0f, 0f, 120, default(Color), Main.rand.NextFloat(1.5f, 3.5f));
                Main.dust[z].noGravity = true;
            }

        }

    }
}
