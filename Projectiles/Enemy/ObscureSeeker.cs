using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class ObscureSeeker : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wave Attack");
        }

        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.height = 34;
            projectile.tileCollide = false;
            projectile.width = 34;
            projectile.timeLeft = 160;
            Main.projFrames[projectile.type] = 4;
            projectile.light = 1;

            projectile.width = 194;
            projectile.height = 194;
            drawOriginOffsetX = -96;
            drawOriginOffsetY = 94;
            Main.projFrames[projectile.type] = 7;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 2;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 41;
            return true;
        }

        public override void AI()
        {
            projectile.rotation++;
            this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X);
            if (this.projectile.timeLeft < 100)
            {
                this.projectile.scale *= 0.9f;
                this.projectile.damage = 0;
            }

            if (this.projectile.timeLeft > 200 && this.projectile.timeLeft < 500)
            {
                this.projectile.velocity.X -= (this.projectile.position.X - Main.player[(int)this.projectile.ai[0]].position.X) / 1000f;
                this.projectile.velocity.Y -= (this.projectile.position.Y - Main.player[(int)this.projectile.ai[0]].position.Y) / 1000f;

                this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X);
                this.projectile.velocity.Y = (float)Math.Sin(this.projectile.rotation) * 8;
                this.projectile.velocity.X = (float)Math.Cos(this.projectile.rotation) * 8;
            }

            lastpos[lastposindex] = this.projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 100, Color.Red, 2.0f);
            Main.dust[dust].noGravity = true;

            projectile.frameCounter++;
            if (projectile.frameCounter > 2)
            {
                projectile.frame++;
                projectile.frameCounter = 3;
            }
            if (projectile.frame >= 4)
            {
                projectile.frame = 0;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScale = 1;
            if (Main.expertMode) expertScale = 2;
            target.AddBuff(BuffID.Darkness, 18000 / expertScale, false);
            target.AddBuff(BuffID.Poisoned, 18000 / expertScale, false);
            target.AddBuff(BuffID.Bleeding, 18000 / expertScale, false);
            target.AddBuff(BuffID.BrokenArmor, 1200 / expertScale, false);
        }

    }
}