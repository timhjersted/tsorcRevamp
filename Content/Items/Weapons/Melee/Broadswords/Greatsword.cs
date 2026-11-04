using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public abstract class Greatsword : ModItem
    {
        // Coordinates in the inventory sprite, before the item scale is applied.
        protected internal abstract Vector2 Grip { get; }
        protected internal abstract Vector2 BladeHeel { get; }
        protected internal abstract Vector2 CuttingEdge { get; }
        protected internal abstract Vector2 BladeTip { get; }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useLimitPerAnimation = 1;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<GreatswordSwing>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[Item.shoot] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return false;
            }

            GreatswordComboPlayer combo = player.GetModPlayer<GreatswordComboPlayer>();
            int step = combo.NextStep;

            int facing = player.direction;
            if (velocity.X != 0f)
            {
                facing = Math.Sign(velocity.X);
            }

            int duration = Math.Max(GreatswordSwing.MinimumSwingTicks, player.itemAnimationMax);
            int packedSwing = Item.type * GreatswordSwing.ComboLength + step;
            int projectileIndex = Projectile.NewProjectile(source, player.MountedCenter, Vector2.UnitX * facing, type,
                damage, knockback, player.whoAmI, packedSwing, 0f, duration);
            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Main.projectile[projectileIndex].CritChance = (int)player.GetWeaponCrit(Item);
                combo.ResetCombo();
            }

            return false;
        }
    }

    public sealed class GreatswordComboPlayer : ModPlayer
    {
        private int weaponType;
        private int weaponSlot;
        private int nextStep;
        private ulong comboExpires;

        internal int NextStep
        {
            get
            {
                if (Player.HeldItem.type == weaponType && Player.selectedItem == weaponSlot && Main.GameUpdateCount <= comboExpires)
                {
                    return nextStep;
                }

                return 0;
            }
        }

        internal void CompleteSwing(int step)
        {
            weaponType = Player.HeldItem.type;
            weaponSlot = Player.selectedItem;
            nextStep = (step + 1) % GreatswordSwing.ComboLength;
            comboExpires = Main.GameUpdateCount + 45;
        }

        internal void ResetCombo()
        {
            weaponType = 0;
            nextStep = 0;
        }

        public override void ResetEffects()
        {
            if (nextStep != 0 && (NextStep == 0 || Player.GetModPlayer<tsorcRevampPlayer>().isDodging))
            {
                ResetCombo();
            }
        }

        public override void UpdateDead()
        {
            ResetCombo();
        }
    }

    public sealed class GreatswordSwing : ModProjectile
    {
        internal const int ComboLength = 3;
        internal const int MinimumSwingTicks = 8;
        private const int SlamStep = 2;
        private static readonly SoundStyle SlamSound = new("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
        {
            Volume = 0.45f
        };

        // The normal projectile packet carries weapon, step, elapsed ticks, duration and facing.
        private int WeaponType => (int)Projectile.ai[0] / ComboLength;
        private int Step => (int)Projectile.ai[0] % ComboLength;
        private float WindupEnd => Step == SlamStep ? 0.40f : 0.30f;
        private float CutEnd => Step == SlamStep ? 0.75f : 0.70f;

        private Vector2 hand;
        private Vector2 heel;
        private Vector2 edge;
        private Vector2 tip;
        private Vector2 previousHeel;
        private Vector2 previousEdge;
        private Vector2 previousTip;
        private Vector2 grip;
        private float drawRotation;
        private float drawScale;
        private bool flipSprite;
        private bool poseReady;
        private bool strikeActive;
        private bool playedCut;
        private bool playedImpact;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanCutTiles() => false;

        public override bool? CanDamage()
        {
            if (!strikeActive)
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

            Item swingItem = player.HeldItem;
            if (isOwner)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (swingItem.type != WeaponType || player.noItems || player.CCed || modPlayer.isDodging || modPlayer.isSwordflipping)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (swingItem.type != WeaponType)
            {
                // A swing packet can arrive before the remote player's selected slot is updated.
                ContentSamples.ItemsByType.TryGetValue(WeaponType, out swingItem);
            }

            if (swingItem?.ModItem is not Greatsword weapon)
            {
                Projectile.Kill();
                return;
            }

            float duration = Math.Max(MinimumSwingTicks, Projectile.ai[2]);
            float age = Projectile.ai[1];
            if (age + 1f >= duration)
            {
                if (isOwner)
                {
                    player.GetModPlayer<GreatswordComboPlayer>().CompleteSwing(Step);
                }

                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.MountedCenter;
            player.heldProj = Projectile.whoAmI;

            int facing = Projectile.velocity.X < 0f ? -1 : 1;
            player.ChangeDir(facing);
            float progress = (age + 1f) / duration;
            float angle = SwingAngle(progress);
            Vector2 axis = new(MathF.Cos(angle) * facing, MathF.Sin(angle) * player.gravDir);
            float worldAngle = axis.ToRotation();
            float armRotation = worldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

            Vector2 lastHand = hand;
            hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
            grip = weapon.Grip;
            drawScale = player.GetAdjustedItemScale(swingItem);
            flipSprite = facing * player.gravDir < 0f;
            Vector2 bladeAxis = weapon.BladeTip - grip;
            if (flipSprite)
            {
                bladeAxis.Y = -bladeAxis.Y;
            }

            drawRotation = worldAngle - bladeAxis.ToRotation();
            previousHeel = heel;
            previousEdge = edge;
            previousTip = tip;
            heel = BladePosition(weapon.BladeHeel);
            edge = BladePosition(weapon.CuttingEdge);
            tip = BladePosition(weapon.BladeTip);
            if (!poseReady || Vector2.DistanceSquared(lastHand, hand) > 64f * 64f)
            {
                previousHeel = heel;
                previousEdge = edge;
                previousTip = tip;
            }

            poseReady = true;
            strikeActive = progress > WindupEnd && age / duration < CutEnd;

            if (!Main.dedServ)
            {
                if (strikeActive && !playedCut)
                {
                    playedCut = true;
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.32f }, hand);
                }

                if (Step == SlamStep && !playedImpact && progress >= CutEnd - 0.05f && progress <= CutEnd + 0.1f)
                {
                    TryPlayGroundImpact(player.gravDir);
                }
            }

            Projectile.ai[1]++;
        }

        private float SwingAngle(float progress)
        {
            // The ready pose for each cut matches the preceding cut's finish, including when the combo loops.
            const float Carry = 0.25f;
            const float Low = 0.18f;
            const float High = -2.10f;
            float ready = Carry;
            float loaded = High;
            float finish = Low;
            if (Step == 1)
            {
                ready = Low;
                loaded = 0.23f;
                finish = High;
            }
            else if (Step == SlamStep)
            {
                ready = High;
                loaded = -2.88f;
                finish = Carry;
            }

            if (progress < WindupEnd)
            {
                return MathHelper.SmoothStep(ready, loaded, progress / WindupEnd);
            }

            float cutProgress = (progress - WindupEnd) / (CutEnd - WindupEnd);
            return MathHelper.SmoothStep(loaded, finish, cutProgress);
        }

        private Vector2 BladePosition(Vector2 pixel)
        {
            Vector2 offset = (pixel - grip) * drawScale;
            if (flipSprite)
            {
                offset.Y = -offset.Y;
            }

            return hand + offset.RotatedBy(drawRotation);
        }

        private void TryPlayGroundImpact(float gravity)
        {
            // Test the last few pixels under the tip for terrain or a platform, including a tip that
            // is already touching solid ground. An airborne finisher still swings without debris.
            Vector2 probeStart = tip - new Vector2(1f, 5f * gravity);
            Vector2 probe = new(0f, 14f * gravity);
            Vector2 travel = Collision.TileCollision(probeStart, probe, 2, 2, gravDir: (int)gravity);
            Vector2 impact = probeStart + travel;
            if (travel.Y == probe.Y)
            {
                if (!Collision.SolidCollision(tip - Vector2.One, 2, 2))
                {
                    return;
                }

                impact = tip;
            }

            playedImpact = true;
            SoundEngine.PlaySound(SlamSound, impact);
            UsefulFunctions.ScreenShake(impact, 3.5f, 10, distanceFalloff: 440f);
            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = new(Main.rand.NextFloat(-3.4f, 3.4f), Main.rand.NextFloat(-3.8f, -0.9f) * gravity);
                Dust.NewDustPerfect(impact, DustID.Stone, velocity, Scale: Main.rand.NextFloat(0.8f, 1.2f));
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = new(Main.rand.NextFloat(-4.6f, 4.6f), Main.rand.NextFloat(-2.8f, -0.6f) * gravity);
                Dust spark = Dust.NewDustPerfect(impact, DustID.BoneTorch, velocity, Scale: Main.rand.NextFloat(0.65f, 1f));
                spark.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Step == SlamStep)
            {
                modifiers.SourceDamage *= 1.25f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!strikeActive)
            {
                return false;
            }

            // Sweep both segments of the curved blade across the space between the two poses.
            Vector2 targetPosition = targetHitbox.TopLeft();
            Vector2 targetSize = targetHitbox.Size();
            float collisionPoint = 0f;
            for (int i = 0; i <= 8; i++)
            {
                float fraction = i / 8f;
                Vector2 sweptHeel = Vector2.Lerp(previousHeel, heel, fraction);
                Vector2 sweptEdge = Vector2.Lerp(previousEdge, edge, fraction);
                Vector2 sweptTip = Vector2.Lerp(previousTip, tip, fraction);
                if (Collision.CheckAABBvLineCollision(targetPosition, targetSize, sweptHeel, sweptEdge, 12f, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetPosition, targetSize, sweptEdge, sweptTip, 8f, ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!poseReady)
            {
                return false;
            }

            Main.instance.LoadItem(WeaponType);
            Texture2D texture = TextureAssets.Item[WeaponType].Value;
            SpriteEffects effects = SpriteEffects.None;
            Vector2 origin = grip;
            if (flipSprite)
            {
                effects = SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
            }

            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, lightColor, drawRotation, origin, drawScale, effects);
            return false;
        }
    }
}
