using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class AuraOfIlluminance : Projectiles.VFX.DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Illuminant Trail");
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
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = NPC.immuneTime;

            trailWidth = 45;
            trailPointLimit = 2000;
            trailMaxLength = 2000; 
            trailCollision = true;
            collisionFrequency = 3;
            noFadeOut = false;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            base.AI();
        }

        public override bool HostEntityValid()
        {
            if (Main.player[(int)Projectile.ai[0]].active && Main.player[(int)Projectile.ai[0]].GetModPlayer<tsorcRevampPlayer>().AuraOfIlluminance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Entity HostEntity => Main.player[(int)Projectile.ai[0]];

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            collisionEndPadding = trailPositions.Count / 24;
            trailWidth = 40;

            collisionEndPadding = 0;
            collisionPadding = 0;

            Color shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.03f);


            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["finalStand"].SetValue(0);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(new Color(0.2f, 0.7f, 1f).ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}