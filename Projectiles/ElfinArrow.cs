using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    public class ElfinArrow : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Elfin Arrow");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.height = 5;
            Projectile.width = 5;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
        }

        float topSpeed = 14;
        float homingStrength = 0.005f;
        public override void AI()
        {
            if (!UsefulFunctions.IsTileReallySolid(Projectile.Center / 16))
            {
                Dust thisdust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0, 0, 0, default, 1f);
                thisdust.velocity = Vector2.Zero;
            }


            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            homingStrength += 0.035f;
            if (topSpeed < 24)
            {
                topSpeed += 0.02f;
            }
            if (Projectile.ai[0] >= 0)
            {
                Projectile target = Main.projectile[(int)Projectile.ai[0]];
                if (target != null && target.active && target.type == ModContent.ProjectileType<ElfinTargeting>())
                {
                    UsefulFunctions.SmoothHoming(Projectile, target.Center, 0.3f, 20, bufferZone: false);
                }
                else
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i] != null && Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<ElfinTargeting>())
                        {
                            Projectile.ai[0] = Main.projectile[i].whoAmI;
                        }
                    }
                }
            }
        }

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
}
