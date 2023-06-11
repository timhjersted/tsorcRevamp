using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class SyntheticFirestorm : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cataclysmic Firestorm");
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
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        float size = 0;
        int dustCount = 0;
        Vector2 truePosition; 
        float maxSize = 8000;
        float explosionTime = 4000;
        public override void AI()
        {
            maxSize = 1200;
            Projectile.rotation += 0.02f;
            if(Projectile.rotation > MathHelper.TwoPi)
            {
                Projectile.rotation -= MathHelper.TwoPi;
            }

            float distance = Vector2.Distance(truePosition, Main.LocalPlayer.Center);
            if (distance < size && UsefulFunctions.CompareAngles(Vector2.Normalize(truePosition - Main.LocalPlayer.Center), Projectile.rotation.ToRotationVector2()) > MathHelper.Pi - angle / 2f)
            {
                Main.NewText("Colliding! " + size);
            }
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
                size += (8000 / explosionTime) * 3.5f;
                dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            //behindNPCsAndTiles.Add(index);
        }

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(truePosition, targetHitbox.Center.ToVector2());
            if (distance < size)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Effect effect;
        public float angle;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/SyntheticFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            angle = MathHelper.TwoPi / 6f;
            float shaderRotation = Projectile.rotation + (MathHelper.Pi - angle / 2f);
            shaderRotation %= MathHelper.TwoPi;
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 312);


            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.tNoiseTexture1.Width, tsorcRevamp.tNoiseTexture1.Height);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.tNoiseTexture1, truePosition - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * 2.5f, recsize, Color.White, 0, Vector2.Zero, 2.5f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
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