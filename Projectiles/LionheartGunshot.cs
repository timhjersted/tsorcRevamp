using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Items.Weapons;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles
{
    class LionheartGunshot : VFX.DynamicTrail
    {
        public float Timer;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 3;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.knockBack = 0f;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;

            trailWidth = 35;
            trailPointLimit = 900;
            trailCollision = true;
            collisionFrequency = 2;
            trailYOffset = 50;
            trailMaxLength = 500;
            collisionEndPadding = 1;
            collisionPadding = 1;

            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RadiantStrand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DeathLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            //customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void AI()
        {
            base.AI();
            Lighting.AddLight(Projectile.Center, Main.hslToRgb((float)(Main.timeForVisualEffects / 100f) % 1, 1, 0.5f).ToVector3());
            Timer++;
        }

        float color;
        public override void SetEffectParameters(Effect effect)
        {
            Color shaderColor = Main.hslToRgb((float)(Main.timeForVisualEffects / 100f) % 1, 1, 0.5f);
            Color rgbColor = shaderColor;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            effect.Parameters["fadeOut"].SetValue(0.5f);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 100f);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);


            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarked)
            {
                modifiers.SetCrit();
                modifiers.CritDamage += (float)Projectile.CritChance / 100f;
                modifiers.FinalDamage *= LionheartGunblade.MarkExtraDmgMultBase + Timer / LionheartGunblade.MarkDistanceBasedDmgDivisor;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarked)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarked = false;
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks = 0;
            }
        }
    }
}
