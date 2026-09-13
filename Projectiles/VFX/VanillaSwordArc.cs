using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Projectiles.VFX
{
    public enum VanillaSwordArcTexture : short
    {
        NightsEdge = ProjectileID.NightsEdge,
        Excalibur = ProjectileID.Excalibur,
        TrueExcalibur = ProjectileID.TrueExcalibur,
        TerraBlade = ProjectileID.TerraBlade2,
        HorsemansBlade = ProjectileID.TheHorsemansBlade,
    }

    public enum VanillaSwordArcEasing : byte
    {
        Linear,
        SmoothStep,
        EaseIn,
        EaseOut,
        SineInOut,
        /// <summary>Tick-authored cubic acceleration with an exponential follow-through.</summary>
        Weighted,
    }

    public enum VanillaSwordArcAnchor : byte
    {
        World,
        Player,
        NPC,
    }

    /// <summary>
    /// Appearance, timing, particles, and optional collision for <see cref="VanillaSwordArc"/>.
    /// Angles are world-space radians. Dust radial/tangential speeds are relative to the current
    /// arc direction, while <see cref="DustVelocityOffset"/> is world-space.
    /// </summary>
    public sealed class VanillaSwordArcSettings
    {
        public VanillaSwordArcTexture Texture = VanillaSwordArcTexture.NightsEdge;
        public VanillaSwordArcEasing Easing = VanillaSwordArcEasing.Linear;

        // Weighted is intentionally shaped the same way as PuppetNPC's Weighted sword clock.
        // Leave these at zero when using the ordinary fractional easing modes above.
        public int WeightedEaseInTicks;
        public int WeightedEaseOutTicks;
        public float WeightedEaseOutDecay = 6f;

        public int Duration = 25;
        public float StartAngle = MathHelper.Pi;
        public float SweepAngle = MathHelper.Pi;
        public float Radius = 94f;
        public float ConeHalfAngle = MathHelper.PiOver4;
        public float Opacity = 1f;
        public float FadeInFraction = 0.12f;
        public float FadeOutFraction = 0.2f;
        public float AfterimageLag = MathHelper.PiOver4;
        public float AfterimageOpacity = 0.75f;
        public float BodyOpacity = 0.7f;
        public float CoreOpacity = 0.35f;
        public float TipSparkleOpacity = 0.5f;
        public bool DrawTipSparkle = true;
        public bool TintWithWorldLighting = true;

        // Optional fire-material pass. It reuses the exact same source frame, pivot, rotation,
        // scale, and flip as the readable vanilla arc below, so the shader cannot drift away from
        // the sword silhouette. This uses the already-compiled GwynCinderBlade technique; enabling
        // it does not require rebuilding an effect file.
        public bool DrawCinderOverlay;
        public float CinderOverlayOpacity = 0.6f;
        public Color CinderOverlayDarkColor = new Color(64, 8, 2);
        public Color CinderOverlayFlameColor = new Color(255, 116, 14);
        public Color CinderOverlayCoreColor = new Color(255, 236, 172);

        public Color DarkColor = new Color(40, 20, 60);
        public Color BodyColor = new Color(80, 40, 180);
        public Color CoreColor = Color.White;

        // Collision is deliberately opt-in. Cosmetic arcs cannot accidentally become attacks.
        public bool EnableCollision;
        public float DamageStartFraction = 0.05f;
        public float DamageEndFraction = 0.95f;
        public bool SweepCollision = true;
        public int Penetrate = -1;
        public bool OwnerHitCheck;
        public float OwnerHitCheckDistance = 300f;

        // One configurable edge-breakup layer. Set DustType < 0 or DustCount = 0 to disable it.
        // More elaborate layered bursts should remain in the calling attack.
        public int DustType = DustID.Shadowflame;
        public Color DustColor = Color.White;
        public int DustCount = 1;
        public int DustAlpha = 100;
        public float DustScale = 0.9f;
        public float DustScaleJitter = 0.15f;
        public float DustRadiusFraction = 0.9f;
        public float DustAngularSpread = 0.7f;
        public float DustRadialSpeed;
        public float DustTangentialSpeed = 1f;
        public float DustVelocityJitter = 0.35f;
        public float DustInheritAnchorVelocity = 0.1f;
        public Vector2 DustVelocityOffset;
        public bool DustNoGravity = true;
        public bool DustNoLight;

        public Vector2 WorldVelocity;
        public bool FollowAnchorRotation;

        // Opt in for puppet weapons when the arc must follow the live hand and blade pose.
        public bool TrackPuppetBlade;

        public VanillaSwordArcSettings Clone()
        {
            return (VanillaSwordArcSettings)MemberwiseClone();
        }

        /// <summary>
        /// Creates a 180-degree swing that begins behind the supplied facing direction and ends
        /// in front. Set <paramref name="reverse"/> for the opposite travel direction.
        /// </summary>
        public static VanillaSwordArcSettings CreateFacingSwing(int direction, bool reverse = false)
        {
            direction = direction >= 0 ? 1 : -1;
            int sweepDirection = reverse ? -direction : direction;
            float forwardAngle = direction > 0 ? 0f : MathHelper.Pi;
            return new VanillaSwordArcSettings
            {
                StartAngle = forwardAngle + sweepDirection * MathHelper.Pi,
                SweepAngle = sweepDirection * MathHelper.Pi,
            };
        }

        internal void Sanitize()
        {
            if (!Enum.IsDefined(typeof(VanillaSwordArcTexture), Texture))
            {
                Texture = VanillaSwordArcTexture.NightsEdge;
            }
            if (!Enum.IsDefined(typeof(VanillaSwordArcEasing), Easing))
            {
                Easing = VanillaSwordArcEasing.Linear;
            }

            Duration = Math.Clamp(Duration, 1, 600);
            WeightedEaseInTicks = Math.Clamp(WeightedEaseInTicks, 0, Duration);
            WeightedEaseOutTicks = Math.Clamp(WeightedEaseOutTicks, 0, Duration - WeightedEaseInTicks);
            WeightedEaseOutDecay = Math.Max(0f, WeightedEaseOutDecay);
            Radius = Math.Clamp(Radius, 1f, 2000f);
            ConeHalfAngle = Math.Clamp(ConeHalfAngle, 0.01f, MathHelper.Pi);
            Opacity = MathHelper.Clamp(Opacity, 0f, 1f);
            FadeInFraction = MathHelper.Clamp(FadeInFraction, 0f, 1f);
            FadeOutFraction = MathHelper.Clamp(FadeOutFraction, 0f, 1f);
            AfterimageLag = Math.Clamp(AfterimageLag, 0f, MathHelper.TwoPi);
            AfterimageOpacity = Math.Max(0f, AfterimageOpacity);
            BodyOpacity = Math.Max(0f, BodyOpacity);
            CoreOpacity = Math.Max(0f, CoreOpacity);
            TipSparkleOpacity = Math.Max(0f, TipSparkleOpacity);
            CinderOverlayOpacity = MathHelper.Clamp(CinderOverlayOpacity, 0f, 1f);
            DamageStartFraction = MathHelper.Clamp(DamageStartFraction, 0f, 1f);
            DamageEndFraction = MathHelper.Clamp(DamageEndFraction, DamageStartFraction, 1f);
            Penetrate = Math.Max(-1, Penetrate);
            OwnerHitCheckDistance = Math.Max(0f, OwnerHitCheckDistance);
            DustCount = Math.Clamp(DustCount, 0, 32);
            DustAlpha = Math.Clamp(DustAlpha, 0, 255);
            DustScale = Math.Clamp(DustScale, 0.05f, 3f);
            DustScaleJitter = MathHelper.Clamp(DustScaleJitter, 0f, 1f);
            DustRadiusFraction = Math.Max(0f, DustRadiusFraction);
            DustAngularSpread = Math.Max(0f, DustAngularSpread);
            DustInheritAnchorVelocity = Math.Max(0f, DustInheritAnchorVelocity);
        }
    }

    /// <summary>
    /// Reusable version of the four-frame vanilla sword crescent used by Night's Edge, Excalibur,
    /// Terra Blade, and the Horseman's Blade. Use one of the Spawn methods so the settings and
    /// anchor information are synchronized in multiplayer.
    /// </summary>
    public class VanillaSwordArc : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/VFX/VanillaSwordArc";

        VanillaSwordArcSettings settings = new VanillaSwordArcSettings();
        VanillaSwordArcAnchor anchorType;
        int anchorIndex = -1;
        Vector2 anchorOffset;
        Vector2 anchorVelocity;
        float previousRotation;
        bool hasPreviousRotation;
        bool isFriendly;
        bool isHostile;
        bool hasTrackedPuppetBlade;
        bool hasEverTrackedPuppetBlade;
        Vector2 trackedPuppetBladeDirection;
        float trackedPuppetBladeProgress;

        static Effect cinderOverlayEffect;
        static Texture2D cinderOverlayNoise;

        float Progress => MathHelper.Clamp(Projectile.localAI[0] / settings.Duration, 0f, 1f);
        int SweepDirection => settings.SweepAngle >= 0f ? 1 : -1;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 602;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.netImportant = true;
        }

        public static int SpawnForPlayer(IEntitySource source, Player player, int damage, float knockBack,
            VanillaSwordArcSettings settings, Vector2? anchorOffset = null, bool friendly = true)
        {
            return Spawn(source, player.RotatedRelativePoint(player.MountedCenter), damage, knockBack,
                player.whoAmI, settings, VanillaSwordArcAnchor.Player, player.whoAmI,
                anchorOffset ?? Vector2.Zero, friendly, hostile: false);
        }

        public static int SpawnForNPC(IEntitySource source, NPC npc, int damage, float knockBack, int owner,
            VanillaSwordArcSettings settings, Vector2? anchorOffset = null, bool hostile = true)
        {
            return Spawn(source, npc.Center, damage, knockBack, owner, settings,
                VanillaSwordArcAnchor.NPC, npc.whoAmI, anchorOffset ?? Vector2.Zero,
                friendly: false, hostile);
        }

        public static int SpawnAt(IEntitySource source, Vector2 center, int damage, float knockBack, int owner,
            VanillaSwordArcSettings settings, bool friendly = false, bool hostile = false)
        {
            return Spawn(source, center, damage, knockBack, owner, settings,
                VanillaSwordArcAnchor.World, -1, Vector2.Zero, friendly, hostile);
        }

        static int Spawn(IEntitySource source, Vector2 center, int damage, float knockBack, int owner,
            VanillaSwordArcSettings requestedSettings, VanillaSwordArcAnchor anchorType, int anchorIndex,
            Vector2 anchorOffset, bool friendly, bool hostile)
        {
            VanillaSwordArcSettings resolvedSettings = requestedSettings?.Clone() ?? new VanillaSwordArcSettings();
            resolvedSettings.Sanitize();

            int index = Projectile.NewProjectile(source, center, Vector2.Zero,
                ModContent.ProjectileType<VanillaSwordArc>(),
                resolvedSettings.EnableCollision ? damage : 0, knockBack, owner);

            if (index < 0 || index >= Main.maxProjectiles)
            {
                return index;
            }

            Projectile projectile = Main.projectile[index];
            VanillaSwordArc arc = (VanillaSwordArc)projectile.ModProjectile;
            arc.settings = resolvedSettings;
            arc.anchorType = anchorType;
            arc.anchorIndex = anchorIndex;
            arc.anchorOffset = anchorOffset;
            arc.isFriendly = resolvedSettings.EnableCollision && friendly;
            arc.isHostile = resolvedSettings.EnableCollision && hostile;

            projectile.friendly = arc.isFriendly;
            projectile.hostile = arc.isHostile;
            projectile.penetrate = resolvedSettings.Penetrate;
            projectile.ownerHitCheck = resolvedSettings.OwnerHitCheck;
            projectile.ownerHitCheckDistance = resolvedSettings.OwnerHitCheckDistance;
            projectile.timeLeft = resolvedSettings.Duration + 2;

            // A landing-timed swing can finish its combo phase in the same tick that it spawns
            // its impact VFX. Sample the blade immediately so the brief fade keeps the real
            // hand/pivot instead of falling back to the NPC's centre on the following frame.
            if (resolvedSettings.TrackPuppetBlade
                && anchorType == VanillaSwordArcAnchor.NPC
                && anchorIndex >= 0 && anchorIndex < Main.maxNPCs
                && Main.npc[anchorIndex].ModNPC is PuppetNPC puppet)
            {
                arc.TryTrackPuppetBlade(puppet);
            }

            projectile.netUpdate = true;
            return index;
        }

        public override void AI()
        {
            if (!UpdateAnchor())
            {
                Projectile.Kill();
                return;
            }

            previousRotation = Projectile.rotation;
            float easedProgress = ApplyEasing(Progress);
            Projectile.rotation = hasTrackedPuppetBlade
                ? trackedPuppetBladeDirection.ToRotation()
                : settings.StartAngle + settings.SweepAngle * easedProgress + GetAnchorRotation();
            hasPreviousRotation = Projectile.localAI[0] > 0f;

            EmitDust();

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > settings.Duration)
            {
                Projectile.Kill();
            }
        }

        bool UpdateAnchor()
        {
            hasTrackedPuppetBlade = false;
            switch (anchorType)
            {
                case VanillaSwordArcAnchor.Player:
                    if (anchorIndex < 0 || anchorIndex >= Main.maxPlayers || !Main.player[anchorIndex].active)
                    {
                        return false;
                    }

                    Player player = Main.player[anchorIndex];
                    Projectile.Center = player.RotatedRelativePoint(player.MountedCenter) + anchorOffset;
                    anchorVelocity = player.velocity;
                    break;

                case VanillaSwordArcAnchor.NPC:
                    if (anchorIndex < 0 || anchorIndex >= Main.maxNPCs || !Main.npc[anchorIndex].active)
                    {
                        return false;
                    }

                    NPC npc = Main.npc[anchorIndex];
                    if (settings.TrackPuppetBlade
                        && npc.ModNPC is PuppetNPC puppet
                        && TryTrackPuppetBlade(puppet))
                    {
                        // The helper above has already snapped this frame to the live hand pose.
                    }
                    else if (hasEverTrackedPuppetBlade)
                    {
                        // The follow-through may outlive the active combo phase by a few frames.
                        // Keep the last honest blade pose for that fade; reverting to npc.Center
                        // produces the detached pop the effect is meant to avoid.
                        hasTrackedPuppetBlade = true;
                    }
                    else
                    {
                        Projectile.Center = npc.Center + anchorOffset;
                    }
                    anchorVelocity = npc.velocity;
                    break;

                default:
                    Projectile.Center += settings.WorldVelocity;
                    anchorVelocity = settings.WorldVelocity;
                    break;
            }

            Projectile.velocity = Vector2.Zero;
            return true;
        }

        bool TryTrackPuppetBlade(PuppetNPC puppet)
        {
            if (!puppet.TryGetMeleeSlashTrailPose(out Vector2 pivot,
                out Vector2 direction, out _, out float progress, out _,
                out _, out _, out _))
            {
                return false;
            }

            Projectile.Center = pivot + anchorOffset;
            trackedPuppetBladeDirection = direction;
            trackedPuppetBladeProgress = progress;
            Projectile.rotation = direction.ToRotation();
            hasTrackedPuppetBlade = true;
            hasEverTrackedPuppetBlade = true;
            return true;
        }

        float GetAnchorRotation()
        {
            if (!settings.FollowAnchorRotation)
            {
                return 0f;
            }

            return anchorType switch
            {
                VanillaSwordArcAnchor.Player => Main.player[anchorIndex].fullRotation,
                VanillaSwordArcAnchor.NPC => Main.npc[anchorIndex].rotation,
                _ => 0f,
            };
        }

        float ApplyEasing(float progress)
        {
            return settings.Easing switch
            {
                VanillaSwordArcEasing.SmoothStep => progress * progress * (3f - 2f * progress),
                VanillaSwordArcEasing.EaseIn => progress * progress,
                VanillaSwordArcEasing.EaseOut => 1f - (1f - progress) * (1f - progress),
                VanillaSwordArcEasing.SineInOut => (1f - MathF.Cos(MathHelper.Pi * progress)) * 0.5f,
                VanillaSwordArcEasing.Weighted => ApplyWeightedEasing(progress),
                _ => progress,
            };
        }

        float ApplyWeightedEasing(float progress)
        {
            int totalTicks = Math.Max(1, settings.Duration);
            int inTicks = Math.Clamp(settings.WeightedEaseInTicks, 0, totalTicks);
            int outTicks = Math.Clamp(settings.WeightedEaseOutTicks, 0, totalTicks - inTicks);
            int cruiseTicks = totalTicks - inTicks - outTicks;
            float decay = settings.WeightedEaseOutDecay > 0f
                ? settings.WeightedEaseOutDecay
                : 6f;
            float decayCoverage = 1f - MathF.Exp(-decay);
            float speed = 1f / (inTicks / 3f + cruiseTicks + outTicks * decayCoverage / decay);
            float inFraction = speed * inTicks / 3f;
            float cruiseFraction = speed * cruiseTicks;
            float elapsed = MathHelper.Clamp(progress, 0f, 1f) * totalTicks;

            if (elapsed < inTicks && inTicks > 0)
            {
                float rampProgress = elapsed / inTicks;
                return inFraction * rampProgress * rampProgress * rampProgress;
            }

            if (elapsed < inTicks + cruiseTicks)
            {
                return inFraction + speed * (elapsed - inTicks);
            }

            if (outTicks <= 0)
            {
                return 1f;
            }

            float tailProgress = (elapsed - inTicks - cruiseTicks) / outTicks;
            float settled = (1f - MathF.Exp(-decay * tailProgress)) / decayCoverage;
            return inFraction + cruiseFraction + (1f - inFraction - cruiseFraction) * settled;
        }

        float VisualOpacity()
        {
            float fadeIn = settings.FadeInFraction <= 0f
                ? 1f
                : Utils.GetLerpValue(0f, settings.FadeInFraction, Progress, clamped: true);
            float fadeOutStart = 1f - settings.FadeOutFraction;
            float fadeOut = settings.FadeOutFraction <= 0f
                ? 1f
                : Utils.GetLerpValue(1f, fadeOutStart, Progress, clamped: true);
            return settings.Opacity * fadeIn * fadeOut;
        }

        void EmitDust()
        {
            if (Main.dedServ || settings.DustType < 0 || settings.DustCount <= 0 || VisualOpacity() <= 0.01f)
            {
                return;
            }

            float dustRadius = settings.Radius * settings.DustRadiusFraction;
            for (int i = 0; i < settings.DustCount; i++)
            {
                float angle = Projectile.rotation
                    + Main.rand.NextFloatDirection() * settings.ConeHalfAngle * settings.DustAngularSpread;
                Vector2 radial = angle.ToRotationVector2();
                Vector2 tangent = new Vector2(-radial.Y, radial.X) * SweepDirection;
                Vector2 velocity = radial * settings.DustRadialSpeed
                    + tangent * settings.DustTangentialSpeed
                    + settings.DustVelocityOffset
                    + anchorVelocity * settings.DustInheritAnchorVelocity
                    + Main.rand.NextVector2Circular(settings.DustVelocityJitter, settings.DustVelocityJitter);
                float scale = settings.DustScale
                    * Main.rand.NextFloat(1f - settings.DustScaleJitter, 1f + settings.DustScaleJitter);

                Dust dust = Dust.NewDustPerfect(Projectile.Center + radial * dustRadius,
                    settings.DustType, velocity, settings.DustAlpha, settings.DustColor, scale);
                dust.noGravity = settings.DustNoGravity;
                dust.noLight = settings.DustNoLight;
            }
        }

        public override bool? CanDamage()
        {
            if (!settings.EnableCollision
                || Progress < settings.DamageStartFraction
                || Progress > settings.DamageEndFraction)
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!settings.EnableCollision)
            {
                return false;
            }

            if (targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, settings.Radius,
                Projectile.rotation, settings.ConeHalfAngle))
            {
                return true;
            }

            if (!settings.SweepCollision || !hasPreviousRotation)
            {
                return false;
            }

            float angleDelta = MathHelper.WrapAngle(Projectile.rotation - previousRotation);
            int samples = Math.Clamp((int)MathF.Ceiling(MathF.Abs(angleDelta)
                / Math.Max(0.05f, settings.ConeHalfAngle * 0.5f)), 1, 8);
            for (int i = 0; i < samples; i++)
            {
                float sampledRotation = previousRotation + angleDelta * (i / (float)samples);
                if (targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, settings.Radius,
                    sampledRotation, settings.ConeHalfAngle))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            float frameProgress = hasTrackedPuppetBlade ? trackedPuppetBladeProgress : Progress;
            int frameIndex = Math.Clamp((int)(frameProgress * 4f), 0, 3);
            Rectangle bodyFrame = texture.Frame(1, 4, 0, frameIndex);
            Vector2 origin = bodyFrame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawScale = settings.Radius / 94f * 1.1f;
            float opacity = VisualOpacity();
            SpriteEffects effects = SweepDirection < 0
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            float lighting = 1f;
            if (settings.TintWithWorldLighting)
            {
                lighting = MathHelper.Lerp(0.25f, 1f,
                    (lightColor.R + lightColor.G + lightColor.B) / (255f * 3f));
            }

            float lagRotation = Projectile.rotation
                - SweepDirection * settings.AfterimageLag * (1f - Progress);
            Main.EntitySpriteDraw(texture, drawPosition, bodyFrame,
                settings.DarkColor * (opacity * settings.AfterimageOpacity * lighting),
                lagRotation, origin, drawScale * 0.975f, effects, 0);

            Color bodyGlow = settings.BodyColor * (opacity * settings.BodyOpacity * lighting);
            bodyGlow.A = 0;
            Main.EntitySpriteDraw(texture, drawPosition, bodyFrame, bodyGlow,
                Projectile.rotation, origin, drawScale, effects, 0);

            Main.EntitySpriteDraw(texture, drawPosition, bodyFrame,
                settings.BodyColor * (opacity * settings.BodyOpacity * 0.55f * lighting),
                Projectile.rotation, origin, drawScale * 0.975f, effects, 0);

            Color coreGlow = settings.CoreColor * (opacity * settings.CoreOpacity * lighting);
            coreGlow.A = 0;
            Main.EntitySpriteDraw(texture, drawPosition, bodyFrame, coreGlow,
                Projectile.rotation, origin, drawScale * 0.98f, effects, 0);

            if (settings.DrawCinderOverlay && settings.CinderOverlayOpacity > 0f)
            {
                DrawCinderOverlay(texture, drawPosition, bodyFrame, origin, drawScale, effects,
                    opacity * settings.CinderOverlayOpacity, frameProgress);
            }

            if (settings.DrawTipSparkle && settings.TipSparkleOpacity > 0f)
            {
                float nativeTipRadius = (bodyFrame.Width * 0.5f - 4f) * drawScale;
                float tipRotation = Projectile.rotation
                    + SweepDirection * MathHelper.PiOver2 * Progress;
                Vector2 tipPosition = drawPosition + tipRotation.ToRotationVector2() * nativeTipRadius;
                DrawTipSparkle(tipPosition, settings.CoreColor,
                    opacity * settings.TipSparkleOpacity, Progress, tipRotation);
            }

            return false;
        }

        void DrawCinderOverlay(Texture2D texture, Vector2 drawPosition, Rectangle sourceRectangle,
            Vector2 origin, float scale, SpriteEffects effects, float opacity, float progress)
        {
            cinderOverlayEffect ??= ModContent.Request<Effect>(
                "tsorcRevamp/Effects/GwynCinderTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            cinderOverlayNoise ??= ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/T_Aurax44", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = cinderOverlayNoise;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                cinderOverlayEffect.CurrentTechnique = cinderOverlayEffect.Techniques["GwynCinderBlade"];
                cinderOverlayEffect.Parameters["CinderColor"].SetValue(settings.CinderOverlayDarkColor.ToVector3());
                cinderOverlayEffect.Parameters["FlameColor"].SetValue(settings.CinderOverlayFlameColor.ToVector3());
                cinderOverlayEffect.Parameters["CoreColor"].SetValue(settings.CinderOverlayCoreColor.ToVector3());
                cinderOverlayEffect.Parameters["Opacity"].SetValue(opacity);
                cinderOverlayEffect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                cinderOverlayEffect.Parameters["DrawSize"].SetValue(sourceRectangle.Size());
                cinderOverlayEffect.Parameters["PrimaryTextureSize"].SetValue(texture.Size());
                cinderOverlayEffect.Parameters["Progress"].SetValue(progress);
                cinderOverlayEffect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(texture, drawPosition, sourceRectangle, Color.White,
                    Projectile.rotation, origin, scale, effects, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawTipSparkle(Vector2 drawPosition, Color color, float opacity,
            float progress, float rotation)
        {
            Texture2D sparkle = TextureAssets.Extra[98].Value;
            Vector2 origin = sparkle.Size() * 0.5f;
            float pulse = MathF.Sin(MathHelper.Pi * progress);
            Vector2 longScale = new Vector2(0.45f, 2f) * (0.6f + pulse * 0.4f);
            Vector2 shortScale = new Vector2(0.45f, 1f) * (0.6f + pulse * 0.4f);
            Color glow = color * opacity;
            glow.A = 0;

            Main.EntitySpriteDraw(sparkle, drawPosition, null, glow,
                rotation, origin, longScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(sparkle, drawPosition, null, glow * 0.65f,
                rotation + MathHelper.PiOver2, origin, shortScale, SpriteEffects.None, 0);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)anchorType);
            writer.Write(anchorIndex);
            writer.WriteVector2(anchorOffset);
            writer.Write(isFriendly);
            writer.Write(isHostile);

            writer.Write((short)settings.Texture);
            writer.Write((byte)settings.Easing);
            writer.Write(settings.WeightedEaseInTicks);
            writer.Write(settings.WeightedEaseOutTicks);
            writer.Write(settings.WeightedEaseOutDecay);
            writer.Write(settings.Duration);
            writer.Write(settings.StartAngle);
            writer.Write(settings.SweepAngle);
            writer.Write(settings.Radius);
            writer.Write(settings.ConeHalfAngle);
            writer.Write(settings.Opacity);
            writer.Write(settings.FadeInFraction);
            writer.Write(settings.FadeOutFraction);
            writer.Write(settings.AfterimageLag);
            writer.Write(settings.AfterimageOpacity);
            writer.Write(settings.BodyOpacity);
            writer.Write(settings.CoreOpacity);
            writer.Write(settings.TipSparkleOpacity);
            writer.Write(settings.DrawTipSparkle);
            writer.Write(settings.TintWithWorldLighting);
            writer.Write(settings.DrawCinderOverlay);
            writer.Write(settings.CinderOverlayOpacity);
            WriteColor(writer, settings.CinderOverlayDarkColor);
            WriteColor(writer, settings.CinderOverlayFlameColor);
            WriteColor(writer, settings.CinderOverlayCoreColor);
            WriteColor(writer, settings.DarkColor);
            WriteColor(writer, settings.BodyColor);
            WriteColor(writer, settings.CoreColor);

            writer.Write(settings.EnableCollision);
            writer.Write(settings.DamageStartFraction);
            writer.Write(settings.DamageEndFraction);
            writer.Write(settings.SweepCollision);
            writer.Write(settings.Penetrate);
            writer.Write(settings.OwnerHitCheck);
            writer.Write(settings.OwnerHitCheckDistance);

            writer.Write(settings.DustType);
            WriteColor(writer, settings.DustColor);
            writer.Write(settings.DustCount);
            writer.Write(settings.DustAlpha);
            writer.Write(settings.DustScale);
            writer.Write(settings.DustScaleJitter);
            writer.Write(settings.DustRadiusFraction);
            writer.Write(settings.DustAngularSpread);
            writer.Write(settings.DustRadialSpeed);
            writer.Write(settings.DustTangentialSpeed);
            writer.Write(settings.DustVelocityJitter);
            writer.Write(settings.DustInheritAnchorVelocity);
            writer.WriteVector2(settings.DustVelocityOffset);
            writer.Write(settings.DustNoGravity);
            writer.Write(settings.DustNoLight);

            writer.WriteVector2(settings.WorldVelocity);
            writer.Write(settings.FollowAnchorRotation);
            writer.Write(settings.TrackPuppetBlade);

            // Preserve an impact pose when the source combo has already advanced by the time a
            // remote client receives this cosmetic projectile.
            writer.Write(hasEverTrackedPuppetBlade);
            writer.WriteVector2(Projectile.Center);
            writer.WriteVector2(trackedPuppetBladeDirection);
            writer.Write(trackedPuppetBladeProgress);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            anchorType = (VanillaSwordArcAnchor)reader.ReadByte();
            anchorIndex = reader.ReadInt32();
            anchorOffset = reader.ReadVector2();
            isFriendly = reader.ReadBoolean();
            isHostile = reader.ReadBoolean();

            settings.Texture = (VanillaSwordArcTexture)reader.ReadInt16();
            settings.Easing = (VanillaSwordArcEasing)reader.ReadByte();
            settings.WeightedEaseInTicks = reader.ReadInt32();
            settings.WeightedEaseOutTicks = reader.ReadInt32();
            settings.WeightedEaseOutDecay = reader.ReadSingle();
            settings.Duration = reader.ReadInt32();
            settings.StartAngle = reader.ReadSingle();
            settings.SweepAngle = reader.ReadSingle();
            settings.Radius = reader.ReadSingle();
            settings.ConeHalfAngle = reader.ReadSingle();
            settings.Opacity = reader.ReadSingle();
            settings.FadeInFraction = reader.ReadSingle();
            settings.FadeOutFraction = reader.ReadSingle();
            settings.AfterimageLag = reader.ReadSingle();
            settings.AfterimageOpacity = reader.ReadSingle();
            settings.BodyOpacity = reader.ReadSingle();
            settings.CoreOpacity = reader.ReadSingle();
            settings.TipSparkleOpacity = reader.ReadSingle();
            settings.DrawTipSparkle = reader.ReadBoolean();
            settings.TintWithWorldLighting = reader.ReadBoolean();
            settings.DrawCinderOverlay = reader.ReadBoolean();
            settings.CinderOverlayOpacity = reader.ReadSingle();
            settings.CinderOverlayDarkColor = ReadColor(reader);
            settings.CinderOverlayFlameColor = ReadColor(reader);
            settings.CinderOverlayCoreColor = ReadColor(reader);
            settings.DarkColor = ReadColor(reader);
            settings.BodyColor = ReadColor(reader);
            settings.CoreColor = ReadColor(reader);

            settings.EnableCollision = reader.ReadBoolean();
            settings.DamageStartFraction = reader.ReadSingle();
            settings.DamageEndFraction = reader.ReadSingle();
            settings.SweepCollision = reader.ReadBoolean();
            settings.Penetrate = reader.ReadInt32();
            settings.OwnerHitCheck = reader.ReadBoolean();
            settings.OwnerHitCheckDistance = reader.ReadSingle();

            settings.DustType = reader.ReadInt32();
            settings.DustColor = ReadColor(reader);
            settings.DustCount = reader.ReadInt32();
            settings.DustAlpha = reader.ReadInt32();
            settings.DustScale = reader.ReadSingle();
            settings.DustScaleJitter = reader.ReadSingle();
            settings.DustRadiusFraction = reader.ReadSingle();
            settings.DustAngularSpread = reader.ReadSingle();
            settings.DustRadialSpeed = reader.ReadSingle();
            settings.DustTangentialSpeed = reader.ReadSingle();
            settings.DustVelocityJitter = reader.ReadSingle();
            settings.DustInheritAnchorVelocity = reader.ReadSingle();
            settings.DustVelocityOffset = reader.ReadVector2();
            settings.DustNoGravity = reader.ReadBoolean();
            settings.DustNoLight = reader.ReadBoolean();

            settings.WorldVelocity = reader.ReadVector2();
            settings.FollowAnchorRotation = reader.ReadBoolean();
            settings.TrackPuppetBlade = reader.ReadBoolean();
            hasEverTrackedPuppetBlade = reader.ReadBoolean();
            Projectile.Center = reader.ReadVector2();
            trackedPuppetBladeDirection = reader.ReadVector2();
            trackedPuppetBladeProgress = reader.ReadSingle();
            hasTrackedPuppetBlade = hasEverTrackedPuppetBlade;
            if (hasEverTrackedPuppetBlade)
                Projectile.rotation = trackedPuppetBladeDirection.ToRotation();
            settings.Sanitize();

            Projectile.friendly = settings.EnableCollision && isFriendly;
            Projectile.hostile = settings.EnableCollision && isHostile;
            Projectile.penetrate = settings.Penetrate;
            Projectile.ownerHitCheck = settings.OwnerHitCheck;
            Projectile.ownerHitCheckDistance = settings.OwnerHitCheckDistance;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, settings.Duration + 2);
        }

        static void WriteColor(BinaryWriter writer, Color color)
        {
            writer.Write(color.PackedValue);
        }

        static Color ReadColor(BinaryReader reader)
        {
            Color color = default;
            color.PackedValue = reader.ReadUInt32();
            return color;
        }
    }
}
