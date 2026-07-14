using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynSlash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SwordOfGwyn";

        // Player-facing draw scale for the 128px SwordOfGwyn sprite. At 1.0 it filled the screen;
        // 0.5 reads as a large greatsword. (User asked for "0.05" — that would be ~6px / invisible,
        // so interpreted as 0.5; change this one constant to taste.)
        const float DrawScale = 0.5f;
        // Hitbox kept proportional to the drawn blade so the sword hits where it visibly reaches
        // (the old 142/42 were tuned for the full-size sprite).
        const float BladeLength = 142f * DrawScale;
        const float BladeWidth = 42f * DrawScale;
        // Grip position within the sprite (handle is lower-left): the pivot the sword is drawn
        // around, so the hilt sits IN the hand instead of floating mid-blade like the old 0.45.
        const float GripNormX = 0.19f;
        const float GripNormY = 0.81f;
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
            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, Lifetime - (int)Timer);
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, false, false);
            Projectile.rotation = CurrentAngle();
            Projectile.spriteDirection = Direction;
            player.heldProj = Projectile.whoAmI;

            player.itemTime = 2;
            player.itemAnimation = 2;
            player.ChangeDir(Direction);
            SwordOfLordGwynPlayerAnimation.SetUseFrame(player, CurrentUseFrame());

            if (DashSlash)
            {
                player.ResetMeleeHitCooldowns();
                ApplyDashBraking(player);
                UpdateCinderTrail(player);
            }

            Vector2 start = BladeStart();
            Vector2 end = BladeEnd();
            for (int i = 0; i < (DashSlash ? 5 : 3); i++)
            {
                Vector2 pos = Vector2.Lerp(start, end, Main.rand.NextFloat());
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos - new Vector2(3f), 6, 6, type, player.velocity.X * 0.1f, player.velocity.Y * 0.1f, 60, default, DashSlash ? 1.9f : 1.35f);
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.8f, 0.42f, 0.12f);

            if (Timer >= Lifetime)
            {
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), BladeStart(), BladeEnd(), BladeWidth, ref point);
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

        int CurrentUseFrame()
        {
            float progress = MathHelper.Clamp(Timer / AnimationLifetime, 0f, 0.999f);
            return (int)(progress * 4f);
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

        Vector2 BladeStart()
        {
            Player player = Main.player[Projectile.owner];
            return SwordOfLordGwynPlayerAnimation.GetHandPosition(player, CurrentUseFrame());
        }

        Vector2 BladeEnd()
        {
            return BladeStart() + CurrentAngle().ToRotationVector2() * BladeLength;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            // Mirror the grip's X for a left-facing (flipped) draw so the hilt stays in the hand
            // for both facings (origin is in pre-flip texture space).
            float originX = Direction == 1 ? texture.Width * GripNormX : texture.Width * (1f - GripNormX);
            Vector2 origin = new Vector2(originX, texture.Height * GripNormY);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = Projectile.rotation + (Direction == 1 ? MathHelper.PiOver4 : MathHelper.Pi - MathHelper.PiOver4);
            Color color = Color.White * MathHelper.Clamp(Projectile.timeLeft / 6f, 0.35f, 1f);
            Main.EntitySpriteDraw(texture, BladeStart() - Main.screenPosition, null, color, drawRotation, origin, DashSlash ? DrawScale * 1.18f : DrawScale, effects, 0);
            return false;
        }
    }
}
