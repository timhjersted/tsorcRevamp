using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class PhazonRound : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.scale = 0.7f;
            projectile.timeLeft = 240;
            projectile.hostile = false;
            projectile.tileCollide = true;
            projectile.friendly = true;
            projectile.ranged = true;
            projectile.extraUpdates = 1;
        }

        int storedDamage = 0;
        bool hasExploded = false;

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            target.AddBuff(ModContent.BuffType<Buffs.PhazonContamination>(), 600);            
        }

        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
            Lighting.AddLight(projectile.Center, Color.DarkBlue.ToVector3());

            if (storedDamage == 0)
            {
                projectile.damage = (int)(0.9f * projectile.damage);
                storedDamage = projectile.damage;
            }

            if (Main.rand.Next(0, 4) == 0)
            {
                int speed = 2;
                //Dust.NewDustPerfect(projectile.position, 29, Vector2.Zero, 120, default, 2f).noGravity = true;
                Dust.NewDustPerfect(projectile.position, DustID.FireworkFountain_Blue, (projectile.velocity * 0.6f) + Main.rand.NextVector2Circular(speed, speed), 200, default, 0.8f).noGravity = true;                
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            //Only the explosion does damage. Without this, both the impact *and* explosion do damage (annoying to balance, requires toning down explosion damage which makes it less useful)
            if (!hasExploded)
            {
                damage = 0;
            }
            else
            {
                damage = storedDamage;
            }
        }

        public override bool PreKill(int timeLeft)
        {
            for (int i = 0; i < 2; i++)
            {
                
                Projectile.NewProjectile(projectile.Center, Main.rand.NextVector2Circular(10, 10), ProjectileID.CrystalShard, projectile.damage / 4, 0.5f, projectile.owner);
                
            }

            projectile.penetrate = 20;
            projectile.width = 70;
            projectile.height = 70;
            hasExploded = true;
            projectile.Damage();

            int speed = 3;

            for (int i = 0; i < 10; i++)
            {
                //Dust.NewDustPerfect(projectile.position, 29, (projectile.velocity * 0.1f) + Main.rand.NextVector2Circular(speed, speed), 0, default, 3f).noGravity = true;
                Dust.NewDustPerfect(projectile.position, DustID.FireworkFountain_Blue, (projectile.velocity * Main.rand.NextFloat(0, 0.8f)) + Main.rand.NextVector2Circular(speed, speed), 200, default, 1.2f).noGravity = true;
            }
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PhazonRound];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = projectile.GetAlpha(lightColor);
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            return false;
        }
    }
}
