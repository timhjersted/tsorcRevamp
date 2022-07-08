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
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.width = 10;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void AI()
        {
            Projectile.alpha = 255;

            if (Main.rand.NextBool(4))
            {
                int z = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0f, 0f, 120, default(Color), Main.rand.NextFloat(1.5f, 3.5f));
                Main.dust[z].noGravity = true;
            }

        }

    }
}
