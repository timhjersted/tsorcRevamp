using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    class CaiusPyreFireball : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.SentryShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.sentry = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 120;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.MagicSummonHybrid;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

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
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Projectile.originalDamage = (int)Projectile.ai[0];
        }
        public override void AI()
        {
            base.AI();

            if (Projectile.owner == Main.myPlayer)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.MouseWorld, 1f, 20, null, true, 0.1f);
            }
            if (Projectile.wet)
            {
                Projectile.Kill();
            }
            if (Main.GameUpdateCount % 5 == 0)
            {
                Projectile.netUpdate = true;
            }

            if (UsefulFunctions.IsTileReallySolid(Projectile.Center / 16f))
            {
                dying = true;
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            if (progress > 0.9)
            {
                return ((1 - progress) / 0.1f) * trailWidth;
            }

            return trailWidth * progress;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(4))
            {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.45f }, Projectile.position);
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
            effect.Parameters["shaderColor"].SetValue(new Color(0.886f, 0.17f, 0.15f, 1f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
