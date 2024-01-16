using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    class SamuraiBeetleTrail : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = 600;
            Projectile.friendly = true;
            Projectile.penetrate = -1;

            trailWidth = 60;
            trailPointLimit = 2000;
            trailCollision = false;
            trailYOffset = 0;
            trailMaxLength = 500;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void AI()
        {
            Projectile SamuraiBeetle = Main.projectile[(int)Projectile.ai[0]];
            base.AI();
            Projectile.tileCollide = SamuraiBeetle.tileCollide;
            Projectile.velocity = SamuraiBeetle.velocity;
            Projectile.Center = SamuraiBeetle.Center;

            if (Main.GameUpdateCount % 2 == 0)
            {
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
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
            effect.Parameters["shaderColor"].SetValue(Color.Purple.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
