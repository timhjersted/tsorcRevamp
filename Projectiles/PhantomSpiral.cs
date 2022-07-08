using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PhantomSpiral : ModProjectile
    {

        private const string TexturePath = "tsorcRevamp/Projectiles/Comet";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 2;
        }

        int distfrommaster = 0;
        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            if (this.Projectile.timeLeft > 100)
            {
                this.Projectile.rotation -= (float)0.01;
                this.Projectile.position.X = Main.npc[(int)this.Projectile.ai[0]].position.X + (float)Math.Sin(this.Projectile.rotation) * distfrommaster;
                this.Projectile.position.Y = Main.npc[(int)this.Projectile.ai[0]].position.Y + (float)Math.Cos(this.Projectile.rotation) * distfrommaster;
                if (distfrommaster < this.Projectile.ai[1]) distfrommaster += 2;
            }
            else
            {
                this.Projectile.velocity.X = 0;
                this.Projectile.velocity.Y = 0;
                this.Projectile.scale *= 0.9f;
                this.Projectile.damage = 0;
            }
            lastpos[lastposindex] = this.Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;

            Main.player[Main.myPlayer].AddBuff(36, 120 / expertScaling, false); //broken armor
            Main.player[Main.myPlayer].AddBuff(39, 300 / expertScaling, false); //cursed inferno
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 3600, false); //


            if (Main.rand.NextBool(10))
                Main.player[Main.myPlayer].AddBuff(35, 120 / expertScaling, false); //silenced
            Main.player[Main.myPlayer].AddBuff(32, 600 / expertScaling, false); //slow
            Main.player[Main.myPlayer].AddBuff(39, 600 / expertScaling, false); //cursed inferno
        }

        public override void PostDraw(Color lightColor)
        {
            Random rand1 = new Random((int)Main.GameUpdateCount);
            Texture2D MyTexture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()];
            Rectangle fromrect = new Rectangle(0, 0, this.Projectile.width, this.Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += rand1.Next(-1, 1);
                lastpos[modlastposindex].Y += rand1.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);


                Main.EntitySpriteDraw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.1f * i) * this.Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            targetColor = new Color(0, 0, 255, 0);
            modlastposindex = lastposindex;
            rand1 = new Random((int)Main.GameUpdateCount);

            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);

                Main.EntitySpriteDraw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.09f * i) * this.Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}