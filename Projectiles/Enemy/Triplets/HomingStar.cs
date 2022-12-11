using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    class HomingStar : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seeking Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 200;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = false;
        }
        
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }


        float[] trailRotations = new float[6] { 0, 0, 0, 0, 0, 0 };
        bool playedSound = false;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f}, Projectile.Center);
                playedSound = true;
            }

            //Default homing strength
            float homingAcceleration = 0.15f;

            //Accelerate downwards, do not despawn until impact
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 100;
                if (Projectile.velocity.Y < 14f)
                {
                    Projectile.velocity.Y += 1f;
                }
                homingAcceleration = 0;
            }
            
            //No homing
            if (Projectile.ai[0] == 2)
            {
                homingAcceleration = 0;
            }

            //Perform homing
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            if (target != null)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 30, target.velocity, false);
            }
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);
        }

        float Progress(float progress)
        {
            float percent = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, progress, clamped: true);
            percent *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, percent);
        }

        Color ColorValue(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 3));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            //Main.NewText(timeFactor + 1);
            //result = ;
            result.A = 0;

            return result;
        }

        Texture2D texture;
        Texture2D starTexture;
        Texture2D flameJetTexture;
        ArmorShaderData data;
        int vertexCount = 0;
        BasicEffect effect;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            if (effect == null)
            {
                effect = new BasicEffect(Main.graphics.GraphicsDevice);
                effect.VertexColorEnabled = true;
                effect.FogEnabled = false;
                effect.View = Main.GameViewMatrix.TransformationMatrix;
                var viewport = Main.instance.GraphicsDevice.Viewport;
                effect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -1, 1);
            }
            effect.World = Matrix.CreateTranslation(-new Vector3(Main.screenPosition.X, Main.screenPosition.Y, 0));

            Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            effect.CurrentTechnique.Passes[0].Apply();

            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(Projectile.oldPos, Projectile.oldRot, ColorValue, Progress, includeBacksides: true);
            vertexStrip.DrawTrail();





            if(texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (starTexture == null || starTexture.IsDisposed)
            {
                starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Vector2 starOrigin = starSourceRectangle.Size() / 2f;

            //Draw shadow trails
            for (float i = 5; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, Projectile.oldPos[(int)i] - Main.screenPosition, sourceRectangle, Color.Cyan * ((6 - i) / 6), Projectile.oldRot[(int)i] - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(texture, Projectile.position - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            Vector2 starOffset = Projectile.velocity;
            starOffset.Normalize();
            starOffset *= 30;
            Main.spriteBatch.Draw(starTexture, Projectile.position - Main.screenPosition + starOffset, starSourceRectangle, Color.White, Projectile.rotation + starRotation, starOrigin, Projectile.scale * 0.75f, SpriteEffects.None, 0);
            starRotation += 0.1f;
            return false;
        }
    }
}
