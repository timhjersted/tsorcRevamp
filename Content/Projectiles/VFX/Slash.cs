using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee._Animations;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    // Custom swings provide their rendered blade, rather than asking the trail to guess its
    // radius from an inventory rectangle. Returning false ends emission and starts the short fade.
    public interface IBladeSlashSource
    {
        Color SwordSlashColor { get; }
        int SwordSlashSequence { get; }
        bool TryGetSwordSlashPose(out Vector2 hand, out Vector2 bladeHeel, out Vector2 bladeTip);
    }

    public class Slash : DynamicTrail
    {
        public const int BladeFadeTicks = 6;
        public const float BladeHeadFadeStart = 0.985f;
        private bool bladeSourceDriven;
        private float bladeBandHalfWidth;
        private bool bladeRotationInitialized;
        private float previousBladeRotation;
        private Vector2 bladeAnchorOffset;
        private int bladeSequence;
        private uint npcSampleTick = uint.MaxValue;
        private int npcTrailAge;

        private bool NPCDriven => Projectile.ai[1] == 2f;

        public override string Texture => base.Texture;
        public sealed override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;

            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 500;
            Projectile.hide = true;
            collisionPadding = 5;
            NPCSource = false;
            trailCollision = false;
            noFadeOut = true;
            // Keep trail points in world space so camera movement cannot separate their pivots.
            ScreenSpace = false;
            newPointDistance = 0.000f;
            if (!Main.dedServ)
            {
                customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (NPCDriven)
            {
                // Sample the final held-weapon draw after the NPC has rendered this frame.
                behindProjectiles.Add(index);
            }
            else
            {
                behindNPCs.Add(index);
            }
        }

        bool rotatingRight;
        bool initializedSlash;
        float speed;
        float progress;
        float lastPercent;
        Color slashColor = Color.White;
        float timeMax = 1;
        tsorcSlashStyle slashStyle = tsorcSlashStyle.Metal;
        bool flippedSwing = false;
        int AttackId = 0;

        // ai[0] > 0 means "driven by another projectile": ai[0] - 1 is that projectile's identity, and its
        // Projectile.rotation is the blade's world angle. Used by custom-swing weapons (SwordOfLordGwyn) whose
        // swing is a projectile, not the held item's useAnimation. 0 = the normal held-item-driven slash.
        // ai[1] = 1 opts that source into measured blade geometry and active-cut timing; 0 keeps legacy callers.
        // ai[1] = 2 instead names an NPC in ai[0] (index + 1), with its maximum emission ticks in ai[2].
        bool DrivenByProjectile => Projectile.ai[0] > 0f;

        // The strip is drawn trailWidth on EACH side of the points, and the points sit trailWidth from the pivot, so the
        // normal slash covers radius 0..2*trailWidth. A driven (projectile) swing instead gets a thin band, a third as
        // wide: with points at radius R the glow covers 0.67R..1.33R, so R = 0.75 * blade length puts its outer edge on the tip.
        public override float WidthFunction(float progress)
        {
            if (bladeSourceDriven)
            {
                return bladeBandHalfWidth;
            }

            if (DrivenByProjectile)
            {
                return trailWidth / 3f;
            }

            return trailWidth;
        }

        // Bound the complete shader strip to the real blade's radial span. Radius +/- halfWidth
        // gives heel..tip exactly, including the player's current item scale and facing/gravity.
        public static void GetBladeBand(Vector2 hand, Vector2 bladeHeel, Vector2 bladeTip,
            out Vector2 direction, out float radius, out float halfWidth)
        {
            Vector2 handToTip = bladeTip - hand;
            float outerRadius = handToTip.Length();
            direction = handToTip.SafeNormalize(Vector2.UnitX);
            float heelProjection = Vector2.Dot(bladeHeel - hand, direction);
            float innerRadius = MathHelper.Clamp(heelProjection, 0f, outerRadius);
            radius = (innerRadius + outerRadius) * 0.5f;
            halfWidth = (outerRadius - innerRadius) * 0.5f;
        }

        // DrawData origins are already mirrored by the puppet's item draw. Mirror each measured
        // sprite texel as well before applying that origin, final scale and rotation.
        public static Vector2 TransformBladePixel(Vector2 pixel, Vector2 frameSize, Vector2 origin,
            Vector2 scale, float rotation, SpriteEffects effects, Vector2 position)
        {
            if ((effects & SpriteEffects.FlipHorizontally) != 0)
            {
                pixel.X = frameSize.X - pixel.X;
            }

            if ((effects & SpriteEffects.FlipVertically) != 0)
            {
                pixel.Y = frameSize.Y - pixel.Y;
            }

            Vector2 offset = (pixel - origin) * scale;
            return position + Vector2.Transform(offset, Matrix.CreateRotationZ(rotation));
        }

        private void UpdateBladeSource(Vector2 bodyCenter, IBladeSlashSource source)
        {
            bladeSourceDriven = true;
            Vector2 hand = Vector2.Zero;
            Vector2 bladeHeel = Vector2.Zero;
            Vector2 bladeTip = Vector2.Zero;
            bool emitting = false;

            bool sameSwing = !bladeRotationInitialized || source == null || source.SwordSlashSequence == bladeSequence;

            if (source != null && !reachedEnd && sameSwing)
            {
                emitting = source.TryGetSwordSlashPose(out hand, out bladeHeel, out bladeTip);
            }

            if (!emitting)
            {
                // Remote spawn packets can precede the source's first live pose. Wait invisibly
                // for that pose; the initialization cap still removes an orphaned effect.
                if (!bladeRotationInitialized)
                {
                    return;
                }

                // End at the cut, rather than waiting through its windup/settle projectile's
                // remaining lifetime. The same shader fade/erosion as Moonlight runs over six ticks.
                reachedEnd = true;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, BladeFadeTicks);
                trailCurrentLength = CalculateLength();
                return;
            }

            GetBladeBand(hand, bladeHeel, bladeTip, out Vector2 direction, out float radius, out bladeBandHalfWidth);
            slashColor = source.SwordSlashColor;
            FollowOwnerPivot(hand);
            bladeAnchorOffset = hand - bodyCenter;
            Projectile.Center = hand;
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.timeLeft = BladeFadeTicks + 1;

            if (!bladeRotationInitialized)
            {
                previousBladeRotation = Projectile.rotation;
                bladeSequence = source.SwordSlashSequence;
                bladeRotationInitialized = true;
                trailPositions.Add(hand + direction * radius);
                trailRotations.Add(Projectile.rotation + MathHelper.Pi);
            }

            float rotationDelta = MathHelper.WrapAngle(Projectile.rotation - previousBladeRotation);
            int subdivisionCount = Math.Min(120, (int)Math.Ceiling(Math.Abs(rotationDelta) * 120f));

            for (int i = 1; i <= subdivisionCount; i++)
            {
                float fraction = i / (float)subdivisionCount;
                float rotation = previousBladeRotation + rotationDelta * fraction;
                Vector2 radial = (rotation - MathHelper.PiOver2).ToRotationVector2();
                trailPositions.Add(hand + radial * radius);
                trailRotations.Add(rotation + MathHelper.Pi);
            }

            while (trailPositions.Count > trailPointLimit)
            {
                trailPositions.RemoveAt(0);
                trailRotations.RemoveAt(0);
            }

            previousBladeRotation = Projectile.rotation;
            trailCurrentLength = CalculateLength();
        }

        float rotationDirection = 0;
        bool reachedEnd = false;
        Vector2 trailPivot;
        bool trailPivotInitialized;

        private void FollowOwnerPivot(Vector2 pivot)
        {
            if (trailPositions == null)
            {
                return;
            }

            if (trailPivotInitialized)
            {
                Vector2 movement = pivot - trailPivot;
                for (int i = 0; i < trailPositions.Count; i++)
                {
                    trailPositions[i] += movement;
                }
            }

            trailPivot = pivot;
            trailPivotInitialized = true;
        }

        public override void AI()
        {
            if (NPCDriven)
            {
                if (!initializedSlash)
                {
                    Projectile.timeLeft = Math.Max(1, (int)Projectile.ai[2]) + BladeFadeTicks;
                    Initialize();
                    initializedSlash = true;
                }

                int npcIndex = (int)Projectile.ai[0] - 1;

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    Projectile.Center = Main.npc[npcIndex].Center;
                }
                else
                {
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, BladeFadeTicks);
                }

                // NPC blade sampling happens in the additive draw pass, after the current
                // sprite has supplied its final DrawData. Servers only advance the safety lifetime.
                npcTrailAge++;
                return;
            }

            Player owner = Main.player[Projectile.owner];

            if (!initializedSlash)
            {
                if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aimingInit))
                {
                    AttackId = aimingInit.AttackId;
                }
                // The trail's radial path is authored in the opposite screen-space winding from
                // the held-item rotation, so the same AttackId parity must be inverted here.
                // Without this, a rising/bottom-up sword swing leaves the descending/top-down
                // slash trail (and vice versa).
                flippedSwing = AttackId % 2 == 0;

                // Prefer the animation's own flip flag: for ordinary swings it matches the parity above
                // (QuickSlash sets it from AttackId before the id is incremented, hence the apparent
                // inversion), but weapons that override the flip for one swing (e.g. the Ancient Fire
                // Axe's always-overhead held attacks) would otherwise get a trail running the wrong way.
                if (owner.HeldItem.TryGetGlobalItem(out QuickSlashMeleeAnimation animationInit) && animationInit.Enabled && animationInit.FlipAttackEachSwing)
                {
                    flippedSwing = animationInit.IsAttackFlipped;
                }
                trailWidth = (int)(Math.Sqrt(owner.HeldItem.height * owner.HeldItem.height + owner.HeldItem.width * owner.HeldItem.width) * owner.HeldItem.scale);
                trailWidth = Math.Max(trailWidth, 50);
                Projectile.timeLeft = owner.itemAnimationMax + 10;

                if (DrivenByProjectile)
                {
                    // The driving swing ends on its own (reachedEnd below); this is only a safety cap.
                    Projectile.timeLeft = 120;

                    if (Projectile.ai[1] > 0f)
                    {
                        Projectile.timeLeft = 30;
                    }
                }

                tsorcInstancedGlobalItem instancedGlobal = owner.HeldItem.GetGlobalItem<tsorcInstancedGlobalItem>();
                float visualScale = Math.Max(0.05f, instancedGlobal.slashVisualScale);
                trailWidth = Math.Max(1, (int)(trailWidth * visualScale));
                slashColor = instancedGlobal.slashColor;
                slashStyle = instancedGlobal.slashStyle;
                initializedSlash = true;
            }

            Projectile.Center = owner.Center;

            if (!initialized)
            {
                Initialize();
            }

            Projectile drivingSwing = null;

            if (DrivenByProjectile)
            {
                int drivingIndex = Projectile.GetByUUID(Projectile.owner, (int)Projectile.ai[0] - 1);

                if (drivingIndex >= 0)
                {
                    drivingSwing = Main.projectile[drivingIndex];
                }

                IBladeSlashSource bladeSource = drivingSwing?.ModProjectile as IBladeSlashSource;

                if (Projectile.ai[1] > 0f)
                {
                    if (drivingSwing == null || !drivingSwing.active)
                    {
                        bladeSource = null;
                    }

                    UpdateBladeSource(owner.Center, bladeSource);
                    return;
                }

                if (drivingSwing == null || !drivingSwing.active)
                {
                    // Swing is over: stop adding points and let the existing trail fade (trailIntensity ramps over the last 15 ticks).
                    reachedEnd = true;
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, 15);
                }
            }
            else if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aiming))
            {
                //Main.NewText(aiming.AttackId);
                if (AttackId != aiming.AttackId)
                {
                    reachedEnd = true;
                }
            }

            // Moonlight's held-item path keeps its established pivot, sizing and timing.
            FollowOwnerPivot(owner.Center);

            bool flipped = owner.gravDir == -1f && ModContent.GetInstance<tsorcRevampConfig>().GravityFix;

            if (Projectile.timeLeft > 10 && !reachedEnd)
            {
                if (drivingSwing != null)
                {
                    // Trail points sit at (rotation - PiOver2), so +PiOver2 puts them exactly along the driving blade.
                    Projectile.rotation = drivingSwing.rotation + MathHelper.PiOver2;
                }
                else
                {
                    Projectile.rotation = QuickSlashMeleeAnimation.MeleeSwingRotation(owner, owner.HeldItem, flippedSwing, 1.2f);
                    if (owner.gravDir == 1f)
                    {
                        Projectile.rotation += MathHelper.PiOver2;
                    }
                    else if (owner.direction == 1)
                    {
                        Projectile.rotation += (float)Math.PI;
                    }
                }

                //Skip the first
                if (lastPercent == 0)
                {
                    lastPercent = Projectile.rotation;
                    return;
                }

                float rotationDelta = MathHelper.WrapAngle(Projectile.rotation - lastPercent);
                int subdivisionCount = Math.Min((int)(Math.Abs(rotationDelta) * 120), 120);
                for (int i = 0; i < subdivisionCount; i++)
                {
                    float interpolatedRotation = lastPercent + rotationDelta * (i / (float)subdivisionCount);
                    trailPositions.Add(owner.Center + new Vector2(trailWidth, 0).RotatedBy(interpolatedRotation - MathHelper.PiOver2));
                    trailRotations.Add(interpolatedRotation + MathHelper.Pi);
                }

                while (trailPositions.Count > trailPointLimit)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
            }

            // Slash owns its trail points instead of calling DynamicTrail.AI(), so keep the
            // length field current here. The 2x2 shader grid uses this value for its X block count;
            // leaving it at zero collapses the whole arc to one UV sample and produces a solid shape.
            trailCurrentLength = CalculateLength();
            lastPercent = Projectile.rotation;
        }

        //Projectile.rotation += speed * 3 * ((float)Math.Pow(Math.Sin(MathHelper.Pi * ((float)Projectile.timeLeft) / ((float)owner.HeldItem.useTime)), 5) + 0.08f);

        //Sample one line of the base noise texture, centered on this U coordinate
        float baseNoiseUOffset = 0;
        float trailIntensity = 1;
        public override void SetEffectParameters(Effect effect)
        {

            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            if (baseNoiseUOffset == 0)
            {
                baseNoiseUOffset = Main.rand.NextFloat();
            }

            Texture2D noiseTexture = tsorcRevamp.NoiseWavy;
            switch (slashStyle)
            {
                case tsorcSlashStyle.Metal:
                    {
                        noiseTexture = tsorcRevamp.NoiseVoronoi;
                        break;
                    }
                case tsorcSlashStyle.LightMagic:
                    {
                        noiseTexture = tsorcRevamp.NoiseVoronoi;
                        break;
                    }
                case tsorcSlashStyle.DarkMagic:
                    {
                        noiseTexture = tsorcRevamp.NoiseWavy;
                        break;
                    }
                case tsorcSlashStyle.Scifi:
                    {
                        noiseTexture = tsorcRevamp.NoiseCircuit;
                        break;
                    }
            }


            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseUOffset);
            effect.Parameters["secondaryNoise"].SetValue(noiseTexture);

            const float pixelBlockSize = 2f;
            Vector2 pixelGridSize = new Vector2(
                Math.Max(trailCurrentLength / pixelBlockSize, 1f),
                Math.Max((WidthFunction(0f) * 2f) / pixelBlockSize, 1f));
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y));

            float headFadeStart = 0.75f;

            if (bladeSourceDriven)
            {
                headFadeStart = BladeHeadFadeStart;
            }

            effect.Parameters["HeadFadeStart"].SetValue(headFadeStart);

            visualizeTrail = false;
            float fadeTicks = 15f;

            if (bladeSourceDriven)
            {
                fadeTicks = BladeFadeTicks;
            }

            trailIntensity = MathHelper.Clamp(Projectile.timeLeft / fadeTicks, 0f, 1f);

            effect.Parameters["fadeOut"].SetValue(trailIntensity);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color lightingColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            if (NPCDriven)
            {
                lightingColor = Color.Lerp(lightingColor, Color.White, 0.42f);
            }
            effect.Parameters["slashCenter"].SetValue(Color.White.MultiplyRGB(lightingColor).ToVector4());
            effect.Parameters["slashEdge"].SetValue(slashColor.MultiplyRGB(lightingColor).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }


        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            visualizeTrail = false;

            if (NPCDriven)
            {
                int npcIndex = (int)Projectile.ai[0] - 1;
                NPC sourceNPC = null;

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    sourceNPC = Main.npc[npcIndex];
                }

                if (additiveContext && npcSampleTick != Main.GameUpdateCount)
                {
                    npcSampleTick = Main.GameUpdateCount;
                    IBladeSlashSource source = sourceNPC?.ModNPC as IBladeSlashSource;

                    if (npcTrailAge > Math.Max(1, (int)Projectile.ai[2]))
                    {
                        source = null;
                    }

                    Vector2 center = Projectile.Center;

                    if (sourceNPC != null)
                    {
                        center = sourceNPC.Center;
                    }

                    UpdateBladeSource(center, source);
                }

                if (sourceNPC != null)
                {
                    FollowOwnerPivot(sourceNPC.Center + bladeAnchorOffset);
                }

                return base.PreDraw(ref lightColor);
            }

            // The owner can move after projectile AI; anchor again immediately before rendering. A slash swung by a
            // mirror puppet (Soul Mimic / mirror invader) anchors to the puppet, not the player whose ItemCheck it borrowed.
            Vector2 ownerCenter = NPCs.FriendlyPuppets.MirrorPuppetProjectile.GetOwnerVisualCenter(Projectile);
            if (bladeSourceDriven)
            {
                ownerCenter += bladeAnchorOffset;
            }

            FollowOwnerPivot(ownerCenter);
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}
