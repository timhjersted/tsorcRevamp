using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Enemy.WyvernMage
{
    class TaintedIcicle : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 200;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57f; //90 degrees in radians!

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * .65f);
        }

        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
