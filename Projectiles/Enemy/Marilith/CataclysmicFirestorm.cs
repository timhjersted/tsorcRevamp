using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class CataclysmicFirestorm : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cataclysmic Firestorm");
        }

        public override void SetDefaults()
        {
            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        float size = 0;
        int dustCount = 0;
        Vector2 truePosition; 
        float maxSize = 8000;
        float explosionTime = 4000;
        public override void AI()
        {
            maxSize = 8000;
            explosionTime = 4000;
            if(truePosition == Vector2.Zero)
            {
                truePosition = Projectile.Center;
            }

            Projectile.Center = Main.LocalPlayer.Center;
            Projectile.timeLeft = 2;
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            
            
            if (size < maxSize)
            {
                size += (maxSize / explosionTime) * 3.5f;
                dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
            }
            else
            {
                Projectile.Kill();
                return;
            }

            /*
            for (int j = 0; j < dustCount; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                Vector2 dustPos = truePosition + dir;
                if (EnemyGenericLaser.FastContainsPoint(screenRect, dustPos))
                {
                    dir.Normalize();
                    Vector2 dustVel = dir;
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 0.5f);
                    d.noGravity = true;
                    if (!Main.rand.NextBool(4))
                    {
                        d.noLight = true;
                    }
                }
            }*/
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //behindNPCsAndTiles.Add(index);
        }

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(truePosition, targetHitbox.Center.ToVector2());
            if (distance < size && distance > size - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static ArmorShaderData data;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/FireWaveShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "FireWaveShaderPass");
            }

            //Pass the size parameter in through the "saturation" variable, because there isn't a "size" one
            data.UseSaturation(1.55f * size / maxSize);

            //Apply the shader
            data.Apply(null);

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.tNoiseTexture1.Width, tsorcRevamp.tNoiseTexture1.Height);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.tNoiseTexture1, truePosition - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * 2.5f, recsize, Color.White, 0, Vector2.Zero, 2.5f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.OnFire, 450, false);
            }
            else
            {
                target.AddBuff(BuffID.OnFire, 900, false);
            }
        }
    }
}