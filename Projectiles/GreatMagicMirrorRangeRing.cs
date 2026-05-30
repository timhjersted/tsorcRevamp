using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    public class GreatMagicMirrorRangeRing : DynamicTrail
    {
        private const int RingPoints = 360;

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = false;

            trailWidth = 26;
            trailPointLimit = RingPoints + 1;
            trailMaxLength = MathHelper.TwoPi * GreatMagicMirror.MaxTeleportDistance;
            trailCollision = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        public override void AI()
        {
            int playerIndex = (int)Projectile.ai[0];
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.player[playerIndex];
            if (!player.active || player.dead || !GreatMagicMirror.ShouldDrawRangeIndicator(player))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.GetModPlayer<tsorcRevampPlayer>().greatMirrorWarpPoint;

            trailPositions ??= new List<Vector2>(RingPoints + 1);
            trailRotations ??= new List<float>(RingPoints + 1);
            trailPositions.Clear();
            trailRotations.Clear();

            float rotation = Main.GlobalTimeWrappedHourly * 0.12f;
            for (int i = 0; i <= RingPoints; i++)
            {
                float angle = MathHelper.TwoPi * i / RingPoints + rotation;
                trailPositions.Add(Projectile.Center + angle.ToRotationVector2() * GreatMagicMirror.MaxTeleportDistance);
                trailRotations.Add(angle + MathHelper.PiOver2);
            }

            trailCurrentLength = CalculateLength();
        }

        public override float WidthFunction(float progress)
        {
            float pulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            return trailWidth * pulse;
        }

        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            collisionEndPadding = 0;
            collisionPadding = 0;

            Color baseColor = Color.Lerp(new Color(0.15f, 0.75f, 1f), new Color(1f, 0.35f, 0.85f), (float)Math.Pow(Math.Sin(Main.GlobalTimeWrappedHourly * 1.4f), 2));
            Vector3 hslColor = Main.rgbToHsl(baseColor);
            hslColor.X += 0.03f * (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4f);
            Color rgbColor = Main.hslToRgb(hslColor);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["fadeOut"].SetValue(0.55f);
            effect.Parameters["finalStand"].SetValue(0);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(new Color(0.2f, 0.7f, 1f).ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
