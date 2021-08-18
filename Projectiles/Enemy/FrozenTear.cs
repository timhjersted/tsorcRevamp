using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FrozenTear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Frozen Orb");

        }
        public override void SetDefaults()
        {
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
            projectile.type = 44;
            return true;
        }

        public override void AI()
        {
            int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 250, default(Color), 1f);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertMultiplier = 1;
            if (Main.expertMode) expertMultiplier = 2;
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 600, false);
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 30, false);
            Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600 / expertMultiplier, false);
        }
    }
}