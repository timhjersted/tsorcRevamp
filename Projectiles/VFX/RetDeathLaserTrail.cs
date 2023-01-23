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
    class RetDeathLaserTrail : DynamicTrail
    {
        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 50;
            trailMaxLength = 150;
            NPCSource = false;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            timeFactor++;
            trailYOffset = 30;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture1);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = new Color(1.0f, 0.4f, 0.4f, 1.0f);
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}