using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class DemonSpirit : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 38;
            projectile.height = 46;
            projectile.aiStyle = 0;
            projectile.timeLeft = 120;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.damage = 20;
            projectile.friendly = false;
            projectile.penetrate = 3;
            projectile.light = .5f;
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

            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y - 10), projectile.width, projectile.height, DustID.Shadowflame, 0, 0, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;

            if (this.projectile.timeLeft <= 120)
            {
                this.projectile.scale = 0.7f;
                this.projectile.damage = 15;
            }
            if (this.projectile.timeLeft <= 110)
            {
                this.projectile.scale = 0.8f;
                this.projectile.damage = 20;
            }
            if (this.projectile.timeLeft <= 90)
            {
                this.projectile.scale = 0.9f;
                this.projectile.damage = 22;
            }
            if (this.projectile.timeLeft <= 70)
            {
                this.projectile.scale = 1f;
                this.projectile.damage = 25;
            }
            if (this.projectile.timeLeft <= 50)
            {
                this.projectile.scale = 1.2f;
                this.projectile.damage = 30;
            }
            if (this.projectile.timeLeft <= 30)
            {
                this.projectile.scale = 1.4f;
                this.projectile.damage = 40;
            }

            this.projectile.ai[0] += 1f;


            this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X) + 1.57f;
            if (this.projectile.velocity.Y > 16f)
            {
                this.projectile.velocity.Y = 16f;
                return;
            }
        }
    }
}