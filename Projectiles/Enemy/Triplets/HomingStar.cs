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
            float lerpPercent = 1f;
            float startLerpValue = Utils.GetLerpValue(0f, 0.001f, progress, clamped: true);
            lerpPercent *= 1f - (1f - startLerpValue) * (1f - startLerpValue);
            float width = MathHelper.Lerp(0f, 100f, lerpPercent);

            if(progress < 0.1f)
            {
                return 50f;
            }

            return 50;
        }

        Color ColorValue(float progress)
        {
            float timeFactor = (float)Math.Sin((Main.GlobalTimeWrappedHourly * 2));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, progress + (timeFactor + 1) / 2);

            //result = ;
            result.A = 0;

            return Color.White;
            return result;
        }

        Texture2D texture;
        Texture2D flameJetTexture;
        ArmorShaderData data;
        int vertexCount = 0;
        public override bool PreDraw(ref Color lightColor)
        {
            /*
            MiscShaderData ShaderData = GameShaders.Misc["RainbowRod"];
            ShaderData.UseSaturation(-.8f);
            ShaderData.UseOpacity(1f);
            ShaderData.Apply();
            */

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/FireWallShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "FireWallShaderPass");
            }

            if (flameJetTexture == null || flameJetTexture.IsDisposed)
            {
                flameJetTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }


            //Pass relevant data to the shader via these parameters
            //data.UseSaturation(Projectile.ai[0]);
            //data.UseSecondaryColor(1, 0, Main.GlobalTimeWrappedHourly);

            /*
            Effect thisEffect = , Main.LocalPlayer).Shader;
            thisEffect.Parameters["uTexture"].SetValue(flameJetTexture);
            thisEffect.Parameters["uTexture2"].SetValue(flameJetTexture);
            thisEffect.Parameters["Progress"].SetValue(Main.GlobalTimeWrappedHourly * -1f);
            thisEffect.Parameters["xMod"].SetValue(1.5f);
            thisEffect.Parameters["StartColor"].SetValue(Color.Blue.ToVector4());
            thisEffect.Parameters["MidColor"].SetValue(Color.Blue.ToVector4());
            thisEffect.Parameters["EndColor"].SetValue(Color.Blue.ToVector4());
            thisEffect.CurrentTechnique.Passes[0].Apply();
            */


            //This works
            /*
            MiscShaderData ShaderData = GameShaders.Misc["RainbowRod"];
            ShaderData.UseSaturation(-.8f);
            ShaderData.UseOpacity(1f);
            ShaderData.Apply();
            */

            //This doesn't lol
            //GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer).Apply();


            Vector2 zoom = Main.GameViewMatrix.Zoom;
            Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) * Matrix.CreateTranslation(Main.screenWidth / 2, Main.screenHeight / -2, 0) * Matrix.CreateRotationZ(MathHelper.Pi) * Matrix.CreateScale(zoom.X, zoom.Y, 1f);
            Matrix projection = Matrix.CreateOrthographic(Main.screenWidth, Main.screenHeight, 0, 1000);

            
            Effect effect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/FireTrailShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uTransform"].SetValue(Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1));


            TestVertexStrip vertexStrip = new TestVertexStrip();
            for(int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                vertexCount = i;
                if (Projectile.oldPos[i] == Vector2.Zero)
                {
                    break;
                }
            }
            vertexStrip.PrepareStrip(Projectile.oldPos, Projectile.oldRot, ColorValue, Progress, Projectile.Size / 2f - Main.screenPosition, null, false);
            vertexStrip.DrawTrail();
            //Main.pixelShader.CurrentTechnique.Passes[0].Apply();


            
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;


            //Draw a dot on every vertex, for debugging
            for (int i = 0; i < vertexStrip._vertexAmountCurrentlyMaintained; i++)
            {
                Main.spriteBatch.Draw(texture, vertexStrip._vertices[i].Position, sourceRectangle, Color.White, 0, origin, .05f, SpriteEffects.None, 0);
            }
            
            return false;
        }
    }
}
