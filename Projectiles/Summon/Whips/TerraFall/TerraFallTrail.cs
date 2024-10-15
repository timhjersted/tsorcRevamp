using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.Summon.Whips.NightsCracker;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Whips.TerraFall
{
    public class TerraFallTrail : DynamicTrail
    {
        public bool FullyCharged;
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
        public int Timer = 0;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            Timer++;

            Main.NewText(Timer);

            float swingTime = Projectile.ai[1] * Projectile.MaxUpdates;
            if (Timer >= swingTime)
            {
                Projectile.Kill();
                return;
            }

            Projectile Whip = Main.projectile[(int)Projectile.ai[0]];
            List<Vector2> points = Whip.WhipPointsForCollision;
            Projectile.FillWhipControlPoints(Whip, points);

            base.AI();
            Projectile.tileCollide = false;

            FullyCharged = (Projectile.ai[2] >= TerraFallProjectile.MaximumChargeTime) ? true : false;
            Color TrailColor = FullyCharged ? new Color(0.15f, 1f, 0.8f, 0.25f) : new Color(0.1f, 1f, 0.1f, 0.25f);
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
            FullyCharged = (Projectile.ai[2] >= TerraFallProjectile.MaximumChargeTime) ? true : false;
            Color TrailColor = FullyCharged ? new Color(0.15f, 1f, 0.8f, 0.25f) : new Color(0.1f, 1f, 0.1f, 0.25f);
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
            target.AddBuff(ModContent.BuffType<TerraFallDebuff>(), ModdedWhipProjectile.DefaultWhipDebuffDuration * 60);
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks < TerraFallItem.MaxStacks)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks++;
                if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks == TerraFallItem.MaxStacks)
                {
                    SoundEngine.PlaySound(SoundID.Item60 with { Volume = 1f }, target.Center);
                }
            }
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByTerraFall)
            {
                Main.player[Projectile.owner].AddBuff(ModContent.BuffType<TerraFallBuff>(), ModdedWhipProjectile.DefaultWhipBuffDuration * 60);
                if(FullyCharged)
                {
                    target.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks = 0;
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (FullyCharged && target.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByTerraFall)
            {
                modifiers.SourceDamage += MathF.Max(Projectile.ai[2] / (TerraFallProjectile.MaximumChargeTime / (TerraFallProjectile.MaxChargeDmgMult * 5f)), 0.5f);
            }
            else
            {
                modifiers.SourceDamage *= MathF.Max(Projectile.ai[2] / (TerraFallProjectile.MaximumChargeTime / TerraFallProjectile.MaxChargeDmgMult), 1f);
            }
        }
    }
}
