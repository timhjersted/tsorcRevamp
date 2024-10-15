﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons
{
    public class StarForger : RuneterraDragon
    {
        public override float Scale => 0.6f;
        public override int BuffType => ModContent.BuffType<CenterOfTheUniverseBuff>();
        public override int DebuffType => ModContent.BuffType<SunburnDebuff>();
        public override int PairedProjectileType => ModContent.ProjectileType<CenterOfTheUniverseStar>();
        public override int DragonTier => 3;
        public override float maxSize => 2700;
        public override float size => 3800;
        override public void SetupBody()
        {
            NeckSegments = new BodySegment[4];

            FrontBody = new BodySegment(8, 0, 0, 7);
            FrontBody.Texture = ModContent.Request<Texture2D>(Texture + "/Front_Body");
            FrontBody.segmentOrigin = new Vector2(66, 46);

            BackBody = new BodySegment(8, 0, 0, 7);
            BackBody.Texture = ModContent.Request<Texture2D>(Texture + "/Back_Body");
            BackBody.segmentOrigin = new Vector2(233, 41);
            BackBody.offset = new Vector2(-31, 7);
            FrontBody.backSegments.Add(BackBody);

            BackRLeg = new BodySegment(8, 0, 0, 7);
            BackRLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Back_R_Leg");
            BackRLeg.segmentOrigin = new Vector2(76, 14);
            BackRLeg.offset = new Vector2(-37, 9);
            BackBody.AddSegment(BackRLeg, false);

            BackLLeg = new BodySegment(8, 0, 0, 7);
            BackLLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Back_L_Leg");
            BackLLeg.segmentOrigin = new Vector2(64, 16);
            BackLLeg.offset = new Vector2(1, 14);
            BackBody.AddSegment(BackLLeg, true);

            FrontRLeg = new BodySegment(16, 4, 0, 7, 8, 15);
            FrontRLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Front_R_Leg");
            FrontRLeg.segmentOrigin = new Vector2(80, 102);
            FrontRLeg.offset = new Vector2(18, 12);
            FrontBody.AddSegment(FrontRLeg, false);

            FrontLLeg = new BodySegment(16, 0, 0, 7, 8, 15);
            FrontLLeg.Texture = ModContent.Request<Texture2D>(Texture + "/Front_L_Leg");
            FrontLLeg.segmentOrigin = new Vector2(78, 94);
            FrontLLeg.offset = new Vector2(54, 4);
            FrontBody.AddSegment(FrontLLeg, true);

            Vector2 neckOrigin = new Vector2(16, 26);

            BodySegment NeckSegment = new BodySegment(1, 0, 0, 0);
            NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
            NeckSegment.segmentOrigin = neckOrigin;
            NeckSegment.offset = new Vector2(52, -2);
            NeckSegments[0] = NeckSegment;
            FrontBody.AddSegment(NeckSegment, true);
            BodySegment LastNeckSegment = NeckSegment;

            NeckSegment = new BodySegment(1, 0, 0, 0);
            NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
            NeckSegment.segmentOrigin = neckOrigin;
            NeckSegment.offset = new Vector2(20, 0);
            NeckSegments[1] = NeckSegment;
            LastNeckSegment.AddSegment(NeckSegment, false);
            LastNeckSegment = NeckSegment;

            for (int i = 2; i < NeckSegments.Length; i++) // 2 cuz ^^ 
            {
                NeckSegment = new BodySegment(1, 0, 0, 0);
                NeckSegment.Texture = ModContent.Request<Texture2D>(Texture + "/Neck_Segment");
                NeckSegment.segmentOrigin = neckOrigin;
                NeckSegment.offset = new Vector2(20, 0);
                NeckSegments[i] = NeckSegment;
                LastNeckSegment.AddSegment(NeckSegment, false);
                LastNeckSegment = NeckSegment;
            }

            Head = new BodySegment(8, 0, -1, -1);
            Head.Texture = ModContent.Request<Texture2D>(Texture + "/Dragon_Head");
            Head.segmentOrigin = new Vector2(70, 76);
            Head.offset = new Vector2(17, 4);
            LastNeckSegment.AddSegment(Head, false);

            Mouth = new BodySegment(0, 0, -1, -1);
            Mouth.offset = new Vector2(0, 26); // might need "some" calibration
            Mouth.rotation = 0; // might need "some" calibration
            Head.AddSegment(Mouth, false);
        }

        public override void AltSequenceEnd()
        {
            // Do meteor thingy...
        }

        public override bool PreDraw(ref Color lightColor)
        {
            FrontBody.Draw(lightColor);

            if (Head.frame < 4)
                return false;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/DragonBreath", AssetRequestMode.ImmediateLoad).Value;

            float angle = MathHelper.TwoPi / 10f;
            float shaderRotation = MathF.PI * 0.9f;

            effect.Parameters["Color"].SetValue(new Vector4(0.2f, 0.05f, 0.35f, 1f));
            effect.Parameters["Color2"].SetValue(new Vector4(1f, 0.75f, 0.00f, 1f));
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            effect.Parameters["length"].SetValue(.01f * breathSize / maxBreathSize);
            float opacity = 0.5f;

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
            float trueSizeMultiplier = 1.2f;
            Main.spriteBatch.Draw(NoiseSmooth, Mouth.finalPosition - Main.screenPosition, recsize, Color.White, Mouth.finalRotation - MathF.PI * (Mouth.curEffect == SpriteEffects.None ? 0 : 1), origin, trueSizeMultiplier * trueSizeMultiplier * 7.5f, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}