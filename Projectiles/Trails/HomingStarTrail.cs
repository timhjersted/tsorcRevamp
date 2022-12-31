using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Trails
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
            collisionFrequency = 5;
            trailYOffset = 50;
            trailMaxLength = 750;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override float CollisionWidthFunction(float progress)
        {
            if (progress >= 0.85)
            {
                float scale = (1f - progress) / 0.15f;
                return (float)Math.Pow(scale, 0.1) * (float)trailWidth;
            }
            else
            {
                return (float)Math.Pow(progress, 0.6f) * trailWidth;
            }
        }

        
        public override void SetEffectParameters(Effect effect)
        {
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture1);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(1.0f, 0.4f, 0.8f, 1.0f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}