using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// The Sword of Lord Gwyn's rising flame dash (right click). Held like the left-click combo: the front arm
    /// straight out along the blade (vanilla composite arm), the rear hand on the handle, the sword drawn at the
    /// same scale with its grip point in the fist - geometry read from the SwordOfLordGwyn Greatsword.
    /// ai[0] = 1 (dash; the old ground slash mode is gone), ai[1] = facing, ai[2] = 1 when empowered.
    /// </summary>
    class SwordOfLordGwynSlash : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SwordOfLordGwyn));

        const float BladeWidth = 32f;
        const int DashSwingLifetime = 28;
        const int DashLifetime = DashSwingLifetime + 10;
        const int DashBrakeStartTick = 8;
        const float HeldDashExitSpeed = 7.5f;
        const float CinderSpacing = 32f;
        const float CinderEndCapDistance = 8f;

        bool DashSlash => Projectile.ai[0] == 1f;
        bool EmpoweredDash => Projectile.ai[2] == 1f;
        int Direction => Projectile.ai[1] >= 0f ? 1 : -1;
        int Lifetime => DashSlash ? DashLifetime : 22;
        int AnimationLifetime => DashSlash ? DashSwingLifetime : Lifetime;
        float Timer => Projectile.localAI[0];

        bool cinderTrailInitialized;
        float cinderTrailY;
        float lastCinderX;

        // This tick's pose, set in AI: fist, blade tip, draw rotation and scale.
        Vector2 hand;
        Vector2 tip;
        float drawRotation;
        float drawScale = 1f;
        bool poseReady;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = DashLifetime;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.localAI[0]++;
            if (DashSlash && !cinderTrailInitialized)
            {
                InitializeCinderTrail();
            }
            if (Timer == 1f && Main.myPlayer == Projectile.owner)
            {
                // Broadsword-rework slash trail (colored by the item's slashColor). ai[0] = this swing's identity + 1
                // tells the trail to follow our blade angle instead of the held item's useAnimation, which this
                // projectile-driven swing never advances.
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.VFX.Slash>(), 0, 0f, Projectile.owner, Projectile.identity + 1);
            }

            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, Lifetime - (int)Timer);
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, false, false);
            Projectile.rotation = CurrentAngle();
            Projectile.spriteDirection = Direction;
            player.heldProj = Projectile.whoAmI;

            player.itemTime = 2;
            player.itemAnimation = 2;
            player.ChangeDir(Direction);
            UpdatePose(player);

            if (DashSlash)
            {
                player.ResetMeleeHitCooldowns();
                ApplyDashBraking(player);
                UpdateCinderTrail(player);
            }

            Vector2 start = hand;
            Vector2 end = tip;
            for (int i = 0; i < (DashSlash ? 8 : 5); i++)
            {
                Vector2 pos = Vector2.Lerp(start, end, Main.rand.NextFloat());
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos - new Vector2(3f), 6, 6, type, player.velocity.X * 0.1f, player.velocity.Y * 0.1f, 60, default, DashSlash ? 1.9f : 1.35f);
                Main.dust[dust].noGravity = true;
            }

            if (DashSlash && Timer <= AnimationLifetime)
            {
                // Rolling FireBreath flames peel off the outer blade and hang in the air behind the dash.
                Vector2 bladeNormal = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 flamePosition = Vector2.Lerp(start, end, Main.rand.NextFloat(0.35f, 1f));
                    Vector2 flameVelocity = player.velocity * 0.15f + bladeNormal * Main.rand.NextFloat(-1.2f, 1.2f);
                    float flameScale = Main.rand.NextFloat(0.55f, 0.9f);
                    int flameLife = Main.rand.Next(18, 27);
                    global::tsorcRevamp.VFX.VFX.FlameLobe(flamePosition, flameVelocity, flameScale, flameLife);
                }
            }

            Lighting.AddLight(Projectile.Center, 0.8f, 0.42f, 0.12f);

            if (Timer >= Lifetime)
            {
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!poseReady)
            {
                return false;
            }

            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), hand, tip, BladeWidth, ref point);
        }

        /// <summary>
        /// Arms and blade for this tick, the same rig as GreatswordSwing: composite front arm along the blade
        /// (0 = arm down, so world angle - pi/2), rear hand solved onto the handle, sprite rotated so the grip-to-tip
        /// line matches the blade angle and flipped vertically when facing left.
        /// </summary>
        void UpdatePose(Player player)
        {
            Greatsword sword = player.HeldItem.ModItem as Greatsword;
            if (sword == null)
            {
                sword = ContentSamples.ItemsByType[ModContent.ItemType<SwordOfLordGwyn>()].ModItem as Greatsword;
            }

            float worldAngle = CurrentAngle();
            Vector2 axis = worldAngle.ToRotationVector2();
            float armRotation = worldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);

            drawScale = player.GetAdjustedItemScale(sword.Item);
            if (player.gravDir == 1f)
            {
                float handleLength = Vector2.Distance(sword.Grip, sword.HiltTop) * drawScale;
                float backArmRotation = GreatswordMotion.BackArmRotation(hand - player.MountedCenter, axis, Direction, handleLength, out _);
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);
            }

            Vector2 bladeAxis = sword.BladeTip - sword.Grip;
            float bladeLength = bladeAxis.Length() * drawScale;
            if (Direction < 0)
            {
                bladeAxis.Y = -bladeAxis.Y;
            }

            drawRotation = worldAngle - bladeAxis.ToRotation();
            tip = hand + axis * bladeLength;
            poseReady = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (DashSlash)
            {
                modifiers.Knockback *= 1.6f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, DashSlash ? 7 * 60 : 5 * 60);
            if (DashSlash)
            {
                target.AddBuff(BuffID.Daybreak, 3 * 60);
            }
        }

        float CurrentBaseAngle()
        {
            float progress = MathHelper.Clamp(Timer / AnimationLifetime, 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);
            return DashSlash
                ? MathHelper.Lerp(0.95f, -1.35f, progress)
                : MathHelper.Lerp(-2.25f, 0.75f, progress);
        }

        float CurrentAngle()
        {
            float baseAngle = CurrentBaseAngle();
            return Direction == 1 ? baseAngle : MathHelper.Pi - baseAngle;
        }

        void ApplyDashBraking(Player player)
        {
            // Preserve the initial burst, then progressively damp horizontal momentum. Continuing
            // to hold the dash direction carries a controlled running-speed exit; releasing it (or
            // pressing opposite) produces a tighter stop. Never push the player back through a wall.
            if (Timer <= DashBrakeStartTick || player.velocity.X * Direction <= 0f)
            {
                return;
            }

            float progress = MathHelper.Clamp(
                (Timer - DashBrakeStartTick) / (Lifetime - DashBrakeStartTick), 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);

            bool holdingDashDirection = Direction == 1 ? player.controlRight : player.controlLeft;
            bool holdingOppositeDirection = Direction == 1 ? player.controlLeft : player.controlRight;
            bool carryingMomentum = holdingDashDirection && !holdingOppositeDirection;
            float targetSpeed = carryingMomentum ? Direction * HeldDashExitSpeed : 0f;
            float damping = MathHelper.Lerp(0.06f, 0.16f, progress);

            player.velocity.X = MathHelper.Lerp(player.velocity.X, targetSpeed, damping);
            if (!carryingMomentum && System.Math.Abs(player.velocity.X) < 0.55f)
            {
                player.velocity.X = 0f;
            }
        }

        void InitializeCinderTrail()
        {
            cinderTrailInitialized = true;
            cinderTrailY = Projectile.Center.Y;
            lastCinderX = Projectile.Center.X;
            SpawnCinder(new Vector2(lastCinderX, cinderTrailY));
        }

        void UpdateCinderTrail(Player player)
        {
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            float forwardDistance = (player.Center.X - lastCinderX) * Direction;
            while (forwardDistance >= CinderSpacing)
            {
                lastCinderX += Direction * CinderSpacing;
                SpawnCinder(new Vector2(lastCinderX, cinderTrailY));
                forwardDistance -= CinderSpacing;
            }

            // Close the small final gap so the flames visibly reach the actual stopping point.
            if (Timer >= Lifetime - 1 && forwardDistance >= CinderEndCapDistance)
            {
                lastCinderX = player.Center.X;
                SpawnCinder(new Vector2(lastCinderX, cinderTrailY));
            }
        }

        void SpawnCinder(Vector2 position)
        {
            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            float damageScale = EmpoweredDash ? 0.4f / 1.55f : 0.28f / 1.35f;
            float knockbackScale = EmpoweredDash ? 0.35f / 2.1f : 0.35f / 1.8f;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<SwordOfLordGwynCinderTrail>(),
                (int)(Projectile.damage * damageScale), Projectile.knockBack * knockbackScale,
                Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!poseReady)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Greatsword sword = ContentSamples.ItemsByType[ModContent.ItemType<SwordOfLordGwyn>()].ModItem as Greatsword;
            Vector2 origin = sword.Grip;
            SpriteEffects effects = SpriteEffects.None;
            if (Direction < 0)
            {
                effects = SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
            }

            Color color = Color.White * MathHelper.Clamp(Projectile.timeLeft / 6f, 0.35f, 1f);
            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, color, drawRotation, origin, drawScale, effects, 0);
            return false;
        }
    }
}
