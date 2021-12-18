using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class JungleWyvernFire : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Breath of the Jungle");

        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 23;
            projectile.hostile = true;
            projectile.height = 28;
            projectile.width = 28;
            projectile.light = 1;
            projectile.magic = true;
            projectile.penetrate = 10;
            projectile.scale = 1;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
            projectile.width = 28;
            projectile.alpha = 255;
        }

        public override bool PreAI()
        {

            int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.CursedTorch, 0, 0, 70, default(Color), Main.rand.NextFloat(2.5f, 4f));
            Main.dust[dust].noGravity = true;

            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Poisoned, 2400);
            target.AddBuff(BuffID.Bleeding, 2400);
        }
    }
}