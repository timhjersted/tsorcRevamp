using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash
{
    public class PolarisLeashFallingStar : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FallingStar);
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180 * 4;
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 4 - 1;

            trailWidth = 50;
            trailPointLimit = 1000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 7;
            collisionPadding = 0;
            trailYOffset = 50;
            trailMaxLength = 350;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void AI()
        {
            base.AI();
            Projectile.tileCollide = false;
            Player player = Main.player[Projectile.owner];

            Lighting.AddLight(Projectile.Center, Color.DeepSkyBlue.ToVector3() * 1f);

            if ((Main.npc[(int)Projectile.ai[0]].Center - Projectile.Center).SafeNormalize(Vector2.Zero).Y > 0)
            {
                Projectile.velocity = (Main.npc[(int)Projectile.ai[0]].Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 5;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }
        public override float CollisionWidthFunction(float progress)
        {
            if (progress > 0.9)
            {
                return ((1 - progress) / 0.1f) * trailWidth;
            }

            return trailWidth * progress;
        }
        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
        public override void SetEffectParameters(Effect effect)
        {
            float hostVel = Projectile.velocity.Length();

            float modifiedTime = 0.0007f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }

            if (fadeOut == 1)
            {
                samplePointOffset1.X += (modifiedTime);
                samplePointOffset1.Y -= (0.001f);
                samplePointOffset2.X += (modifiedTime * 3.01f);
                samplePointOffset2.Y += (0.001f);

                samplePointOffset1.X += modifiedTime;
                samplePointOffset1.X %= 1;
                samplePointOffset1.Y %= 1;
                samplePointOffset2.X %= 1;
                samplePointOffset2.Y %= 1;
            }
            collisionEndPadding = trailPositions.Count / 2;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.4f, 0.8f, 2.5f, 1f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
