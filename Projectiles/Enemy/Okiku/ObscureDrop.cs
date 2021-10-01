using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class ObscureDrop : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 15;
            projectile.height = 15;
            projectile.aiStyle = 1;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
        }
        public override bool PreKill(int timeLeft) {
            projectile.type = 44; //killpretendtype
            return true;
        }
        public override bool PreAI() {
            if (projectile.velocity.Y < 0) {
                projectile.alpha = 50;
                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 200, Color.White, 2.0f);
                    Main.dust[dust].noGravity = true;
                }
            }
            else {
                projectile.alpha = 10;
                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 62, 0, 0, 100, Color.White, 2.0f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;

            if (projectile.timeLeft == 170)
            {
                for (int i = 0; i < 2; i++)
                {
                    Projectile ObscureDropSplits = Main.projectile[Projectile.NewProjectile(projectile.position.X, projectile.position.Y, Main.rand.Next(-100, 100) / 10, projectile.velocity.Y, ModContent.ProjectileType<ObscureDrop>(), 40, 0f, Main.myPlayer)];
                    ObscureDropSplits.timeLeft = 169;
                }
            }

            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.Weak, 600);
                target.AddBuff(BuffID.OnFire, 180);
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 7200);
            }

            if (Main.rand.Next(8) == 0) {
                target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 1800);
            }
        }

        //This is too hard to see especially at night, so i'm making it ignore all lighting and always draw at full brightness
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = ModContent.GetTexture("tsorcRevamp/Projectiles/Enemy/Okiku/ObscureDrop");
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
