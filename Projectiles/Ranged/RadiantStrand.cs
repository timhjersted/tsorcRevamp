using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    class RadiantStrand : VFX.DynamicTrail
    {

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 120;
            Projectile.knockBack = 0f;
            Projectile.tileCollide = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 10;

            trailWidth = 35;
            trailPointLimit = 900;
            trailCollision = true;
            collisionFrequency = 2;
            trailYOffset = 50;
            trailMaxLength = 500;
            collisionEndPadding = 1;
            collisionPadding = 1;

            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RadiantStrand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        bool reachedMouse = false;
        public override void AI()
        {
            base.AI();

            Lighting.AddLight(Projectile.Center, Main.hslToRgb((float)(Main.timeForVisualEffects / 100f) % 1, 1, 0.5f).ToVector3());

            if (!reachedMouse && Main.myPlayer == Projectile.owner)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.MouseWorld, 1f, 50, bufferZone: false);
                if (Projectile.Distance(Main.MouseWorld) < 100)
                {
                    reachedMouse = true;
                }
            }
        }

        public override void SetEffectParameters(Effect effect)
        {
            Color shaderColor = Main.hslToRgb((float)(Main.timeForVisualEffects / 100f) % 1, 1, 0.5f);
            Color rgbColor = shaderColor;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(0.5f);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 100f);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);


            return false;
        }
    }
}
