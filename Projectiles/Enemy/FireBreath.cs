using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FireBreath : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Breath");

        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 23;
            projectile.hostile = true;
            projectile.height = 20;
            projectile.light = 1;
            projectile.magic = true;
            projectile.penetrate = 10;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 3000;
            projectile.width = 28;
        }
        public override bool PreKill(int timeLeft)
        {
            projectile.type = 102;
            return true;
        }

    }
}