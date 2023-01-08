using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Trails
{
    class CursedFlamelashTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Cursed Flamelash Trail");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            trailWidth = 70;
            trailPointLimit = 1000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 7;
            collisionPadding = 0;
            trailYOffset = 50;
            trailMaxLength = 350;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override float CollisionWidthFunction(float progress)
        {
            if(progress > 0.9)
            {
                return ((1 - progress) / 0.1f) * trailWidth;
            }
            
            return trailWidth * progress;
        }

        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
        public override void SetEffectParameters(Effect effect)
        {
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            effect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            float hostVel = 0;
            if (hostProjectile != null)
            {
                hostVel = hostProjectile.velocity.Length();
            }
            float modifiedTime = 0.001f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }
            samplePointOffset1.X += (modifiedTime);
            samplePointOffset1.Y -= (0.001f);
            samplePointOffset2.X += (modifiedTime * 3.01f);
            samplePointOffset2.Y += (0.001f);

            samplePointOffset1.X += modifiedTime;
            samplePointOffset1.X %= 1;
            samplePointOffset1.Y %= 1;
            samplePointOffset2.X %= 1;
            samplePointOffset2.Y %= 1;
            collisionEndPadding = trailPositions.Count / 2;

            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.8f, 1f, 0.3f, 1.0f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (hostProjectile != null)
            {
                int originalDamage = damage;
                damage *= (int)hostProjectile.velocity.Length() / 6;
                damage += originalDamage / 3;
            }
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.CursedInferno, 300);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.CursedInferno, 300);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}