using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class CrystalFire : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal Fire");

        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 8;
            projectile.hostile = true;
            projectile.width = 16;
            projectile.height = 16;
            projectile.tileCollide = true;
            projectile.damage = 21;
            projectile.timeLeft = 350;
            projectile.light = .8f;
            projectile.penetrate = 2;
            projectile.magic = true;

            Main.projFrames[projectile.type] = 4;
        }
    }
}