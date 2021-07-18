using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class PurpleCrush : ModProjectile
    {
        public override void SetDefaults()
        {
            //projectile.aiStyle = 24;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.damage = 25;
            projectile.width = 16;
            //projectile.aiPretendType = 94;
            projectile.timeLeft = 100;
            projectile.light = .8f;
            drawOriginOffsetX = 13;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 44; //killpretendtype
            return true;
        }
        public override void AI()
        {
            projectile.rotation += 0.9f;
            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10)
            {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(22, 18000, false);
            target.AddBuff(30, 600, false);
            target.AddBuff(23, 180, false);
            target.AddBuff(32, 600, false);
            base.OnHitPlayer(target, damage, crit);
        }
    }
}