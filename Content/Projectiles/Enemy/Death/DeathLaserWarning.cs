using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Reusable hostile warning beam. It tracks its host's target for TrackTicks, locks the
    /// final angle for the remaining lifetime, and draws with a constant bright targeting beam.
    /// </summary>
    class DeathLaserWarning : ModProjectile
    {
        public int TrackTicks = 120;
        public int TotalTicks = 150;
        public float BeamLength = 900f;
        public float BeamSize = 0.14f;
        public float BeamBrightness = 0.85f;
        public float AimOffsetRadians;

        bool FollowHost => Projectile.ai[2] != 0f;

        float timeFactor;
        Effect laserShader;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 99999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int age = (int)Projectile.localAI[0];
            if (age > TotalTicks)
            {
                Projectile.Kill();
                return;
            }

            if (FollowHost)
            {
                int ownerIndex = (int)Projectile.ai[1];
                if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs)
                {
                    Projectile.Kill();
                    return;
                }

                NPC owner = Main.npc[ownerIndex];
                if (!owner.active || !owner.HasValidTarget)
                {
                    Projectile.Kill();
                    return;
                }

                if (age <= TrackTicks)
                {
                    Vector2 aim = (Main.player[owner.target].Center - owner.Center).SafeNormalize(Vector2.UnitX);
                    Projectile.velocity = aim.RotatedBy(AimOffsetRadians);
                }

                Projectile.Center = owner.Center + Projectile.velocity * 20f;
            }
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (laserShader == null)
            {
                laserShader = ModContent.Request<Effect>("tsorcRevamp/Effects/GenericLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            timeFactor++;
            Color beamColor = UsefulFunctions.ColorFromFloat(Projectile.ai[0]);
            Vector3 hslColor = Main.rgbToHsl(beamColor);
            hslColor.X += 0.03f * (float)Math.Cos(timeFactor / 50f);
            Color rgbColor = Main.hslToRgb(hslColor);

            float modifiedSize = BeamSize * 100f;
            laserShader.Parameters["Time"].SetValue(timeFactor);
            laserShader.Parameters["Color"].SetValue(rgbColor.ToVector3());
            laserShader.Parameters["SecondaryColor"].SetValue(Color.White.ToVector3());
            laserShader.Parameters["FadeOut"].SetValue(BeamBrightness);
            laserShader.Parameters["ProjectileSize"].SetValue(new Vector2(BeamLength, modifiedSize));
            laserShader.Parameters["TextureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            laserShader.CurrentTechnique.Passes[0].Apply();

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)BeamLength, Math.Max(1, (int)modifiedSize));
            Vector2 origin = new Vector2(0f, sourceRectangle.Height / 2f);
            Main.EntitySpriteDraw(
                tsorcRevamp.NoiseTurbulent,
                Projectile.Center - Main.screenPosition,
                sourceRectangle,
                Color.White,
                Projectile.velocity.ToRotation(),
                origin,
                1f,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(TrackTicks);
            writer.Write(TotalTicks);
            writer.Write(BeamLength);
            writer.Write(BeamSize);
            writer.Write(BeamBrightness);
            writer.Write(AimOffsetRadians);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            TrackTicks = reader.ReadInt32();
            TotalTicks = reader.ReadInt32();
            BeamLength = reader.ReadSingle();
            BeamSize = reader.ReadSingle();
            BeamBrightness = reader.ReadSingle();
            AimOffsetRadians = reader.ReadSingle();
        }

        public static int Spawn(
            IEntitySource source,
            NPC owner,
            Color color,
            int trackTicks = 120,
            int totalTicks = 150,
            float beamLength = 900f,
            float beamSize = 0.14f,
            float beamBrightness = 0.85f,
            float aimOffsetRadians = 0f,
            Vector2? origin = null,
            Vector2? direction = null,
            bool followHost = true)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int index = Projectile.NewProjectile(
                source,
                origin ?? owner.Center,
                direction ?? Vector2.UnitX,
                ModContent.ProjectileType<DeathLaserWarning>(),
                0,
                0f,
                Main.myPlayer,
                UsefulFunctions.ColorToFloat(color),
                owner.whoAmI,
                followHost ? 1f : 0f);

            if (index >= 0 && index < Main.maxProjectiles)
            {
                DeathLaserWarning warning = Main.projectile[index].ModProjectile as DeathLaserWarning;
                if (warning != null)
                {
                    warning.TrackTicks = trackTicks;
                    warning.TotalTicks = totalTicks;
                    warning.BeamLength = beamLength;
                    warning.BeamSize = beamSize;
                    warning.BeamBrightness = beamBrightness;
                    warning.AimOffsetRadians = aimOffsetRadians;
                    Main.projectile[index].netUpdate = true;
                }
            }

            return index;
        }
    }
}
