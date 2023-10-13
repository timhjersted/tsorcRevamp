using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons
{
    public class AshenLord : RuneterraDragon
    {
        public override float Scale => 1f;
        public override int BuffType => ModContent.BuffType<CenterOfTheHeat>();
        public override int DebuffType => ModContent.BuffType<ScorchingDebuff>();
        public override int PairedProjectileType => ModContent.ProjectileType<ScorchingPointFireball>();
        public override int DragonType => 1;
        public override float maxSize => 2700;
        public override float size => 2850;
        override public void SetupBody()
        {
            NeckSegments = new BodySegment[4];

            FrontBody = new BodySegment(8, 0, 0, 7);
            FrontBody.Texture = ModContent.Request<Texture2D>(Texture + "/Front_Body");
            FrontBody.segmentOrigin = new Vector2(31, 43);

            BackBody = new BodySegment(8, 0, 0, 7);
            BackBody.Texture = ModContent.Request<Texture2D>(Texture + "/Back_Body");
            BackBody.segmentOrigin = new Vector2(150, 30);
            BackBody.offset = new Vector2(-11, 1);
            FrontBody.backSegments.Add(BackBody);

            BackRLeg = new BodySegment(8, 0, 0, 7);
            BackRLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Back_R_Leg");
            BackRLeg.segmentOrigin = new Vector2(46, 12);
            BackRLeg.offset = new Vector2(-26, 10);
            BackBody.AddSegment(BackRLeg, false);

            BackLLeg = new BodySegment(8, 0, 0, 7);
            BackLLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Back_L_Leg");
            BackLLeg.segmentOrigin = new Vector2(46, 12);
            BackLLeg.offset = new Vector2(-8, 8);
            BackBody.AddSegment(BackLLeg, true);

            FrontRLeg = new BodySegment(8, 0, 0, 7);
            FrontRLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Front_R_Leg");
            FrontRLeg.segmentOrigin = new Vector2(18, 29);
            FrontRLeg.offset = new Vector2(-3, -10);
            FrontBody.AddSegment(FrontRLeg, false);

            FrontLLeg = new BodySegment(8, 0, 0, 7);
            FrontLLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Front_L_Leg");
            FrontLLeg.segmentOrigin = new Vector2(16, 27);
            FrontLLeg.offset = new Vector2(13, -22);
            FrontBody.AddSegment(FrontLLeg, true);

            Vector2 neckOrigin = new Vector2(8, 24);

            BodySegment NeckSegment = new BodySegment(1, 0, 0, 0);
            NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
            NeckSegment.segmentOrigin = neckOrigin;
            NeckSegment.offset = new Vector2(14, -3);
            NeckSegments[0] = NeckSegment;
            FrontBody.AddSegment(NeckSegment, true);
            BodySegment LastNeckSegment = NeckSegment;

            NeckSegment = new BodySegment(1, 0, 0, 0);
            NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
            NeckSegment.segmentOrigin = neckOrigin;
            NeckSegment.offset = new Vector2(10, 0);
            NeckSegments[1] = NeckSegment;
            LastNeckSegment.AddSegment(NeckSegment, false);
            LastNeckSegment = NeckSegment;

            for (int i = 2; i < NeckSegments.Length; i++) // 2 cuz ^^ 
            {
                NeckSegment = new BodySegment(1, 0, 0, 0);
                NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
                NeckSegment.segmentOrigin = neckOrigin;
                NeckSegment.offset = new Vector2(10, 0);
                NeckSegments[i] = NeckSegment;
                LastNeckSegment.AddSegment(NeckSegment, false);
                LastNeckSegment = NeckSegment;
            }

            Head = new BodySegment(12, 0, -1, -1);
            Head.Texture = ModContent.Request<Texture2D>(Texture + "/Dragon_Head");
            Head.segmentOrigin = new Vector2(46, 52);
            Head.offset = new Vector2(6, -2);
            LastNeckSegment.AddSegment(Head, false);

            Mouth = new BodySegment(0, 0, -1, -1);
            Mouth.offset = new Vector2(5, 10); // might need "some" calibration
            Mouth.rotation = 0f; // might need "some" calibration
            Head.AddSegment(Mouth, false);
        }

        public override void AltSequenceEnd()
        {
            // Do meteor thingy...
        }

        public override bool PreDraw(ref Color lightColor)
        {
            FrontBody.Draw(lightColor);

            if (Head.frame < 8)
                return false;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/DragonBreath", AssetRequestMode.ImmediateLoad).Value;

            float angle = MathHelper.TwoPi / 10f;
            float shaderRotation = MathF.PI * 0.9f;

            effect.Parameters["Color"].SetValue(new Vector4(0.886f, 0.17f, 0.15f, 1f));
            effect.Parameters["Color2"].SetValue(new Vector4(1f, 0.75f, 0.00f, 0.1f));
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            effect.Parameters["length"].SetValue(.01f * breathSize / maxBreathSize);
            float opacity = 1.5f;

            /*
            if (fade < 30)
            {
                MathHelper.Lerp(0.01f, 1, fade / 30f);
                opacity *= fade / 30f;
                opacity *= fade / 30f;
            }*/

            string NoiseTexturePath = "tsorcRevamp/Textures/Noise/";
            Texture2D NoiseWavy = (Texture2D)ModContent.Request<Texture2D>(NoiseTexturePath + "WavyNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            Texture2D NoiseSmooth = (Texture2D)ModContent.Request<Texture2D>(NoiseTexturePath + "SmoothNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);

            effect.Parameters["opacity"].SetValue(opacity * 5);
            effect.Parameters["texScale"].SetValue(NoiseSmooth.Size() / 500);
            effect.Parameters["texScale3"].SetValue(NoiseWavy.Size() / 1000);
            effect.Parameters["noiseTexture2"].SetValue(NoiseWavy);

            //I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, NoiseSmooth.Width, NoiseSmooth.Height);
            Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);

            //Draw the rendertarget with the shader
            float trueSizeMultiplier = 1f;
            Main.spriteBatch.Draw(NoiseSmooth, Mouth.finalPosition - Main.screenPosition, recsize, Color.White, Mouth.finalRotation - MathF.PI * (Mouth.curEffect == SpriteEffects.None ? 0 : 1), origin, trueSizeMultiplier * trueSizeMultiplier * 7.5f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

            return false;
        }
    }
}