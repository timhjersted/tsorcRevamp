using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Ranged
{
    public class KrakenTsunamiShark : DynamicTrail
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ranged/KrakenTsunamiShark";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.ignoreWater = true;

            trailWidth = 0; //wanted to give this a trail but it would pop up way behind the sprite and I couldn't figure out how to make it pop up just behind the sprite
            trailPointLimit = 1000;
            trailYOffset = 0;
            trailMaxLength = 350;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public float maxDetectRadius = 700f;
        public override void AI()
        {
            base.AI();

            Player player = Main.player[Projectile.owner];
            float projSpeed = 5f;

            Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 1f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.RedTorch, 0, 0, 150, default, 0.5f);
            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC == null)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.frame = 3;
                return;
            }

            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy() && target.GetGlobalNPC<tsorcRevampGlobalNPC>().Sundered)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance * 1.25f)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                    return closestNPC;
                }
            }
            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC != null && closestNPC.Hitbox.Equals(targetHitbox) && closestNPC.Hitbox.Intersects(projHitbox))
            {
                return true;
            }
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            SoundEngine.PlaySound(SoundID.SandShark with { Volume = 0.25f });
        }
        public static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            return true;
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

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Red.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}