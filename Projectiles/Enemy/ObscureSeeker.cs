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
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.width = 34;
            Projectile.timeLeft = 160;
            Main.projFrames[Projectile.type] = 4;
            Projectile.light = 1;

            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 41;
            return true;
        }

        public override void AI()
        {
            Projectile.rotation++;
            this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);
            if (this.Projectile.timeLeft < 100)
            {
                this.Projectile.scale *= 0.9f;
                this.Projectile.damage = 0;
            }

            if (this.Projectile.timeLeft > 200 && this.Projectile.timeLeft < 500)
            {
                this.Projectile.velocity.X -= (this.Projectile.position.X - Main.player[(int)this.Projectile.ai[0]].position.X) / 1000f;
                this.Projectile.velocity.Y -= (this.Projectile.position.Y - Main.player[(int)this.Projectile.ai[0]].position.Y) / 1000f;

                this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);
                this.Projectile.velocity.Y = (float)Math.Sin(this.Projectile.rotation) * 8;
                this.Projectile.velocity.X = (float)Math.Cos(this.Projectile.rotation) * 8;
            }

            lastpos[lastposindex] = this.Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, 100, Color.Red, 2.0f);
            Main.dust[dust].noGravity = true;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
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