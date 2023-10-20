using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Birbs
{
    class RageDemonBolt : DynamicTrail
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;

            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            trailCollision = true;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/FuriousEnergy", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DarkInferno>(), 100);
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
                    realVelocity += Vector2.Normalize(Projectile.velocity) * 0.5f;
                }
            }
            Projectile.velocity = realVelocity.RotatedBy(Math.Sin(Main.GameUpdateCount * 0.15f));
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 9;
        }

        float timeFactor = 0;
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

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color shaderColor = new Color(1.0f, 0.01f, 0.8f, 1.0f);
            effect.Parameters["slashCenter"].SetValue(Color.White.ToVector4());
            effect.Parameters["slashEdge"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            collisionEndPadding = trailPositions.Count / 5;
            collisionPadding = trailPositions.Count / 8;
        }
    }
}
