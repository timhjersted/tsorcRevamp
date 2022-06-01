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
            Main.projFrames[Projectile.type] = 6;
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.width = 180;
            Projectile.height = 30;
            Projectile.penetrate = 8;
            Projectile.timeLeft = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.hide = false;
            Projectile.scale = 1f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 40, 118, 40), lightColor, Projectile.rotation, new Vector2(20, 20), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 6)
                {
                    Projectile.frame = 0;
                }
            }

            if (Projectile.ai[0] == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/WhipCrack").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
            }
            /*if (projectile.ai[0] <= 2f)
            {
                projectile.velocity.X = 2f;
            }*/

            Projectile.rotation = Projectile.velocity.ToRotation()/* + MathHelper.ToRadians(90f)*/; //simplified rotation code (no trig!)
            Projectile.spriteDirection = Projectile.direction; //this no work with npcs for some reason. Or me stupid?

        }


    }

}
