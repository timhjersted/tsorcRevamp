using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class IchorFragment : DynamicTrail
    {
        
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ichor Fragment");
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 200;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;

            trailWidth = 20;
            trailPointLimit = 50;
            trailCollision = true;
            NPCSource = false;
            trailYOffset = 50;
            trailMaxLength = 200;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/IchorTrackerShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 300);
        }

        public override void SetEffectParameters(Effect effect)
        {
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseSplotchy);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Gold.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.5f}, Projectile.Center);
        }
    }
}
