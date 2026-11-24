using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Sentry.Galeforce;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Systems.LethalTempo;

    public class LethalTempoBolt : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.timeLeft = 600;
            Projectile.friendly = true;
            Projectile.tileCollide = false; //custom tile collision code
            Projectile.DamageType = DamageClass.Melee;

            trailWidth = 10;
            trailPointLimit = 1000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 0;
            collisionPadding = 0;
            trailYOffset = 0;
            trailMaxLength = 200;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public int DmgCounter = 0;
        public const float MaxTicks = 1.5f * 60f;
        public override void AI()
        {
            base.AI();

            float speedMult = 1.5f;
            
            DmgCounter++;
            float maxBonusSpeedMult = 2f; //can deal up to 33% bonus dmg based on lifetime
            float bonusSpeedMult = (maxBonusSpeedMult / MaxTicks) * DmgCounter;
            speedMult *= MathF.Min(bonusSpeedMult, maxBonusSpeedMult);;

            NPC target = Main.npc[(int)Projectile.ai[0]];

            Projectile.velocity = Projectile.Center.DirectionTo(target.Center) * speedMult + target.velocity;
            
            if (Main.GameUpdateCount % 5 == 0)
            {
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
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
            effect.Parameters["shaderColor"].SetValue(new Color(2.55f, 2.15f, 0f, 1f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC wantedTarget = Main.npc[(int)Projectile.ai[0]];
            Rectangle wantedTargetHitbox = wantedTarget.Hitbox;
            if (wantedTargetHitbox == targetHitbox && targetHitbox.Intersects(projHitbox))
            {
                return true;
            }
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 1f;
            var modPlayer = Main.player[Projectile.owner].GetModPlayer<LethalTempoPlayer>();
            float bonusDmgMult = (LethalTempoPlayer.BoltMaxDistanceBonusDmgMult / MaxTicks) * DmgCounter;
            modifiers.ScalingBonusDamage += MathF.Min(bonusDmgMult, LethalTempoPlayer.BoltMaxDistanceBonusDmgMult);
        }
    }