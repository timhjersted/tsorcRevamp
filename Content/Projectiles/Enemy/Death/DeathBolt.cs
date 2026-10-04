using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Shader-trail bolt used by Death's phase-one fan attack. It accelerates from rest to
    /// 20px/tick over half a second, then fades out during the final half second.
    /// </summary>
    class DeathBolt : DynamicTrail
    {
        const float DefaultTravelSpeed = 20f;
        const int AccelTicks = 30;
        const int LifetimeTicks = 5 * 60 + 30;
        const int FadeOutTicks = 30;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.netImportant = true;

            trailWidth = 30;
            trailPointLimit = 220;
            trailYOffset = 32;
            trailMaxLength = 220;
            trailCollision = true;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/OolacileBolt", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override bool? CanDamage()
        {
            return Projectile.timeLeft > FadeOutTicks;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] <= 0f)
            {
                Projectile.localAI[1] = Projectile.velocity.Length();
            }

            Vector2 direction = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (direction.LengthSquared() < 0.01f)
            {
                direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
            else
            {
                direction.Normalize();
            }

            float speedRamp = MathHelper.SmoothStep(
                0f,
                1f,
                MathHelper.Clamp(Projectile.localAI[0] / AccelTicks, 0f, 1f));
            float travelSpeed = Projectile.localAI[1] > 0f ? Projectile.localAI[1] : DefaultTravelSpeed;
            Projectile.velocity = direction * travelSpeed * speedRamp;
            Projectile.rotation = direction.ToRotation();
            fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTicks, 0f, 1f);
            Projectile.alpha = (int)(255f * (1f - fadeOut));

            Color boltColor = UsefulFunctions.ColorFromFloat(Projectile.ai[2]);
            Lighting.AddLight(Projectile.Center, boltColor.ToVector3());

            base.AI();
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 4f;
        }


        float baseNoiseUOffset;
        public override void SetEffectParameters(Effect effect)
        {
            if (baseNoiseUOffset == 0)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);

            const float pixelBlockSize = 2f;
            Vector2 pixelGridSize = new Vector2(
                Math.Max(trailCurrentLength / pixelBlockSize, 1f),
                Math.Max(trailWidth * 2f / pixelBlockSize, 1f));
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y));

            visualizeTrail = false;
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["slashCenter"].SetValue(Color.Black.ToVector4());
            effect.Parameters["slashEdge"].SetValue(UsefulFunctions.ColorFromFloat(Projectile.ai[2]).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            collisionEndPadding = trailPositions.Count / 5;
            collisionPadding = trailPositions.Count / 8;
        }
    }
}
