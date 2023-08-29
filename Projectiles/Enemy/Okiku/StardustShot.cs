using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku;

public class StardustShot : ModProjectile
{

    public override void SetDefaults()
    {
        Projectile.aiStyle = 0;
        Projectile.height = 16;
        Projectile.scale = 1.2f;
        Projectile.tileCollide = false;
        Projectile.width = 16;
        Projectile.hostile = false;
        Projectile.timeLeft = 420;
    }


    bool initialized = false;
    float timer = 60;
    bool randomize = false;
    int delay = 0;
    public override void AI()
    {
        timer--;

        if (!initialized && timer <= 0)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 targetVector = UsefulFunctions.GenerateTargetingVector(Projectile.Center, Main.player[(int)Projectile.ai[0]].Center, 5);

                targetVector += Main.rand.NextVector2Circular(2, 2);
                targetVector.Normalize();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, targetVector, ModContent.ProjectileType<StardustBeam>(), Projectile.damage, 0, Main.myPlayer, Projectile.ai[1], UsefulFunctions.EncodeID(Projectile));

                if (!randomize)
                {
                    break;
                }
            }
            Projectile.timeLeft = (int)delay + 220;
            initialized = true;
        }

        if (Main.rand.NextBool(2))
        {
            int dust = Dust.NewDust(new Vector2((float)Projectile.Center.X, (float)Projectile.Center.Y), Projectile.width, Projectile.height, 234, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, Color.White, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        Projectile.velocity.X *= .95f;
        Projectile.velocity.Y *= .95f;
        Projectile.rotation++;                        
    }
    static Texture2D texture;
    public override bool PreDraw(ref Color lightColor)
    {
        if (texture == null || texture.IsDisposed)
        {
            texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/StardustShot", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        }
        SpriteEffects spriteEffects = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }
        int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
        int startY = frameHeight * Projectile.frame;
        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
        Vector2 origin = sourceRectangle.Size() / 2f;
        //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
        Color drawColor = Projectile.GetAlpha(lightColor);
        Main.spriteBatch.Draw(texture,
            Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

        return false;
    }
}
