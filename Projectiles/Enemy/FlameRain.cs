using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FlameRain : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 14;
            //projectile.pretendType = 85;
            aiType = 79;
            projectile.alpha = 100;
            projectile.aiStyle = 1;
            projectile.timeLeft = 600;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 1;
            projectile.light = 0.8f;
            projectile.tileCollide = true;
            projectile.magic = true;

            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.height = 34;
            projectile.tileCollide = false;
            projectile.width = 34;
            projectile.timeLeft = 150;
            projectile.light = .3f;
            Main.projFrames[projectile.type] = 4;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 15;
            return true;
        }
    }
}