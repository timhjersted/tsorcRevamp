using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class ScorchingPointTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Interstellar Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999999999;
            Projectile.penetrate = -1;
            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 111;
            Projectile.hide = true;
            collisionPadding = 50;
            NPCSource = false;

            trailCollision = true;
            collisionFrequency = 5;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float timer = 0;
        float transitionTimer = 0;
        public override bool? CanCutTiles()
        {
            return false;
        }

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
        bool pinkTrail = false;
        Color trailColor = new Color(2.57f, 0.55f, 0.24f);
        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 45;
            trailMaxLength = 400;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            float hostVel = 0;
            if (hostProjectile != null)
            {
                hostVel = hostProjectile.velocity.Length();
            }
            float modifiedTime = 0.001f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }
            samplePointOffset1.X += (modifiedTime);
            samplePointOffset1.Y -= (0.001f);
            samplePointOffset2.X += (modifiedTime * 3.01f);
            samplePointOffset2.Y += (0.001f);

            samplePointOffset1.X += modifiedTime;
            samplePointOffset1.X %= 1;
            samplePointOffset1.Y %= 1;
            samplePointOffset2.X %= 1;
            samplePointOffset2.Y %= 1;
            collisionEndPadding = trailPositions.Count / 2;

            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Orange.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            return;

            /*
             * 
float3 Color;
float3 SecondaryColor;
float FadeOut;
float Time;
float2 ProjectileSize;
float TextureSize;
             * */
            effect.Parameters["TextureSize"].SetValue(tsorcRevamp.tNoiseTexture3.Width);
            effect.Parameters["FadeOut"].SetValue(fadeOut);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["Color"].SetValue(Color.Blue.ToVector4());
            effect.Parameters["SecondaryColor"].SetValue(Color.White.ToVector4());
            effect.Parameters["ProjectileSize"].SetValue(200);
        }
    }
}