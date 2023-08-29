using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles;

public class IdealArrow : ModProjectile
{

    public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Ideal Arrow");
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.height = 5;
        Projectile.width = 5;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 360;
        Projectile.aiStyle = -1;
        //Missing: aiStyle = 1;
        //Vanilla aiStyles are kinda janky. We can do better with just a few lines below in AI()...
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2; //This makes it rotate to face where it's moving
                                                                                     //projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!
        Projectile.velocity.Y += 0.07f;

        Dust thisdust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0, 0, 0, default, 1f); //This creates a dust trail
        thisdust.velocity = Vector2.Zero; //This makes the dust stay still instead of wandering randomly
    }

    //The explosion of dust upon hitting an enemy
    public override bool PreKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = Projectile.velocity + Main.rand.NextVector2Circular(5, 5);
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.MagicMirror, vel, 10, default, 2);
            d.noGravity = true;
            d.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
        }
        return true;
    }

    //Disabled multiple effects due to lag. The game is really not a fan of rapidly beginning and ending dozens of shaded spriteBatches every frame haha...
    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
        data.Apply(null);

        SpriteEffects spriteEffects = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }
        //Get the premultiplied, properly transparent texture
        Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ElfinArrow];
        int frameHeight = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
        int startY = frameHeight * Projectile.frame;
        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
        Vector2 origin = sourceRectangle.Size() / 2f;
        for (int i = 0; i < 9; i++)
        {
            Main.spriteBatch.Draw(texture,
              Projectile.oldPos[9 - i] - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
              sourceRectangle, Color.White * (0.15f * i), Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
        }
        Main.spriteBatch.Draw(texture,
            Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
            sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }
}
