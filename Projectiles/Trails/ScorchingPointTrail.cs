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
    class ScorchingPointTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Scorching Point Trail");
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
            trailWidth = 10;
            trailPointLimit = 10;
            trailCollision = true;
            collisionFrequency = 5;
        }

        public override void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);
        }
        float DefaultWidthFunction(float progress)
        {
            float num = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, 1 - progress, clamped: true);
            num *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, num);
        }

        Color DefaultColorFunction(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs((1 - progress) * 10 - Main.GlobalTimeWrappedHourly * 20));
            Color result = Color.Lerp(Color.Orange, Color.Red, (timeFactor + 1f) / 2f);
            result.A = 0;

            return result;
        }
    }
}