using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class ArcheologistWhip : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 6;
        }
        public override void SetDefaults()
        {
            projectile.aiStyle = 0;
            projectile.width = 180;
            projectile.height = 30;
            projectile.penetrate = 8;
            projectile.timeLeft = 16;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.tileCollide = false;
            projectile.hide = false;
            projectile.scale = 1f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 40, 118, 40), lightColor, projectile.rotation, new Vector2(20, 20), projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        public override void AI()
        {
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 3)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 6)
                {
                    projectile.frame = 0;
                }
            }

            if (projectile.ai[0] == 1)
            {
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WhipCrack").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
            }
            /*if (projectile.ai[0] <= 2f)
            {
                projectile.velocity.X = 2f;
            }*/

            projectile.rotation = projectile.velocity.ToRotation()/* + MathHelper.ToRadians(90f)*/; //simplified rotation code (no trig!)
            projectile.spriteDirection = projectile.direction; //this no work with npcs for some reason. Or me stupid?

        }


    }

}
