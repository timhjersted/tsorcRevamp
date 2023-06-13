using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    class CursedFlamelash : DynamicTrail
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
            Projectile.netImportant = true;

            trailWidth = 70;
            trailPointLimit = 1000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 7;
            collisionPadding = 0;
            trailYOffset = 50;
            trailMaxLength = 350;
            NPCSource = false;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            base.AI();

            if (Main.GameUpdateCount % 5 == 0)
            {
                Projectile.netUpdate = true;
            }

            if (UsefulFunctions.IsTileReallySolid(Projectile.Center / 16f))
            {
                dying = true;
            }
            if (!dying)
            {
                Lighting.AddLight(Projectile.Center, TorchID.Cursed);
                Main.player[Projectile.owner].manaRegenDelay = 10;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.MouseWorld, 1f, 20, null, true, 0.2f);
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


        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {/*
            int originalDamage = damage;
            damage *= (int)Projectile.velocity.Length() / 6;
            damage += originalDamage / 3;*/
            modifiers.SourceDamage *= Projectile.velocity.Length() / 6;
            modifiers.FinalDamage += modifiers.GetDamage(Projectile.damage, false) / 3;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(5))
            {
                target.AddBuff(BuffID.CursedInferno, 5 * 60);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.rand.NextBool(5) && info.PvP)
            {
                target.AddBuff(BuffID.CursedInferno, 5 * 60);
            }
        }
        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
        public override void SetEffectParameters(Effect effect)
        {
            float hostVel = Projectile.velocity.Length();

            float modifiedTime = 0.001f * hostVel;

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

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTextureWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.8f, 1f, 0.3f, 1.0f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
