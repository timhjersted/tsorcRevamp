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
    class IchorFragmentTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Ichor Trail");
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
            trailWidth = 20;
            trailPointLimit = 50;
            trailCollision = false;
            NPCSource = false;
            trailYOffset = 50;
            trailMaxLength = 200;            
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/IchorTrackerShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void SetEffectParameters(Effect effect)
        {
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/IchorTrackerShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Gold.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}