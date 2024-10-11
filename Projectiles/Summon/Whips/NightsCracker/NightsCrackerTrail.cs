using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Whips.NightsCracker
{
    public class NightsCrackerTrail : DynamicTrail
    {
        Color TrailColor;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            trailWidth = 50;
            trailPointLimit = 4000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 7;
            collisionPadding = 0;
            trailYOffset = 50;
            trailMaxLength = 4000;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Player player = Main.player[Projectile.owner];
            Projectile.timeLeft = player.itemAnimationMax;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            Projectile Whip = Main.projectile[(int)Projectile.ai[0]];
            List<Vector2> points = Whip.WhipPointsForCollision;
            Projectile.FillWhipControlPoints(Whip, points);
            //Main.NewText(player.itemAnimationMax);

            base.AI();
            Projectile.tileCollide = false;

            bool FullyCharged = (Projectile.ai[1] == 1) ? true : false;
            Color TrailColor = FullyCharged ? new Color(0.5f, 1f, 0.2f, 0.25f) : new Color(0.25f, 0.08f, 1f, 0.25f);
            Lighting.AddLight(Projectile.Center, TrailColor.ToVector3() * 1f);

            Projectile.velocity = Projectile.Center.DirectionTo(Whip.WhipPointsForCollision[points.Count - 1]) * Projectile.Center.Distance(Whip.WhipPointsForCollision[points.Count - 1]);
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
            bool FullyCharged = (Projectile.ai[1] == 1) ? true : false;
            Color TrailColor = FullyCharged ? new Color(0.5f, 1f, 0.2f, 0.25f) : new Color(0.25f, 0.08f, 1f, 0.25f);
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
            effect.Parameters["shaderColor"].SetValue(TrailColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<NightsCrackerDebuff>(), ModdedWhipProjectile.DefaultWhipDebuffDuration * 60);
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks < NightsCrackerItem.MaxStacks)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks++;
                if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks == NightsCrackerItem.MaxStacks)
                {
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 1f }, target.Center);
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            switch (target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks)
            {
                case > NightsCrackerItem.MaxStacks:
                    {
                        modifiers.SourceDamage *= MathF.Max(Projectile.ai[2] / (NightsCrackerProjectile.MaximumChargeTime / NightsCrackerProjectile.MaxChargeDmgMult), 1f);
                        target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks = 0;
                        break;
                    }
                case NightsCrackerItem.MaxStacks:
                    {
                        modifiers.SourceDamage += MathF.Max(Projectile.ai[2] / (NightsCrackerProjectile.MaximumChargeTime / (NightsCrackerProjectile.MaxChargeDmgMult * 4f)), 0.5f);
                        target.GetGlobalNPC<tsorcRevampGlobalNPC>().NightsCrackerStacks++;
                        break;
                    }
                case < NightsCrackerItem.MaxStacks:
                    {
                        modifiers.SourceDamage *= MathF.Max(Projectile.ai[2] / (NightsCrackerProjectile.MaximumChargeTime / NightsCrackerProjectile.MaxChargeDmgMult), 1f);
                        break; 
                    }
            }
        }
    }
}
