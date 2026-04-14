using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Items.Accessories.Damage;

namespace tsorcRevamp.Projectiles.Summon.YoungHunter
{
    class YoungHunterProjectile : DynamicTrail
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 115;
            Projectile.hostile = true;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 1;
            trailWidth = 25;
            trailPointLimit = 100;
            trailYOffset = 30;
            trailMaxLength = 140;
            trailCollision = true;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            Projectile.DamageType = DamageClass.Summon;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/FuriousEnergy", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 60;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 240);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.CritDamage *= 1f + (EyeOfTheHunt.CritDamage / 100);
        }

        bool playedSound = false;
        Vector2 realVelocity = Vector2.Zero;
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.Green.ToVector3());
            if (!playedSound)
            {
                realVelocity = Projectile.velocity;
                playedSound = true;
            }
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

            visualizeTrail = false;

            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color shaderColor = new Color(0.3f, 1.0f, 0.3f, 0.5f);
            effect.Parameters["slashCenter"].SetValue(Color.Gray.ToVector4());
            effect.Parameters["slashEdge"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
            collisionEndPadding = trailPositions.Count / 5;
            collisionPadding = trailPositions.Count / 8;
        }
    }
}
