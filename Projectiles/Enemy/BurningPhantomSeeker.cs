using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class BurningPhantomSeeker : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            //projectile.maxUpdates = 2;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            this.Projectile.rotation = (float)Math.Atan2((double)this.Projectile.velocity.Y, (double)this.Projectile.velocity.X);

            if (this.Projectile.timeLeft < 100)
            {
                this.Projectile.scale *= 0.9f;
                this.Projectile.damage = 0;
            }

            if (this.Projectile.timeLeft > 150 && this.Projectile.timeLeft < 500)
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
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;
            Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 120 / expertScaling, false); //broken armor
            Main.player[Main.myPlayer].AddBuff(BuffID.OnFire, 600 / expertScaling, false); //on fire
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<FracturingArmor>(), 3600, false);

            if (Main.rand.NextBool(10))
            {
                Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600 / expertScaling, false); //slow
                Main.player[Main.myPlayer].AddBuff(BuffID.OnFire, 600 / expertScaling, false); //on fire
            }
        }

        public override void PostDraw(Color lightColor)
        {

            Texture2D MyTexture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()];
            Rectangle fromrect = new Rectangle(0, 0, this.Projectile.width, this.Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(251, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);

                Main.spriteBatch.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.1f * i) * this.Projectile.scale * scalemod,
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
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);

                Main.spriteBatch.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.09f * i) * this.Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}