using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class RealityBomb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.timeLeft = DetonationTime;
            Projectile.hostile = true;
        }

        int DetonationTime = 120;
        float DetonationProgress = 0;

        float detonationPercent
        {
            get => 1f - (DetonationProgress / DetonationTime);
        }

        public override void AI()
        {
            DetonationProgress++;
            Projectile.rotation++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.Distance(targetHitbox.Center.ToVector2()) < 230 && DetonationTime == DetonationProgress + 1)
            {
                return true;
            }
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 300);
        }


        public override bool PreKill(int timeLeft)
        {
            SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/MetalShatter") with { Volume = 0.1f, PlayOnlyIfFocused = false, MaxInstances = 0, Pitch = -.6f }, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 900, 45);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 600, 30);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);

                NPC.NewNPC(Projectile.GetSource_FromThis(), (int)Projectile.Center.X, (int)Projectile.Center.Y, ModContent.NPCType<NPCs.Special.AbyssFracture>());
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(28, 28);
                Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Blue, dustVel, 200, Color.White, 1.3f).noGravity = true;
            }

            //Cause active Reality Fracture projectile to make a new fracture pointing at this
            /* Again, couldn't make this look good lol
            for(int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<RealityFracture>())
                {
                    RealityFracture proj = (RealityFracture)Main.projectile[i].ModProjectile;
                    proj.newCrackLocation = Projectile.Center;
                    break;
                }
            }*/

            return true;
        }

        float effectTimer;
        float starRotation;
        public static Effect RingEffect;
        public static Effect CoreEffect;
        public override bool PreDraw(ref Color lightColor)
        {
            Vector3 hslColor1 = Main.rgbToHsl(Color.Cyan);
            Vector3 hslColor2 = Main.rgbToHsl(Color.Navy);
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Cache shaders
            if (CoreEffect == null)
            {
                CoreEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 600, 600);
            float attackFadePercent = (float)Math.Pow(detonationPercent, .2) / 2f;
            starRectangle.Width = (int)(starRectangle.Width * (Math.Pow(detonationPercent, .3)));
            starRectangle.Height = (int)(starRectangle.Height * (Math.Pow(detonationPercent, .3)));

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);

            /*
            if (RingEffect == null)
            {
                RingEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/FuzzyRing", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle ringRectangle = new Rectangle(0, 0, 1200, 1200);
            Vector2 ringOrigin = ringRectangle.Size() / 2f;

            float shaderAngle = MathHelper.Pi * 1.2f * ((float)DetonationProgress) / DetonationTime;
            float shaderRotation = 0;
            RingEffect.Parameters["textureToSizeRatio"].SetValue(tsorcRevamp.NoiseWavy.Size() / ringRectangle.Size());
            RingEffect.Parameters["shaderColor"].SetValue(Color.Cyan.ToVector3());
            RingEffect.Parameters["splitAngle"].SetValue(shaderAngle);
            RingEffect.Parameters["rotation"].SetValue(shaderRotation);
            RingEffect.Parameters["length"].SetValue(.2f);
            RingEffect.Parameters["firstEdge"].SetValue(.25f);
            RingEffect.Parameters["secondEdge"].SetValue(.015f);

            //Precomputed
            RingEffect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            RingEffect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + shaderAngle - MathHelper.Pi);
            RingEffect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - shaderAngle)) - MathHelper.Pi);
            RingEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.PiOver2 - 0.35f, ringOrigin, 1, SpriteEffects.None);
            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.Pi + MathHelper.PiOver2 - 0.35f, ringOrigin, 1, SpriteEffects.None);

            */
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}
