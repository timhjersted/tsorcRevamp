using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.TheDestroyer;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal sealed class DestroyerMawBeam : ModProjectile
    {
        internal const int LiveTicks = 190;
        internal const int TellTicks = 25;
        internal const float SweepArc = MathHelper.Pi;
        internal const float Length = 3000f;
        internal const float HitWidth = 90f;
        internal const int RunawayMode = 4;
        internal const int ThrashMode = 5;
        private const float RunawayLength = 930f;
        private const float RunawayHitWidth = 56f;
        private const int ThrashLength = 990;
        private const float ThrashHitWidth = 64f;
        private const int HoldTicks = 12;
        private const int SweepTicks = LiveTicks - HoldTicks - 1;
        private const int SweepRampTicks = 120;
        private const float SweepSpeedGain = 0.30f;
        private const int FadeTicks = 12;
        private const float DrawWidth = 160f;
        private const float PixelBlockSize = 4f;
        private static Asset<Effect> beamEffect;
        private static Asset<Texture2D> turbulentNoise;
        private int age = -TellTicks;
        private bool fired;
        private bool ending;
        private bool mouthArmed;

        private bool IsRunaway => Projectile.ai[2] == RunawayMode;
        private bool IsThrash => Projectile.ai[2] == ThrashMode;
        private bool IsMouthMounted => IsRunaway || IsThrash;
        private int LiveDuration => Projectile.ai[2] switch
        {
            RunawayMode => TheDestroyer.RunawayFireTicks,
            ThrashMode => TheDestroyer.ThrashFireTicks,
            _ => LiveTicks
        };
        private float BeamLength => Projectile.ai[2] switch
        {
            RunawayMode => RunawayLength,
            ThrashMode => ThrashLength,
            _ => Length
        };

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyLingeringLaser));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Length;
        }

        public override void Unload()
        {
            beamEffect = null;
            turbulentNoise = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.netImportant = true;
            int longestBeam = System.Math.Max(TheDestroyer.RunawayFireTicks, TheDestroyer.ThrashFireTicks);
            Projectile.timeLeft = TellTicks + longestBeam + FadeTicks + 2;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => fired && !ending && age <= LiveDuration && (!IsMouthMounted || mouthArmed);

        internal static float SweepAngle(float openingAngle, int turnDirection, int elapsed)
        {
            int sweepTime = System.Math.Clamp(elapsed - HoldTicks, 0, SweepTicks);
            int rampTime = System.Math.Min(sweepTime, SweepRampTicks);
            int fullSpeedTime = sweepTime - rampTime;

            // Integrate the linear speed ramp, then normalize the travel to finish the half-turn on the last live tick.
            float rampTravel = rampTime * rampTime / (2f * SweepRampTicks);
            float travel = sweepTime + SweepSpeedGain * (rampTravel + fullSpeedTime);
            const float totalTravel = SweepTicks + SweepSpeedGain * (SweepTicks - SweepRampTicks * 0.5f);
            float progress = travel / totalTravel;
            return openingAngle + SweepArc * turnDirection * progress;
        }

        public override void AI()
        {
            int headIndex = (int)Projectile.ai[0];
            NPC head = Main.npc[headIndex];
            bool hasHead = head.active && head.type == NPCID.TheDestroyer;
            TheDestroyer headState = null;
            if (hasHead)
            {
                headState = head.GetGlobalNPC<TheDestroyer>();
            }

            bool matchingMove = headState != null && (IsRunaway && headState.IsRunawayMaw || IsThrash && headState.IsHullThrash);
            bool lostMount = headState == null || headState.IsRetreating || IsMouthMounted && !matchingMove;
            mouthArmed = headState != null
                && (IsRunaway && headState.IsRunawayMawFiring || IsThrash && headState.IsHullThrashFiring);
            if (!ending && lostMount && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ending = true;
                if (!fired)
                {
                    Projectile.Kill();
                    return;
                }

                age = LiveDuration;
                Projectile.netUpdate = true;
            }

            if (hasHead && !ending && (age < LiveDuration || matchingMove))
            {
                float angle;
                if (IsMouthMounted)
                {
                    // The mouth sprite drives the beam axis through the whip and each reversal.
                    angle = head.rotation - MathHelper.PiOver2;
                }
                else
                {
                    angle = SweepAngle(Projectile.ai[1], (int)Projectile.ai[2], age);
                }

                Projectile.velocity = angle.ToRotationVector2();
                Projectile.Center = head.Center + Projectile.velocity * (28f * head.scale);
            }

            age++;
            if (age > LiveDuration + FadeTicks)
            {
                Projectile.Kill();
                return;
            }

            if (!fired && age > 0)
            {
                fired = true;
                if (!Main.dedServ && !ending)
                {
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 1.1f, Pitch = -0.45f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 14f, 25, distanceFalloff: 1800f);
                }
            }

            if (!Main.dedServ && fired && age <= LiveDuration && age % 3 == 0)
            {
                Vector2 offset = Main.rand.NextVector2Circular(9f, 9f);
                Dust.NewDustPerfect(Projectile.Center + offset, DustID.SolarFlare, Projectile.velocity * 7f + offset * 0.35f, Scale: 2.4f).noGravity = true;
                Vector2 sparkPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(30f, MathHelper.Min(1100f, BeamLength * 0.9f));
                Dust.NewDustPerfect(sparkPosition, DustID.OrangeTorch, Projectile.velocity * 5f, Scale: 1.6f).noGravity = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)age);
            writer.Write(ending);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            age = reader.ReadInt16();
            ending = reader.ReadBoolean();
            fired = age > 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 origin = Projectile.Center;
            Vector2 endpoint = origin + Projectile.velocity * BeamLength;
            float point = 0f;
            float hitWidth = Projectile.ai[2] switch
            {
                RunawayMode => RunawayHitWidth,
                ThrashMode => ThrashHitWidth,
                _ => HitWidth
            };
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, endpoint, hitWidth, ref point);
        }

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (fired && IsThrash)
            {
                float fade = MathHelper.Clamp((LiveDuration + FadeTicks - age) / (float)FadeTicks, 0f, 1f);
                DestroyerHullLaser.DrawLegacyBeam(Projectile.Center, Projectile.velocity, ThrashLength, age, fade);
                return false;
            }

            beamEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DestroyerMawBeam", AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/TurbulentNoise", AssetRequestMode.ImmediateLoad);
            Texture2D primary = turbulentNoise.Value;
            Effect effect = beamEffect.Value;
            Vector2 origin = new Vector2(0f, primary.Height * 0.5f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            if (!fired)
            {
                float charge = MathHelper.Clamp((TellTicks + age) / (float)TellTicks, 0f, 1f);
                float tellWidth = MathHelper.Lerp(110f, 180f, charge);
                Vector2 tellSize = new Vector2(BeamLength, tellWidth);
                Vector2 tellBlocks = tellSize / 3f;
                float tellScroll = 14f * (TellTicks + age) / primary.Width;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                try
                {
                    effect.CurrentTechnique = effect.Techniques["DestroyerHullLaser"];
                    effect.Parameters["PixelGrid"].SetValue(new Vector4(tellBlocks.X, tellBlocks.Y, 1f / tellBlocks.X, 1f / tellBlocks.Y));
                    effect.Parameters["Opacity"].SetValue(0.35f + charge * 0.45f);
                    effect.Parameters["Time"].SetValue(0.35f * charge);
                    effect.Parameters["BeamColor"].SetValue(new Vector3(1.0f, 0.16f, 0.02f));
                    effect.Parameters["NoiseScale"].SetValue(new Vector4(
                        BeamLength / primary.Width,
                        tellWidth / primary.Height,
                        tellScroll,
                        BeamLength / 140f));
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.EntitySpriteDraw(primary, drawPosition, null, Color.White, Projectile.velocity.ToRotation(),
                        origin, tellSize / primary.Size(), SpriteEffects.None, 0f);
                }
                finally
                {
                    UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
                }

                return false;
            }

            float drawWidth = IsRunaway ? 126f : DrawWidth;
            Vector2 drawSize = new Vector2(BeamLength, drawWidth);
            Vector2 blocks = drawSize / PixelBlockSize;
            float opacity = MathHelper.Clamp((LiveDuration + FadeTicks - age) / (float)FadeTicks, 0f, 1f);
            float scroll = -14f * age / primary.Width;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                effect.CurrentTechnique = effect.Techniques["DestroyerMawBeam"];
                effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(age / 60f);
                effect.Parameters["BeamColor"].SetValue(new Vector3(0.85f, 0.08f, 0.015f));
                effect.Parameters["NoiseScale"].SetValue(new Vector4(
                    BeamLength / primary.Width,
                    drawWidth / primary.Height,
                    scroll,
                    BeamLength / 140f));
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(primary, drawPosition, null, Color.White, Projectile.velocity.ToRotation(),
                    origin, drawSize / primary.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || !fired)
            {
                return;
            }

            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Projectile.velocity * 3f + Main.rand.NextVector2Circular(9f, 9f);
                Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch, velocity, Scale: Main.rand.NextFloat(1.4f, 2.4f)).noGravity = true;
            }
        }
    }
}
