using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.TheDestroyer;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal sealed class DestroyerHullLaser : ModProjectile
    {
        internal const int FireTicks = 30;
        internal const int FadeTicks = 12;
        internal const int OverheadPort = 2;
        internal const int EnclosingPort = 4;
        private const int Length = 2000;
        private const int ProbeLength = 2600;
        private const int OverheadLength = 1120;
        private const int EnclosingLength = 740;
        private const int HitWidth = 22;
        private const int ProbeLiveTicks = 16;
        private const float PixelBlockSize = 3f;
        private static ReLogic.Content.Asset<Effect> laserEffect;
        private static ReLogic.Content.Asset<Texture2D> turbulentNoise;
        private Vector2 initialDirection;
        private float initialHostRotation;
        private int age;
        private bool initialized;
        private bool played;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyLingeringLaser));

        private int TellTicks => (int)Projectile.ai[0];
        private int HostIndex => (int)Projectile.ai[1];
        private bool IsProbe => Projectile.ai[2] < 0f;
        private bool IsOrbitBeam => Projectile.ai[2] >= OverheadPort;

        private int EndTick
        {
            get
            {
                if (IsProbe)
                {
                    return TellTicks + ProbeLiveTicks;
                }

                if (IsOrbitBeam)
                {
                    return TellTicks + TheDestroyer.OrbitFireTicks;
                }

                return TellTicks + FireTicks;
            }
        }

        private float BeamLength
        {
            get
            {
                if (IsProbe)
                {
                    return ProbeLength;
                }

                if (IsOrbitBeam)
                {
                    return Projectile.ai[2] >= EnclosingPort ? EnclosingLength : OverheadLength;
                }

                return Length;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = ProbeLength;
        }

        public override void Unload()
        {
            laserEffect = null;
            turbulentNoise = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => initialized && age >= TellTicks && age < EndTick;

        public override void AI()
        {
            NPC host = null;
            if (HostIndex >= 0 && HostIndex < Main.maxNPCs)
            {
                NPC candidate = Main.npc[HostIndex];
                bool validType = false;
                if (IsProbe)
                {
                    validType = candidate.type == NPCID.Probe && candidate.life * 4 >= candidate.lifeMax;
                }
                else
                {
                    validType = candidate.type == NPCID.TheDestroyerBody
                        || candidate.type == NPCID.TheDestroyer
                        || candidate.type == NPCID.TheDestroyerTail;
                }

                if (candidate.active && validType)
                {
                    host = candidate;
                }
            }

            if (host == null)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
            {
                initialized = true;
                initialDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                initialHostRotation = host.rotation;
            }

            // Keep the telegraph and active laser tethered to the emitting probe or body segment.
            Projectile.Center = host.Center;

            if (IsProbe)
            {
                host.velocity *= 0.86f;
                if (age < TellTicks - 24 && host.HasValidTarget)
                {
                    Player targetPlayer = Main.player[host.target];
                    Vector2 toTarget = (targetPlayer.Center - host.Center).SafeNormalize(initialDirection);
                    float currentAngle = initialDirection.ToRotation();
                    float targetAngle = toTarget.ToRotation();
                    initialDirection = currentAngle.AngleTowards(targetAngle, 0.03f).ToRotationVector2();
                }

                Projectile.velocity = initialDirection;
            }
            else if (IsOrbitBeam)
            {
                // The port mount and the beam share the segment's facing every tick, including during the tell.
                int portMode = (int)Projectile.ai[2];
                int portSide = portMode % 2 == 0 ? 1 : -1;
                Vector2 portDirection = host.rotation.ToRotationVector2() * portSide;
                Projectile.Center = host.Center + portDirection * (TheDestroyer.HullPortOffset * host.scale);
                Projectile.velocity = portDirection;
            }
            else
            {
                float followFactor = MathHelper.Clamp(Projectile.ai[2], 0f, 1f);
                float deltaRotation = MathHelper.WrapAngle(host.rotation - initialHostRotation) * followFactor;
                Projectile.velocity = initialDirection.RotatedBy(deltaRotation);
            }

            age++;
            if (age >= EndTick + FadeTicks)
            {
                Projectile.Kill();
                return;
            }

            if (!Main.dedServ)
            {
                if (age < TellTicks)
                {
                    if (age % 5 == 0)
                    {
                        Vector2 chargeOffset = Main.rand.NextVector2CircularEdge(18f, 18f);
                        Vector2 chargeVelocity = -chargeOffset * 0.12f + host.velocity;
                        Dust chargeDust = Dust.NewDustPerfect(Projectile.Center + chargeOffset, DustID.OrangeTorch, chargeVelocity, Scale: 1.25f);
                        chargeDust.noGravity = true;
                    }
                }
                else if (age < EndTick)
                {
                    if (!played)
                    {
                        played = true;
                        if (IsProbe || IsOrbitBeam || Projectile.identity % 4 == 0)
                        {
                            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.55f }, Projectile.Center);
                        }
                    }

                    if (age % 5 == 0)
                    {
                        Vector2 dustPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(24f, BeamLength * 0.85f);
                        Dust beamDust = Dust.NewDustPerfect(dustPosition, DustID.OrangeTorch, Projectile.velocity * 3.5f, Scale: 1.55f);
                        beamDust.noGravity = true;
                    }
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
            if (initialized)
            {
                writer.Write((short)age);
                writer.WriteVector2(initialDirection);
                writer.Write(initialHostRotation);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                initialized = true;
                age = reader.ReadInt16();
                initialDirection = reader.ReadVector2();
                initialHostRotation = reader.ReadSingle();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * BeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, HitWidth, ref point);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (IsProbe)
            {
                target.AddBuff(ModContent.BuffType<Buffs.ThermalRise>(), 320);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized)
            {
                return false;
            }

            if (IsOrbitBeam && age < TellTicks)
            {
                Vector2 tellEnd = Projectile.Center + Projectile.velocity * BeamLength;
                float progress = age / (float)TellTicks;
                float tellOpacity = MathHelper.Lerp(0.28f, 0.85f, progress);
                DrawBand(Main.spriteBatch, Projectile.Center, tellEnd, Color.OrangeRed * (tellOpacity * 0.6f), 5f);
                DrawBand(Main.spriteBatch, Projectile.Center, tellEnd, Color.Orange * tellOpacity, 2f);
                return false;
            }

            laserEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DestroyerMawBeam", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/TurbulentNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            Effect effect = laserEffect.Value;
            Texture2D primary = turbulentNoise.Value;

            bool firing = age >= TellTicks;
            float beamLength = BeamLength;
            float drawWidth = 110f;
            float opacity = 0.35f;
            float scroll = 0f;
            float coreIntensity = 0f;

            if (!firing)
            {
                float chargeRatio = MathHelper.Clamp(age / (float)Math.Max(1, TellTicks), 0f, 1f);
                float pulse = Math.Abs((float)Math.Cos(age / 14f)) * 0.16f;
                drawWidth = MathHelper.Lerp(90f, 130f, chargeRatio);
                opacity = 0.24f + 0.24f * chargeRatio + pulse;
                scroll = 10f * age / primary.Width;
                coreIntensity = 0f;
            }
            else
            {
                float fade = MathHelper.Clamp((EndTick + FadeTicks - age) / (float)FadeTicks, 0f, 1f);
                int fireAge = age - TellTicks;
                drawWidth = IsOrbitBeam ? 110f : 220f;
                opacity = fade;
                scroll = -12f * fireAge / primary.Width;
                coreIntensity = 1.55f * fade;
            }

            Vector3 hslColor = Main.rgbToHsl(new Color(255, 36, 8));
            hslColor.X += 0.025f * (float)Math.Cos(age / 25f);
            Color beamColor = Main.hslToRgb(hslColor);

            Vector2 drawSize = new Vector2(beamLength, drawWidth);
            Vector2 blocks = drawSize / PixelBlockSize;
            Vector2 origin = new Vector2(0f, primary.Height * 0.5f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                effect.CurrentTechnique = effect.Techniques["DestroyerHullLaser"];
                effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(coreIntensity);
                effect.Parameters["BeamColor"].SetValue(beamColor.ToVector3());
                effect.Parameters["NoiseScale"].SetValue(new Vector4(
                    beamLength / primary.Width,
                    drawWidth / primary.Height,
                    scroll,
                    beamLength / 110f));
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

        internal static void DrawBand(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 offset = end - start;
            if (offset.LengthSquared() < 0.01f)
            {
                return;
            }

            Rectangle source = new Rectangle(0, 0, 1, 1);
            Vector2 origin = new Vector2(0f, 0.5f);
            Vector2 scale = new Vector2(offset.Length(), width);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, source, color,
                offset.ToRotation(), origin, scale, SpriteEffects.None, 0f);
        }

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || !initialized)
            {
                return;
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 dustVelocity = Projectile.velocity * 3f + Main.rand.NextVector2Circular(4f, 4f);
                Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch, dustVelocity, Scale: 1.25f).noGravity = true;
            }
        }
    }
}
