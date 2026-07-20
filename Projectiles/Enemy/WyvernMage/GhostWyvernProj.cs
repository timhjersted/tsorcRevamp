using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.WyvernMage
{
    class GhostWyvernProj : DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("PiecingPlasma");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.scale = 0.5f;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            trailCollision = true;
            trailWidth = 20;
            trailPointLimit = 145;
            Projectile.DamageType = DamageClass.Magic;
            trailYOffset = 30;
            trailMaxLength = 145;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        bool playedSound = false;
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Violet.ToVector3());
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 9;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            collisionEndPadding = trailPositions.Count / 3;
            collisionPadding = trailPositions.Count / 8;
            visualizeTrail = false;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = new Color(1.0f, 0.3f, 1.0f, 0.7f);
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
