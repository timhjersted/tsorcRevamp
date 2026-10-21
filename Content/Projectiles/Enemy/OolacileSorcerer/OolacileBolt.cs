using System;
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
            // Dies on walls: OnTileCollide starts the death (burst + 15t trail fade) instead of flying through terrain.
            Projectile.tileCollide = true;
            deathSpeed = 1f / 15f;

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

        // ai[2] > 0 caps the 1.02x-per-tick acceleration at that speed (the Acolyte passes 10); 0 keeps the old uncapped bolt.
        private float TopSpeed => Projectile.ai[2];
        private bool burstPlayed;

        // A wall hit freezes the head where it struck and lets DynamicTrail fade the trail out over deathSpeed (15t).
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            StartDeath();
            return false;
        }

        public override bool CanHitPlayer(Player target) => !dying;

        private void StartDeath()
        {
            dying = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            PlayDeathBurst();
        }

        // 40 red-black dusts at the head, once per bolt whichever way it dies (wall, timeout fade or being cleared).
        private void PlayDeathBurst()
        {
            if (burstPlayed || Main.dedServ)
            {
                return;
            }

            burstPlayed = true;
            for (int i = 0; i < 40; i++)
            {
                int dustType = Terraria.ID.DustID.CrimsonSpray;
                if (i % 3 == 0)
                {
                    dustType = Terraria.ID.DustID.Shadowflame;
                }

                Dust shard = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(4f, 4f), 60, default, Main.rand.NextFloat(1f, 1.7f));
                shard.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            PlayDeathBurst();
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
            
            if (dying)
            {
                // Timing out: the head stops so the fading trail does not drift on past its last drawn point.
                Projectile.velocity = Vector2.Zero;
                PlayDeathBurst();
            }
            else if (accel)
            {
                Projectile.velocity *= 1.02f;

                float speed = Projectile.velocity.Length();
                if (TopSpeed > 0f && speed > TopSpeed)
                {
                    Projectile.velocity *= TopSpeed / speed;
                }
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
