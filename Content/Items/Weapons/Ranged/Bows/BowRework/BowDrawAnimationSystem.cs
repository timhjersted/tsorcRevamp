using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Bows.BowRework
{
    public struct BowSpriteMetrics
    {
        public bool IsVerticalBow;
        public Vector2 TopTipTexel;
        public Vector2 BottomTipTexel;
        public Vector2 GripTexel;
        public Rectangle StringCutoutRect;
        public Color StringColor;
    }

    public sealed class BowDrawAnimationPlayer : ModPlayer
    {
        // Vanilla shoots on tick 0 of each cycle, so release flick plays right after firing (0..ReleasePhaseEnd)
        // and the next arrow pulls to full draw (1.0) on the exact tick before the next shot fires.
        private const float ReleaseFlickEnd = 0.10f;
        private const float ReleasePhaseEnd = 0.26f;
        private const float FullPullPixels = 10f;

        private static readonly Dictionary<int, BowSpriteMetrics> MetricsCache = new Dictionary<int, BowSpriteMetrics>();

        public bool ActiveStandardBowDraw;
        public bool IsReleasing;
        public float PullProgress;
        public float ReleaseProgress;
        public Vector2 AimDirection = Vector2.UnitX;
        public int ArrowProjectileType = ProjectileID.WoodenArrowFriendly;
        public Player.CompositeArmStretchAmount BackArmStretch = Player.CompositeArmStretchAmount.Full;
        public float BackArmRotation;
        public Player.CompositeArmStretchAmount FrontArmStretch = Player.CompositeArmStretchAmount.Full;
        public float FrontArmRotation;

        public static bool IsStandardAnimatedBow(Item item)
        {
            if (item == null || item.IsAir)
            {
                return false;
            }

            if (item.useAmmo != AmmoID.Arrow || item.useStyle != ItemUseStyleID.Shoot || item.noUseGraphic)
            {
                return false;
            }

            if (item.type == ItemID.CobaltRepeater
                || item.type == ItemID.MythrilRepeater
                || item.type == ItemID.AdamantiteRepeater
                || item.type == ItemID.HallowedRepeater
                || item.type == ItemID.PalladiumRepeater
                || item.type == ItemID.OrichalcumRepeater
                || item.type == ItemID.TitaniumRepeater
                || item.type == ItemID.ChlorophyteShotbow
                || item.type == ModContent.ItemType<Gastraphetes>()
                || item.type == ModContent.ItemType<PhoenixOverdrive>())
            {
                return false;
            }

            return true;
        }

        public static BowSpriteMetrics GetOrComputeBowMetrics(int itemType, Texture2D bowTexture)
        {
            if (MetricsCache.TryGetValue(itemType, out BowSpriteMetrics cached))
            {
                return cached;
            }

            int width = bowTexture.Width;
            int height = bowTexture.Height;
            BowSpriteMetrics metrics = new BowSpriteMetrics
            {
                IsVerticalBow = height > width,
                TopTipTexel = new Vector2(2f, 3f),
                BottomTipTexel = new Vector2(2f, height - 3f),
                GripTexel = new Vector2(width * 0.7f, height * 0.5f),
                StringCutoutRect = Rectangle.Empty,
                StringColor = new Color(215, 205, 185, 230)
            };

            if (!metrics.IsVerticalBow || width <= 0 || height <= 0)
            {
                MetricsCache[itemType] = metrics;
                return metrics;
            }

            Color[] pixels = new Color[width * height];
            bowTexture.GetData(pixels);

            int midY = height / 2;
            int stringX = -1;
            int riserFrontX = -1;

            for (int x = 0; x < width; x++)
            {
                if (pixels[(midY * width) + x].A > 15)
                {
                    if (stringX < 0)
                    {
                        stringX = x;
                    }

                    riserFrontX = x;
                }
            }

            if (stringX >= 0 && riserFrontX > stringX)
            {
                int topY = midY;
                for (int y = midY; y >= 0; y--)
                {
                    bool hasStringPixel = false;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int sampleX = Math.Clamp(stringX + dx, 0, width - 1);
                        if (pixels[(y * width) + sampleX].A > 15)
                        {
                            hasStringPixel = true;
                            break;
                        }
                    }

                    if (hasStringPixel)
                    {
                        topY = y;
                    }
                    else
                    {
                        break;
                    }
                }

                int bottomY = midY;
                for (int y = midY; y < height; y++)
                {
                    bool hasStringPixel = false;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int sampleX = Math.Clamp(stringX + dx, 0, width - 1);
                        if (pixels[(y * width) + sampleX].A > 15)
                        {
                            hasStringPixel = true;
                            break;
                        }
                    }

                    if (hasStringPixel)
                    {
                        bottomY = y;
                    }
                    else
                    {
                        break;
                    }
                }

                Color midStringColor = pixels[(midY * width) + stringX];
                if (midStringColor.A > 40)
                {
                    metrics.StringColor = midStringColor;
                }

                metrics.TopTipTexel = new Vector2(stringX + 0.5f, topY + 1f);
                metrics.BottomTipTexel = new Vector2(stringX + 0.5f, Math.Max(topY + 2f, bottomY - 1f));
                metrics.GripTexel = new Vector2(MathHelper.Lerp(stringX, riserFrontX, 0.72f), height * 0.5f);

                int cutoutTop = topY + 2;
                int cutoutHeight = (bottomY - 1) - cutoutTop;
                if (cutoutHeight > 4)
                {
                    metrics.StringCutoutRect = new Rectangle(Math.Max(0, stringX), cutoutTop, Math.Min(2, width - stringX), cutoutHeight);
                }
            }

            MetricsCache[itemType] = metrics;
            return metrics;
        }

        // Shared Ulhan-style two-arm bow pose used by both standard bows and ChargedBowHeld projectiles.
        public static void ApplyBowArmPose(
            Player player,
            Vector2 aimVector,
            float pullProgress,
            bool isReleasing,
            float releaseProgress,
            out Player.CompositeArmStretchAmount appliedBackStretch,
            out float appliedBackRotation,
            out Player.CompositeArmStretchAmount appliedFrontStretch,
            out float appliedFrontRotation)
        {
            int facing = 1;
            if (player.direction < 0)
            {
                facing = -1;
            }

            Vector2 fallbackAim = new Vector2(facing, 0f);
            Vector2 aimDir = aimVector.SafeNormalize(fallbackAim);
            float aimAngle = aimDir.ToRotation();

            float backArmRotation = aimAngle - MathHelper.PiOver2;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);
            appliedBackStretch = Player.CompositeArmStretchAmount.Full;
            appliedBackRotation = backArmRotation;

            Vector2 shoulderWorld = player.MountedCenter;
            Vector2 bowHandWorld = player.GetBackHandPosition(appliedBackStretch, appliedBackRotation);
            Vector2 restStringWorld = bowHandWorld - (aimDir * 3f);
            Vector2 drawnStringWorld = restStringWorld - (aimDir * (FullPullPixels * MathHelper.Clamp(pullProgress, 0f, 1f)));
            Vector2 targetHandWorld = drawnStringWorld;

            if (isReleasing)
            {
                Vector2 flickWorld = restStringWorld - (aimDir * (FullPullPixels + 2f)) - new Vector2(0f, 2f);
                if (releaseProgress < 0.35f)
                {
                    targetHandWorld = flickWorld;
                }
                else
                {
                    float returnProgress = (releaseProgress - 0.35f) / 0.65f;
                    float easedReturn = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(returnProgress, 0f, 1f));
                    targetHandWorld = Vector2.Lerp(flickWorld, bowHandWorld, easedReturn);
                }
            }

            Vector2 fromShoulder = targetHandWorld - shoulderWorld;
            float frontArmRotation = fromShoulder.ToRotation() - MathHelper.PiOver2;
            float reach = fromShoulder.Length();
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

            if (reach < 6.5f)
            {
                stretch = Player.CompositeArmStretchAmount.None;
            }
            else if (reach < 8.5f)
            {
                stretch = Player.CompositeArmStretchAmount.Quarter;
            }
            else if (reach < 10.5f)
            {
                stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            }

            player.SetCompositeArmFront(true, stretch, frontArmRotation);
            appliedFrontStretch = stretch;
            appliedFrontRotation = frontArmRotation;
        }

        public override void PostItemCheck()
        {
            UpdateStandardBowPose();
        }

        private void UpdateStandardBowPose()
        {
            Item heldItem = Player.HeldItem;
            if (!Player.ItemAnimationActive || !IsStandardAnimatedBow(heldItem))
            {
                ActiveStandardBowDraw = false;
                return;
            }

            ActiveStandardBowDraw = true;

            int cycleMax = Player.itemAnimationMax;
            int cycleRemaining = Player.itemAnimation;
            if (Player.itemTimeMax > 0 && Player.itemTime > 0)
            {
                cycleMax = Player.itemTimeMax;
                cycleRemaining = Player.itemTime;
            }

            // Map cycleRemaining from [cycleMax..1] to [0..1] so cycleProgress == 0 on the fire tick
            // and cycleProgress == 1 on the exact tick before the next shot fires.
            float cycleSpan = Math.Max(1, cycleMax - 1);
            float cycleProgress = (cycleMax - cycleRemaining) / cycleSpan;
            cycleProgress = MathHelper.Clamp(cycleProgress, 0f, 1f);

            bool preparingNextShot = Player.controlUseItem && (heldItem.autoReuse || Player.itemAnimation > Player.itemTime);

            if (cycleProgress < ReleasePhaseEnd)
            {
                IsReleasing = true;
                PullProgress = 0f;

                if (cycleProgress < ReleaseFlickEnd)
                {
                    ReleaseProgress = 0.1f;
                }
                else
                {
                    float returnNorm = (cycleProgress - ReleaseFlickEnd) / (ReleasePhaseEnd - ReleaseFlickEnd);
                    ReleaseProgress = 0.35f + (MathHelper.Clamp(returnNorm, 0f, 1f) * 0.65f);
                }
            }
            else if (preparingNextShot)
            {
                IsReleasing = false;
                ReleaseProgress = 0f;
                float drawNorm = (cycleProgress - ReleasePhaseEnd) / (1f - ReleasePhaseEnd);
                PullProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(drawNorm, 0f, 1f));
            }
            else
            {
                // Single-click shot finished releasing: rest hand on bow with no phantom nocked arrow.
                IsReleasing = true;
                PullProgress = 0f;
                ReleaseProgress = 1f;
            }

            int facing = 1;
            if (Player.direction < 0)
            {
                facing = -1;
            }

            float worldAimAngle = Player.itemRotation;
            if (facing < 0)
            {
                worldAimAngle += MathHelper.Pi;
            }

            AimDirection = worldAimAngle.ToRotationVector2();
            ArrowProjectileType = ResolveArrowProjectileType(heldItem);

            ApplyBowArmPose(
                Player,
                AimDirection,
                PullProgress,
                IsReleasing,
                ReleaseProgress,
                out BackArmStretch,
                out BackArmRotation,
                out FrontArmStretch,
                out FrontArmRotation);

            // Sky-rain bows (Blood Rain Bow, Daedalus Stormbow) summon projectiles from above instead of
            // launching a physical arrow from the bowstring; gather themed dusts at the nock instead.
            if (!IsReleasing && !Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                if (heldItem.type == ItemID.BloodRainBow)
                {
                    Vector2 nockWorld = Player.GetFrontHandPosition(FrontArmStretch, FrontArmRotation);
                    Vector2 bloodVel = (-AimDirection * Main.rand.NextFloat(0.4f, 1.4f)) + Main.rand.NextVector2Circular(0.6f, 0.6f);
                    Dust droplet = Dust.NewDustPerfect(nockWorld + Main.rand.NextVector2Circular(3f, 3f), DustID.Blood, bloodVel, 40, default, Main.rand.NextFloat(0.9f, 1.25f));
                    droplet.noGravity = true;

                    if (Main.rand.NextBool(2))
                    {
                        Dust glint = Dust.NewDustPerfect(nockWorld + Main.rand.NextVector2Circular(2f, 2f), DustID.CrimsonTorch, bloodVel * 0.6f, 80, default, 0.85f);
                        glint.noGravity = true;
                    }
                }
                else if (heldItem.type == ItemID.DaedalusStormbow)
                {
                    Vector2 nockWorld = Player.GetFrontHandPosition(FrontArmStretch, FrontArmRotation);
                    Vector2 sparkleVel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1.8f, -0.5f));
                    int dustType = Main.rand.NextBool(2) ? DustID.Enchanted_Gold : DustID.HallowedWeapons;
                    Dust sparkle = Dust.NewDustPerfect(nockWorld + Main.rand.NextVector2Circular(4f, 4f), dustType, sparkleVel, 60, default, Main.rand.NextFloat(0.75f, 1.1f));
                    sparkle.noGravity = true;
                }
                else if (heldItem.type == ModContent.ItemType<ForgottenThunderBow>())
                {
                    Vector2 nockWorld = Player.GetFrontHandPosition(FrontArmStretch, FrontArmRotation);
                    Vector2 sparkVel = (-AimDirection * Main.rand.NextFloat(0.3f, 1.2f)) + Main.rand.NextVector2Circular(0.9f, 0.9f);
                    int dustType = Main.rand.NextBool(2) ? DustID.GoldFlame : DustID.YellowTorch;
                    Dust spark = Dust.NewDustPerfect(nockWorld + Main.rand.NextVector2Circular(3f, 3f), dustType, sparkVel, 40, default, Main.rand.NextFloat(0.85f, 1.15f));
                    spark.noGravity = true;
                }
            }
        }

        internal int ResolveArrowProjectileType(Item bowItem)
        {
            int ammoProj = ProjectileID.WoodenArrowFriendly;
            if (bowItem.shoot > ProjectileID.None && bowItem.shoot < TextureAssets.Projectile.Length)
            {
                ammoProj = bowItem.shoot;
            }

            bool foundAmmo = false;
            for (int i = 54; i < 58; i++)
            {
                Item ammoItem = Player.inventory[i];
                if (ammoItem != null && !ammoItem.IsAir && ammoItem.ammo == AmmoID.Arrow && ammoItem.shoot > ProjectileID.None)
                {
                    ammoProj = ammoItem.shoot;
                    foundAmmo = true;
                    break;
                }
            }

            if (!foundAmmo)
            {
                for (int i = 0; i < 54; i++)
                {
                    Item ammoItem = Player.inventory[i];
                    if (ammoItem != null && !ammoItem.IsAir && ammoItem.ammo == AmmoID.Arrow && ammoItem.shoot > ProjectileID.None)
                    {
                        ammoProj = ammoItem.shoot;
                        break;
                    }
                }
            }

            // Per-bow visual ammo conversions (matching what each bow actually fires; non-arrow-shaped casters like ForgottenThunderBow keep ammoProj).
            if (bowItem.type == ItemID.PulseBow)
            {
                return ProjectileID.PulseBolt;
            }

            if (bowItem.type == ItemID.IceBow)
            {
                return ProjectileID.FrostArrow;
            }

            if (bowItem.type == ItemID.ShadowFlameBow)
            {
                return ProjectileID.ShadowFlameArrow;
            }

            if (bowItem.type == ItemID.Marrow)
            {
                return ProjectileID.BoneArrow;
            }

            if (bowItem.type == ItemID.HellwingBow)
            {
                return ProjectileID.Hellwing;
            }

            if (ammoProj == ProjectileID.WoodenArrowFriendly)
            {
                if (bowItem.type == ItemID.MoltenFury)
                {
                    return ProjectileID.FireArrow;
                }

                if (bowItem.type == ItemID.BeesKnees)
                {
                    return ProjectileID.BeeArrow;
                }
            }

            return ammoProj;
        }
    }

    public sealed class BowStringAndArrowDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.HeldItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f)
            {
                return false;
            }

            Player drawPlayer = drawInfo.drawPlayer;
            if (drawPlayer == null || !drawPlayer.active || drawPlayer.dead)
            {
                return false;
            }

            BowDrawAnimationPlayer bowPlayer = drawPlayer.GetModPlayer<BowDrawAnimationPlayer>();
            return bowPlayer.ActiveStandardBowDraw && BowDrawAnimationPlayer.IsStandardAnimatedBow(drawPlayer.HeldItem);
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            BowDrawAnimationPlayer bowPlayer = drawPlayer.GetModPlayer<BowDrawAnimationPlayer>();
            Item heldItem = drawPlayer.HeldItem;

            Main.instance.LoadItem(heldItem.type);
            Texture2D bowTexture = TextureAssets.Item[heldItem.type].Value;
            if (bowTexture == null)
            {
                return;
            }

            BowSpriteMetrics metrics = BowDrawAnimationPlayer.GetOrComputeBowMetrics(heldItem.type, bowTexture);
            if (!metrics.IsVerticalBow)
            {
                return;
            }

            // Remove vanilla HeldItem's unaligned draw so we can anchor the bow riser directly to the extended back hand.
            Color bowDrawColor = Lighting.GetColor(drawPlayer.Center.ToTileCoordinates());
            for (int i = drawInfo.DrawDataCache.Count - 1; i >= 0; i--)
            {
                if (drawInfo.DrawDataCache[i].texture == bowTexture)
                {
                    bowDrawColor = drawInfo.DrawDataCache[i].color;
                    drawInfo.DrawDataCache.RemoveAt(i);
                    break;
                }
            }

            int facing = 1;
            if (drawPlayer.direction < 0)
            {
                facing = -1;
            }

            Vector2 aimDir = bowPlayer.AimDirection.SafeNormalize(new Vector2(facing, 0f));
            float aimAngle = aimDir.ToRotation();
            float itemScale = drawPlayer.GetAdjustedItemScale(heldItem);

            Vector2 bowHandWorld = drawPlayer.GetBackHandPosition(bowPlayer.BackArmStretch, bowPlayer.BackArmRotation);
            bowHandWorld.Y += drawPlayer.gfxOffY;

            // Wide/deep bow sprites (like Forgotten Thunder Bow) have their string far behind the riser;
            // shift the bow grip slightly forward so the unpulled string stays in front of the torso.
            float bowDepth = Math.Max(0f, (metrics.GripTexel.X - metrics.TopTipTexel.X) * itemScale);
            float extraBowHoldout = Math.Max(0f, bowDepth - 8f);
            Vector2 bowHandScreen = bowHandWorld + (aimDir * extraBowHoldout) - Main.screenPosition;

            float bowDrawRotation = aimAngle;
            Vector2 bowDrawOrigin = metrics.GripTexel;
            SpriteEffects bowSpriteEffects = SpriteEffects.None;

            if (facing < 0)
            {
                bowDrawRotation = aimAngle + MathHelper.Pi;
                bowDrawOrigin = new Vector2(bowTexture.Width - metrics.GripTexel.X, metrics.GripTexel.Y);
                bowSpriteEffects = SpriteEffects.FlipHorizontally;
            }

            DrawData bowDrawData = new DrawData(
                bowTexture,
                bowHandScreen,
                bowTexture.Bounds,
                bowDrawColor,
                bowDrawRotation,
                bowDrawOrigin,
                itemScale,
                bowSpriteEffects,
                0);

            if (metrics.StringCutoutRect != Rectangle.Empty)
            {
                PuppetNPC.AddBowLimbDraws(drawInfo.DrawDataCache, bowDrawData, bowTexture.Bounds, metrics.StringCutoutRect);
            }
            else
            {
                drawInfo.DrawDataCache.Add(bowDrawData);
            }

            // Transform the auto-detected top and bottom limb tips using the exact same bow hand pivot and rotation.
            Vector2 topLocalOffset = (metrics.TopTipTexel - metrics.GripTexel) * itemScale;
            topLocalOffset.Y *= facing;
            Vector2 topTipScreen = bowHandScreen + topLocalOffset.RotatedBy(aimAngle);

            Vector2 bottomLocalOffset = (metrics.BottomTipTexel - metrics.GripTexel) * itemScale;
            bottomLocalOffset.Y *= facing;
            Vector2 bottomTipScreen = bowHandScreen + bottomLocalOffset.RotatedBy(aimAngle);

            Vector2 restStringScreen = (topTipScreen + bottomTipScreen) * 0.5f;
            Vector2 stringPointScreen = restStringScreen;

            if (!bowPlayer.IsReleasing)
            {
                Vector2 frontHandWorld = drawPlayer.GetFrontHandPosition(bowPlayer.FrontArmStretch, bowPlayer.FrontArmRotation);
                frontHandWorld.Y += drawPlayer.gfxOffY;
                Vector2 handStringScreen = frontHandWorld - Main.screenPosition;

                // Ensure the string pulls back behind its resting line even on wide bows whose string sits far back.
                float handPullBehindRest = Math.Max(0f, Vector2.Dot(restStringScreen - handStringScreen, aimDir));
                float minPullBehindRest = bowPlayer.PullProgress * 10f;
                float actualPull = Math.Max(handPullBehindRest, minPullBehindRest);
                stringPointScreen = restStringScreen - (aimDir * actualPull);
            }

            Color stringColor = metrics.StringColor.MultiplyRGBA(bowDrawColor);
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelRect = new Rectangle(0, 0, 1, 1);

            Vector2 topSpan = stringPointScreen - topTipScreen;
            drawInfo.DrawDataCache.Add(new DrawData(
                magicPixel,
                topTipScreen,
                pixelRect,
                stringColor,
                topSpan.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(topSpan.Length(), 1.5f),
                SpriteEffects.None,
                0));

            Vector2 bottomSpan = stringPointScreen - bottomTipScreen;
            drawInfo.DrawDataCache.Add(new DrawData(
                magicPixel,
                bottomTipScreen,
                pixelRect,
                stringColor,
                bottomSpan.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(bottomSpan.Length(), 1.5f),
                SpriteEffects.None,
                0));

            if (!bowPlayer.IsReleasing
                && heldItem.type != ItemID.BloodRainBow
                && heldItem.type != ItemID.DaedalusStormbow
                && heldItem.type != ModContent.ItemType<ForgottenThunderBow>())
            {
                int arrowProjType = bowPlayer.ArrowProjectileType;
                if (arrowProjType > ProjectileID.None && arrowProjType < TextureAssets.Projectile.Length)
                {
                    Main.instance.LoadProjectile(arrowProjType);
                    Texture2D arrowTexture = TextureAssets.Projectile[arrowProjType].Value;
                    if (arrowTexture != null)
                    {
                        int arrowFrames = Math.Max(1, Main.projFrames[arrowProjType]);
                        int arrowFrameHeight = arrowTexture.Height / arrowFrames;
                        Rectangle arrowSourceRect = new Rectangle(0, 0, arrowTexture.Width, arrowFrameHeight);
                        Vector2 arrowOrigin = new Vector2(arrowTexture.Width * 0.5f, arrowFrameHeight - 2f);
                        float arrowRotation = aimAngle + MathHelper.PiOver2;
                        SpriteEffects arrowEffects = SpriteEffects.None;
                        Color arrowColor = bowDrawColor;

                        if (arrowProjType == ProjectileID.Hellwing)
                        {
                            // Hellwing bat sprite faces right (+X), not up (-Y) like standard arrows.
                            arrowOrigin = new Vector2(arrowTexture.Width * 0.5f, arrowFrameHeight * 0.5f);
                            arrowRotation = aimAngle;
                            arrowColor = Color.White;
                            if (facing < 0)
                            {
                                arrowRotation = aimAngle + MathHelper.Pi;
                                arrowEffects = SpriteEffects.FlipHorizontally;
                            }
                        }
                        else if (arrowProjType == ProjectileID.PulseBolt)
                        {
                            arrowColor = Color.White;
                        }

                        drawInfo.DrawDataCache.Add(new DrawData(
                            arrowTexture,
                            stringPointScreen,
                            arrowSourceRect,
                            arrowColor,
                            arrowRotation,
                            arrowOrigin,
                            1f,
                            arrowEffects,
                            0));
                    }
                }
            }
        }
    }
}
