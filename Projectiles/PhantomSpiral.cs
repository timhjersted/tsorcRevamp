using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PhantomSpiral : ModProjectile
    {

        private const string TexturePath = "tsorcRevamp/Projectiles/Comet";

        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.width = 15;
            projectile.height = 15;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.MaxUpdates = 2;
        }

        int distfrommaster = 0;
        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            if (this.projectile.timeLeft > 100)
            {
                this.projectile.rotation -= (float)0.01;
                this.projectile.position.X = Main.npc[(int)this.projectile.ai[0]].position.X + (float)Math.Sin(this.projectile.rotation) * distfrommaster;
                this.projectile.position.Y = Main.npc[(int)this.projectile.ai[0]].position.Y + (float)Math.Cos(this.projectile.rotation) * distfrommaster;
                if (distfrommaster < this.projectile.ai[1]) distfrommaster += 2;
            }
            else
            {
                this.projectile.velocity.X = 0;
                this.projectile.velocity.Y = 0;
                this.projectile.scale *= 0.9f;
                this.projectile.damage = 0;
            }
            lastpos[lastposindex] = this.projectile.position;
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

       
            if(Main.rand.Next(10) == 0)
            Main.player[Main.myPlayer].AddBuff(35, 120 / expertScaling, false); //silenced
            Main.player[Main.myPlayer].AddBuff(32, 600 / expertScaling, false); //slow
            Main.player[Main.myPlayer].AddBuff(39, 600 / expertScaling, false); //cursed inferno
         }

        public override void PostDraw(SpriteBatch sp, Color lightColor)
        {
        Random rand1 = new Random((int)Main.time);
            Texture2D MyTexture = Main.projectileTexture[ModContent.ProjectileType<Projectiles.Comet>()];
            Rectangle fromrect = new Rectangle(0, 0, this.projectile.width, this.projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += rand1.Next(-1, 1);
                lastpos[modlastposindex].Y += rand1.Next(-1, 1);
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
            targetColor = new Color(0, 0, 255, 0);
            modlastposindex = lastposindex;
            rand1 = new Random((int)Main.time);

            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
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