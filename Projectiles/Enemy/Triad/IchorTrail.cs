using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class IchorTrail : Projectiles.VFX.DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Ichor Trail");
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
            trailWidth = 110;
            trailPointLimit = 50;
            trailCollision = true;
            NPCSource = true;
            trailYOffset = 50;
            trailMaxLength = 200;
            deathSpeed = 1f / 20f;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/IchorTrackerShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 50;
            if (hostEntityType == ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>())
            {
                trailWidth = 20;
                trailMaxLength = 300;
            }
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseSplotchy);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Gold.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}