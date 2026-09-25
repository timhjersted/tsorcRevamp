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
    public class Slash : DynamicTrail
    {
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
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
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

            // Every radial strip segment must share one pivot. Otherwise, moving the player
            // leaves old inner vertices behind and the joins draw as thin radial wedges.
            FollowOwnerPivot(owner.Center);


            if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aiming))
            {
                //Main.NewText(aiming.AttackId);
                if (AttackId != aiming.AttackId)
                {
                    reachedEnd = true;
                }
            }

            bool flipped = owner.gravDir == -1f && ModContent.GetInstance<tsorcRevampConfig>().GravityFix;

            if (Projectile.timeLeft > 10 && !reachedEnd)
            {
                Projectile.rotation = QuickSlashMeleeAnimation.MeleeSwingRotation(owner, owner.HeldItem, flippedSwing, 1.2f);
                if (owner.gravDir == 1f) Projectile.rotation += MathHelper.PiOver2;
                else if (owner.direction == 1) Projectile.rotation += (float)Math.PI;

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
                Math.Max((trailWidth * 2f) / pixelBlockSize, 1f));
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y));

            visualizeTrail = false;
            if (Projectile.timeLeft < 15)
            {
                trailIntensity = Projectile.timeLeft / 15f;
            }

            effect.Parameters["fadeOut"].SetValue(trailIntensity);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            Color lightingColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            effect.Parameters["slashCenter"].SetValue(Color.White.MultiplyRGB(lightingColor).ToVector4());
            effect.Parameters["slashEdge"].SetValue(slashColor.MultiplyRGB(lightingColor).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }


        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            visualizeTrail = false;
            // The owner can move after projectile AI; anchor again immediately before rendering.
            FollowOwnerPivot(Main.player[Projectile.owner].Center);
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}
