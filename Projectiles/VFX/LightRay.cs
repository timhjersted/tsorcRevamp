using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    public class LightRay : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.width = 10;
            Projectile.height = 250;
        }

        Color lightColor
        {
            get
            {
                return Main.hslToRgb(Projectile.ai[1], 255, 255);
            }
        }
        Color BeamColor
        {
            get
            {
                return UsefulFunctions.ColorFromFloat(Projectile.ai[1]);
            }
        }
        int style
        {
            get
            {
                return (int)Projectile.ai[0];
            }
        }

        float beamWidth;
        float beamLength;
        int direction;
        bool initialized = false;
        public override void AI()
        {
            if (direction == 0)
            {
                beamWidth = Main.rand.Next(10, 110);
                Projectile.rotation = Main.rand.NextVector2CircularEdge(1, 1).ToRotation();
                if (Main.rand.NextBool())
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
            }

            if (style == 0)
            {
                beamLength = 800;
            }
            else if (style == 1)
            {
                beamLength += 100;
            }
            else if (style == 2)
            {
                beamLength = 1800;
                Projectile.rotation += 0.003f * direction;
            }
            else if (style == 3)
            {
                beamLength += 100;
                Projectile.rotation += 0.003f * direction;
            }
            else if (style == 4)
            {
                if(!initialized)
                {
                    beamWidth = 25;
                    Projectile.timeLeft = 15;
                    initialized = true;
                }

                beamLength += 100;
                if (beamLength > Projectile.ai[2])
                {
                    beamLength = Projectile.ai[2];
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            if (Projectile.timeLeft == 1)
            {
                Projectile.timeLeft++;
                beamWidth *= 0.90f;
                if (beamWidth < 1)
                {
                    Projectile.Kill();
                }
            }
        }

        public static Effect effect;
        public static ArmorShaderData data;
        public static ArmorShaderData targetingData;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/LightBeam", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)beamLength, (int)beamWidth);

            //Pass relevant data to the shader via these parameters
            effect.Parameters["Color"].SetValue(BeamColor.ToVector3());
            effect.Parameters["SecondaryColor"].SetValue(new Vector3(1, 1, 1));
            effect.Parameters["FadeOut"].SetValue(1);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly * 100);
            effect.Parameters["ProjectileSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["TextureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}