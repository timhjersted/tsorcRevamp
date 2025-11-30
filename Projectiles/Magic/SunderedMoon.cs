using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Magic
{
    class SunderedMoon : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.scale = 1.2f;
            Projectile.timeLeft = 360;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
            trailWidth = 33;
            trailPointLimit = 900;
            trailCollision = true;
            NPCSource = false;
            collisionFrequency = 5;
            trailYOffset = 50;
            trailMaxLength = 250;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        bool playedSound = false;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.0125f }, Projectile.Center);
                playedSound = true;
            }

            base.AI();

            //Stop homing after a few seconds
            if (!hasHitNPC)
            {
                int? target = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
                if (target.HasValue && Main.npc[target.Value].Distance(Projectile.Center) < 320)
                {
                    //Perform homing
                    UsefulFunctions.SmoothHoming(Projectile, Main.npc[target.Value].Center, 0.5f, 20, Main.npc[target.Value].velocity, false);
                }
            }
        }

        bool hasHitNPC = false;
        
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hasHitNPC = true;

            if (Main.rand.Next(1, 3) == 1)
            {
                Vector2 spawnPosition = new Vector2(
                    target.position.X + Main.rand.NextFloat(-100f, 100f), 
                    target.position.Y - 520f 
                );

                Vector2 direction = new Vector2(0, 1); 
                direction.Normalize();
                direction *= 5f; 

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction,
                    ProjectileID.MagicMissile,
                    damageDone, 
                    1.0f,
                    Main.myPlayer
                );
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            if (progress >= 0.85)
            {
                float scale = (1f - progress) / 0.15f;
                return (float)Math.Pow(scale, 0.1) * (float)trailWidth * 0.5f;
            }
            else
            {
                return (float)Math.Pow(progress, 0.6f) * trailWidth * 0.5f;
            }
        }

        public override void SetEffectParameters(Effect effect)
        {
            Color shaderColor = Color.Lerp(new Color(0.05f, 0.15f, 1f), new Color(0.1f, 0.3f, 1f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.02f);

            collisionEndPadding = trailPositions.Count / 30;
            visualizeTrail = false;

            //Shifts its color slightly over time

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(0.5f);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 100f);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
