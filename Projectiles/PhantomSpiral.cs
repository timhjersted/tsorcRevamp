using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

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
            Projectile.timeLeft = 1000;
        }

        int distfrommaster = 0;
        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            if (Projectile.ai[2] != 0)
            {
                Projectile.rotation = Projectile.ai[2];
                Projectile.ai[2] = 0;
            }
            if (Projectile.timeLeft > 100)
            {
                Projectile.rotation -= (float)0.01;
                Projectile.position.X = Main.npc[(int)Projectile.ai[0]].position.X + (float)Math.Sin(Projectile.rotation) * distfrommaster;
                Projectile.position.Y = Main.npc[(int)Projectile.ai[0]].position.Y + (float)Math.Cos(Projectile.rotation) * distfrommaster;
                if (distfrommaster < Projectile.ai[1]) distfrommaster += 2;
            }
            else
            {
                Projectile.velocity.X = 0;
                Projectile.velocity.Y = 0;
                Projectile.scale *= 0.9f;
                Projectile.damage = 0;
            }
            lastpos[lastposindex] = Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;

            Main.player[Main.myPlayer].AddBuff(36, 120 / expertScaling, false); //broken armor
            Main.player[Main.myPlayer].AddBuff(39, 300 / expertScaling, false); //cursed inferno
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<FracturingArmor>(), 3600, false); //


            if (Main.rand.NextBool(10))
                Main.player[Main.myPlayer].AddBuff(35, 120 / expertScaling, false); //silenced
            Main.player[Main.myPlayer].AddBuff(32, 600 / expertScaling, false); //slow
            Main.player[Main.myPlayer].AddBuff(39, 600 / expertScaling, false); //cursed inferno
        }

        public override void PostDraw(Color lightColor)
        {
            Random rand1 = new Random((int)Main.GameUpdateCount);
            Rectangle fromrect = new Rectangle(0, 0, Projectile.width, Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += rand1.Next(-1, 1);
                lastpos[modlastposindex].Y += rand1.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(Projectile.width / 2, Projectile.height / 2);


                Main.EntitySpriteDraw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            Projectile.rotation + rotmod,
                            new Vector2(Projectile.width / 2, Projectile.height / 2),
                            1f * (0.1f * i) * Projectile.scale * scalemod,
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
                PC = lastpos[modlastposindex] + new Vector2(Projectile.width / 2, Projectile.height / 2);

                Main.EntitySpriteDraw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            Projectile.rotation + rotmod,
                            new Vector2(Projectile.width / 2, Projectile.height / 2),
                            1f * (0.09f * i) * Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}