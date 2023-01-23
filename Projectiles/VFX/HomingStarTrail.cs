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
    class HomingStarTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Illuminant Trail");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            trailWidth = 35;
            trailPointLimit = 900;
            trailCollision = true;
            NPCSource = false;
            collisionFrequency = 5;
            trailYOffset = 50;
            trailMaxLength = 750;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float transitionTimer;
        bool PreSetColor = false;
        bool ColorOverride = false;
        public override void AI()
        {
            base.AI();
            timeFactor++;
            int? catIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Cataluminance>());
            if(catIndex != null)
            {
                if (Main.npc[catIndex.Value].life < Main.npc[catIndex.Value].lifeMax / 2f)
                {
                    if (Projectile.timeLeft == 99999999)
                    {
                        PreSetColor = true;
                    }
                    else
                    {
                        transitionTimer++;
                    }
                }
            }
            if (Projectile.ai[0] == 1)
            {
                PreSetColor = true;
            }
            if (Projectile.ai[0] == 2)
            {
                trailMaxLength = 200;
                ColorOverride = true;
            }
            if (Projectile.ai[0] > 3)
            {
                ColorOverride = true;
                trailMaxLength = Projectile.ai[0];
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            if (progress >= 0.85)
            {
                float scale = (1f - progress) / 0.15f;
                return (float)Math.Pow(scale, 0.1) * (float)trailWidth * 0.5f;
            }
            else
            {
                return (float)Math.Pow(progress, 0.6f) * trailWidth * 0.5f;
            }
        }

        float timeFactor = 0;
        int ꙮ; //Note: ​​̵̲̹̞͘​̶̝̥̰̓͐̽​̶̛͍͌̑​̴̜͉̀​̵̨̦̜̈́̕​̴̞̰̖̆​̸̒͜​̸͚̖͌̎​̸̝̊͠​̵̩̒͗͝​̵̟̩͐
        public override void SetEffectParameters(Effect effect)
        {
            float intensity = 0.07f;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            float trueFadeOut = fadeOut;
            Color shaderColor = new Color(0.1f, 0.5f, 1f);
            if (ColorOverride)
            {
                shaderColor = new Color(0.1f, 0.5f, 1f);
            }
            else if (PreSetColor)
            {
                shaderColor = new Color(1f, 0.3f, 0.85f) * 0.5f;
                intensity = 0.1f;
            }
            else if (Projectile.ai[0] != 1 && transitionTimer <= 120)
            {
                shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), transitionTimer / 120);
                trueFadeOut += trueFadeOut * (transitionTimer / 120);
                intensity = 0.07f + 0.03f * (transitionTimer / 120);
            }
            ꙮ += 1;
            //collisionPadding = (int)trailMaxLength / 10;
            collisionEndPadding = (int)trailCurrentLength / 30;
            visualizeTrail = false;

            //Shifts its color slightly over time
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, ꙮ, intensity);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture1);
            effect.Parameters["fadeOut"].SetValue(trueFadeOut);
            effect.Parameters["time"].SetValue(timeFactor / 100f);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}