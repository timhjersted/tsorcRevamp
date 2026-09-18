using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer
{
    class OolacileBolt : DynamicTrail
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;

            trailWidth = 26;
            trailPointLimit = 160;
            trailYOffset = 32;
            trailMaxLength = 170;
            trailCollision = true;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/OolacileBolt", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public bool accel = true;
        float teleAlpha = 0.5f;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 120);
        }

        bool playedSound = false;
        Vector2 realVelocity = Vector2.Zero;
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
            if (!playedSound)
            {
                realVelocity = Projectile.velocity;
                playedSound = true;
            }

            if (Projectile.timeLeft < 560)
            {
                if (realVelocity.Length() < 10)
                {
                    realVelocity += realVelocity * 0.1f;
                }
            }
            
            if (accel)
            {
                Projectile.velocity *= 1.02f;
            }
            teleAlpha -= 0.0025f;
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 9;
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
            //effect.Parameters["secondaryNoise"].SetValue(noiseTexture);

            const float pixelBlockSize = 2f;
            Vector2 pixelGridSize = new Vector2(
                Math.Max(trailCurrentLength / pixelBlockSize, 1f),
                Math.Max((trailWidth * 2f) / pixelBlockSize, 1f));
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y));

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color shaderColor = new Color(1.0f, 0.1f, 0.2f, 1.0f);
            effect.Parameters["slashCenter"].SetValue(Color.Black.ToVector4());
            effect.Parameters["slashEdge"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            collisionEndPadding = trailPositions.Count / 5;
            collisionPadding = trailPositions.Count / 8;
        }
    }
}
