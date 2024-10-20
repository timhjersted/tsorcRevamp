﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.PhotonicDownpour
{
    class PhotonicDownpourLaser : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Death Laser");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 600;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 999;
            Projectile.DamageType = DamageClass.Summon;

            trailCollision = true;
            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 30;
            trailMaxLength = 150;
            NPCSource = false;
            collisionPadding = 0;
            collisionFrequency = 2;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.CursedInferno, 100);
        }

        bool playedSound = false;
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Color.GreenYellow.ToVector3());
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.5f }, Projectile.Center);
                playedSound = true;
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 9;
        }

        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            timeFactor++;
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color shaderColor = new Color(0.1f, 0.5f, 0.2f, 1.0f);
            shaderColor = UsefulFunctions.ShiftColor(shaderColor, timeFactor, 0.03f);
            effect.Parameters["shaderColor"].SetValue(shaderColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
