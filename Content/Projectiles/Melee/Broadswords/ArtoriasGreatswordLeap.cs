using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// Artorias Greatsword hold move: a forward-flip leap along an arc to the cursor (up to LeapRangeTiles), sword
    /// raised overhead through the flip, then brought down hard so it lands as the feet do. The landing slams with
    /// the greatsword thud, a small shake and debris, and sends the abyss smoke wall SmokeWallTiles along the floor
    /// each way. The blade deals SlamDamageMultiplier x damage on the way down.
    ///
    /// Sync: ai[0], ai[1] = target floor point (where player.Bottom lands); velocity = (facing, flight ticks) - it
    /// never moves the projectile. localAI[0] = age. Only the owner steers the player (its position reaches
    /// everyone through normal player sync); every client derives the flip, arms and sword pose from age, and the
    /// owner's landing arrives via SendExtraAI.
    /// </summary>
    class ArtoriasGreatswordLeap : ModProjectile
    {
        const float LaunchEnd = 0.12f;      // share of the flight spent raising the sword before the flip starts
        const float FlipEnd = 0.78f;        // flip done; the sword comes down over the rest of the flight
        const float RaisedAngle = -2.4f;    // facing-space sword angle held through the flip (up and behind)
        const float SlamAngle = 0.3f;       // facing-space angle on landing: point just into the floor
        const float LandedRiseAngle = GreatswordMotion.Carry;
        public const int BaseFlightTicks = 30;
        public const float FlightTicksPerTile = 1f;  // 20 tiles flies in 50 ticks
        const float ArcBaseHeight = 72f;
        const float ArcHeightPerPx = 0.2f;
        const int FallTimeoutTicks = 60;
        const int PlantTicks = 6;
        const int RiseTicks = 10;
        const int FloorSearchTiles = 30;
        public const float SlamDamageMultiplier = 2f;
        const float SmokeWallDamageScale = 1f;
        const int SmokeWallArmDelay = 2;

        static readonly SoundStyle SlamSound = new("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
        {
            Volume = 0.5f
        };

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        Vector2 Target => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        int Facing => Projectile.velocity.X < 0f ? -1 : 1;
        int FlightTicks => Math.Max(1, (int)Projectile.velocity.Y);
        float Age => Projectile.localAI[0];

        Vector2 start;
        Vector2 leapTopLeft;
        float arcHeight;
        bool initialized;
        bool landed;
        bool playedLanding;
        int landedAge;
        float fallSpeed;

        Vector2 hand;
        Vector2 tip;
        Vector2 previousTip;
        float drawRotation;
        float drawScale = 1f;
        bool poseReady;
        bool bladeLive;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        /// <summary>The first floor at or below point (platforms count), or point itself over a pit.</summary>
        public static Vector2 FloorBelow(Vector2 point)
        {
            int tileX = (int)(point.X / 16f);
            int tileY = (int)(point.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5)
            {
                return point;
            }

            for (int depth = 0; depth <= FloorSearchTiles; depth++)
            {
                int y = tileY + depth;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                bool solid = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (solid)
                {
                    return new Vector2(point.X, y * 16f);
                }
            }

            return point;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanCutTiles() => false;

        public override bool? CanDamage()
        {
            if (!bladeLive)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            bool isOwner = Projectile.owner == Main.myPlayer;
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            if (isOwner)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                bool wrongItem = player.HeldItem.ModItem is not ArtoriasGreatsword;
                if (wrongItem || player.noItems || player.CCed || player.mount.Active || modPlayer.isDodging)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (!(player.HeldItem.ModItem is Greatsword weapon))
            {
                weapon = ContentSamples.ItemsByType[ModContent.ItemType<ArtoriasGreatsword>()].ModItem as Greatsword;
            }

            Projectile.localAI[0]++;
            Projectile.timeLeft = 2;
            Projectile.Center = player.MountedCenter;
            player.heldProj = Projectile.whoAmI;
            player.ChangeDir(Facing);

            if (isOwner)
            {
                SteerPlayer(player);
            }

            if (landed && !playedLanding)
            {
                PlayLanding(player, isOwner);
            }

            // ---- pose: flip, sword angle, arms ----
            float progress = MathHelper.Clamp(Age / FlightTicks, 0f, 1f);
            float flip = 0f;
            if (!landed)
            {
                float flipProgress = MathHelper.Clamp((progress - LaunchEnd) / (FlipEnd - LaunchEnd), 0f, 1f);
                flip = MathHelper.SmoothStep(0f, 1f, flipProgress);
            }

            // Forward flip: clockwise facing right, mirrored facing left.
            player.fullRotation = Facing * MathHelper.TwoPi * flip;
            player.fullRotationOrigin = player.Size * 0.5f;

            float angle = SwordAngle(progress);
            Vector2 axis = new Vector2(MathF.Cos(angle) * Facing, MathF.Sin(angle));
            float localWorldAngle = axis.ToRotation();
            float armRotation = localWorldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Vector2 localHand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);

            drawScale = player.GetAdjustedItemScale(weapon.Item);
            if (player.gravDir == 1f)
            {
                float handleLength = Vector2.Distance(weapon.Grip, weapon.HiltTop) * drawScale;
                float backArmRotation = GreatswordMotion.BackArmRotation(localHand - player.MountedCenter, axis, Facing, handleLength, out _);
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);
            }

            // The player draws rotated by fullRotation about fullRotationOrigin, so the sword must follow that turn.
            Vector2 rotationCenter = player.position + player.fullRotationOrigin;
            hand = rotationCenter + (localHand - rotationCenter).RotatedBy(player.fullRotation);
            float bladeWorldAngle = localWorldAngle + player.fullRotation;

            Vector2 bladeAxis = weapon.BladeTip - weapon.Grip;
            if (Facing < 0)
            {
                bladeAxis.Y = -bladeAxis.Y;
            }

            drawRotation = bladeWorldAngle - bladeAxis.ToRotation();
            previousTip = tip;
            tip = hand + bladeWorldAngle.ToRotationVector2() * (weapon.BladeTip - weapon.Grip).Length() * drawScale;
            if (!poseReady)
            {
                previousTip = tip;
            }

            poseReady = true;

            // Blade hits on the way down and for the first ticks of the landing.
            bool descending = !landed && progress >= FlipEnd;
            bool justLanded = landed && Age - landedAge <= 3;
            bladeLive = descending || justLanded;

            if (!Main.dedServ && !landed && progress > LaunchEnd && Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustPerfect(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), DustID.ShadowbeamStaff, Vector2.Zero, 120, new Color(150, 60, 230), Main.rand.NextFloat(0.8f, 1.2f));
                trail.noGravity = true;
            }

            if (isOwner && landed && Age - landedAge >= PlantTicks + RiseTicks)
            {
                Projectile.Kill();
            }
        }

        /// <summary>Owner only. Moves the player along the arc with tile collision, and detects the landing.</summary>
        void SteerPlayer(Player player)
        {
            if (!initialized)
            {
                initialized = true;
                start = player.Bottom;
                leapTopLeft = player.position;
                float rise = Math.Max(0f, start.Y - Target.Y);
                arcHeight = ArcBaseHeight + Math.Abs(Target.X - start.X) * ArcHeightPerPx + rise * 0.5f;
                player.RemoveAllGrapplingHooks();
            }

            // No fall damage from the leap, wherever it ends.
            player.fallStart = (int)(player.position.Y / 16f);

            if (landed)
            {
                player.velocity = Vector2.Zero;
                return;
            }

            Vector2 desiredTopLeft;
            bool inFlight = Age <= FlightTicks;
            if (inFlight)
            {
                float progress = Age / FlightTicks;
                float lift = arcHeight * 4f * progress * (1f - progress);
                Vector2 desiredBottom = Vector2.Lerp(start, Target, progress) - new Vector2(0f, lift);
                desiredTopLeft = desiredBottom - new Vector2(player.width * 0.5f, player.height);
            }
            else
            {
                // Arc over and still airborne (target over a pit, or the floor moved): drop straight down.
                fallSpeed = Math.Min(16f, fallSpeed + 0.6f);
                desiredTopLeft = leapTopLeft + new Vector2(0f, Math.Max(6f, fallSpeed));
            }

            Vector2 delta = desiredTopLeft - leapTopLeft;
            Vector2 moved = Collision.TileCollision(leapTopLeft, delta, player.width, player.height, false, false, (int)player.gravDir);
            leapTopLeft += moved;
            player.position = leapTopLeft;
            player.velocity = moved;

            // Landing: the feet were stopped on the way down past the top of the arc, or we arrived standing on ground.
            bool pastApex = Age > FlightTicks * 0.5f;
            bool stoppedFalling = delta.Y > 0.01f && moved.Y < delta.Y - 0.01f;
            bool arrivedOnGround = !inFlight || Age == FlightTicks;
            bool groundUnderFeet = Collision.SolidCollision(player.BottomLeft, player.width, 4, true);
            bool timedOut = Age > FlightTicks + FallTimeoutTicks;

            if ((pastApex && stoppedFalling) || (arrivedOnGround && groundUnderFeet) || timedOut)
            {
                landed = true;
                landedAge = (int)Age;
                player.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
        }

        float SwordAngle(float progress)
        {
            if (landed)
            {
                // Planted, then a short rise back to the carry pose.
                float sinceLanding = Age - landedAge;
                if (sinceLanding < PlantTicks)
                {
                    return SlamAngle;
                }

                float riseProgress = (sinceLanding - PlantTicks) / RiseTicks;
                return MathHelper.SmoothStep(SlamAngle, LandedRiseAngle, MathHelper.Clamp(riseProgress, 0f, 1f));
            }

            if (progress < LaunchEnd)
            {
                return MathHelper.SmoothStep(GreatswordMotion.Carry, RaisedAngle, progress / LaunchEnd);
            }

            if (progress < FlipEnd)
            {
                return RaisedAngle;
            }

            // The chop accelerates all the way into the landing (speed grows linearly; travel goes as its square).
            float chop = (progress - FlipEnd) / (1f - FlipEnd);
            return MathHelper.Lerp(RaisedAngle, SlamAngle, chop * chop);
        }

        void PlayLanding(Player player, bool isOwner)
        {
            playedLanding = true;
            player.fullRotation = 0f;
            Vector2 impact = player.Bottom;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SlamSound, impact);
                UsefulFunctions.ScreenShake(impact, 3.5f, 10, distanceFalloff: 440f);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.4f, 3.4f), Main.rand.NextFloat(-3.8f, -0.9f));
                    Dust.NewDustPerfect(impact, DustID.Stone, velocity, Scale: Main.rand.NextFloat(0.8f, 1.2f));
                }

                for (int i = 0; i < 10; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-4.6f, 4.6f), Main.rand.NextFloat(-2.8f, -0.6f));
                    Dust spark = Dust.NewDustPerfect(impact, DustID.ShadowbeamStaff, velocity, 100, new Color(150, 60, 230), Main.rand.NextFloat(0.9f, 1.3f));
                    spark.noGravity = true;
                }

                for (int i = 1; i <= 3; i++)
                {
                    Vector2 cloudVelocity = new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-4.5f, -1.5f));
                    Vector2 cloudPosition = impact + new Vector2(Main.rand.NextFloat(-40f, 40f), -10f);
                    Gore.NewGore(Projectile.GetSource_FromThis(), cloudPosition, cloudVelocity,
                        ModContent.Find<ModGore>($"tsorcRevamp/ChaosImpactCloud{i}").Type, Main.rand.NextFloat(0.9f, 1.3f));
                }
            }

            if (isOwner)
            {
                // Smoke wall both ways along the floor from the landing.
                Vector2 wavePosition = new Vector2(player.Center.X, player.Bottom.Y - 28f);
                int waveDamage = (int)(Projectile.damage * SmokeWallDamageScale);
                float runPixels = ArtoriasGreatsword.SmokeWallTiles * 16f;
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), wavePosition, Vector2.Zero,
                        ModContent.ProjectileType<ArtoriasGreatswordSmokeWave>(), waveDamage, Projectile.knockBack * 0.6f,
                        Projectile.owner, direction, SmokeWallArmDelay, runPixels);
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= SlamDamageMultiplier;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!poseReady || !bladeLive)
            {
                return false;
            }

            // The blade line, swept between this tick's and last tick's tip so the fast chop cannot skip a target.
            float collisionPoint = 0f;
            for (int i = 0; i <= 4; i++)
            {
                Vector2 sweptTip = Vector2.Lerp(previousTip, tip, i / 4f);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), hand, sweptTip, 14f, ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.fullRotation = 0f;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(landed);
            writer.Write((short)landedAge);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            landed = reader.ReadBoolean();
            landedAge = reader.ReadInt16();
            if (landed)
            {
                Projectile.localAI[0] = Math.Max(Projectile.localAI[0], landedAge);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!poseReady)
            {
                return false;
            }

            int itemType = ModContent.ItemType<ArtoriasGreatsword>();
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Greatsword weapon = ContentSamples.ItemsByType[itemType].ModItem as Greatsword;
            SpriteEffects effects = SpriteEffects.None;
            Vector2 origin = weapon.Grip;
            if (Facing < 0)
            {
                effects = SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
            }

            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, lightColor, drawRotation, origin, drawScale, effects);
            return false;
        }
    }
}
