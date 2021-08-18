using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class BurningPhantomSeeker : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.width = 15;
            projectile.height = 15;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            //projectile.maxUpdates = 2;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            this.projectile.rotation = (float)Math.Atan2((double)this.projectile.velocity.Y, (double)this.projectile.velocity.X);

            if (this.projectile.timeLeft < 100)
            {
                this.projectile.scale *= 0.9f;
                this.projectile.damage = 0;
            }

            if (this.projectile.timeLeft > 150 && this.projectile.timeLeft < 500)
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
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;
            Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 120 / expertScaling, false); //broken armor
            Main.player[Main.myPlayer].AddBuff(BuffID.OnFire, 600 / expertScaling, false); //on fire
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 3600, false); 

            if (Main.rand.Next(10) == 0)
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600 / expertScaling, false); //slow
                Main.player[Main.myPlayer].AddBuff(BuffID.OnFire, 600 / expertScaling, false); //on fire
            }
        }

        public override void PostDraw(SpriteBatch sp, Color lightColor)
        {
            Texture2D MyTexture = Main.projectileTexture[ModContent.ProjectileType<Projectiles.Comet>()];
            Rectangle fromrect = new Rectangle(0, 0, this.projectile.width, this.projectile.height);
            Vector2 PC;
            Color targetColor = new Color(251, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);


                sp.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.projectile.rotation + rotmod,
                            new Vector2(this.projectile.width / 2, this.projectile.height / 2),
                            1f * (0.1f * i) * this.projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            targetColor = new Color(251, 53, 11);
            modlastposindex = lastposindex;

            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);

                sp.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.projectile.rotation + rotmod,
                            new Vector2(this.projectile.width / 2, this.projectile.height / 2),
                            1f * (0.09f * i) * this.projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}