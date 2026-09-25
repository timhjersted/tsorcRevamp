using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee._Animations;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.Melee.Axes;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Axes
{
    /// <summary>
    /// Shared hold-to-charge machine for both forms of the Ancient Fire Axe (Owl Father's drop).
    /// Right click swaps the held item between the long axe (<see cref="AncientFireAxe"/>) and the
    /// short axe (<see cref="AncientFireAxeShort"/>). Taps swing normally (the broadsword rework's
    /// alternating overhand / underhand swing); holding attack freezes the swing on its raised
    /// start pose, and each form decides what a full charge releases.
    /// </summary>
    public abstract class AncientFireAxeForm : ModItem
    {
        // Cursor this close to a choppable tile = the player is felling a tree, so no charging.
        private const float ChopCheckRange = 10f * 16f;
        // Quick underhand flourish when the thrown short axe is picked up while held.
        private const int CatchSwingTicks = 28;

        protected enum HeldAttackState
        {
            None,
            Charging,
            ChargedSwing,
            SlamFalling,
            SlamStriking,
            ThrowSwing,
        }

        protected HeldAttackState heldState;
        protected int chargeTicks;
        // Read by ModifyHitNPC; reset to 1 at the start of every swing, so a bonus only covers the
        // swing that earned it.
        protected float swingDamageMult = 1f;
        // Bridges the gap between the short axe leaving the hand and ownedProjectileCounts (or, on
        // other clients, the synced projectile) catching up; cleared once IsAxeOut takes over.
        protected bool axeLeftHand;

        protected abstract int ChargeTicksRequired { get; }
        protected abstract int OtherFormType { get; }
        // Grip-to-axe-head distance on this form's sprite, before item scale.
        protected abstract float AxeHeadReach { get; }

        // Charge glow uses fire while the release will be fully powered, grey smoke otherwise.
        protected virtual bool ChargeFuelled(Player player) => true;

        // Called every held charge tick after the pose is pinned; return true if the form switched
        // heldState itself (e.g. into the air slam), which skips the rest of the charge tick.
        protected virtual bool OnChargeTick(Player player, bool isOwner) => false;

        // Called once when the button is released with a full charge. Must set heldState.
        protected abstract void OnChargeReleased(Player player, bool isOwner);

        // Runs every tick for the post-release states the form owns.
        protected abstract void UpdateAfterRelease(Player player, bool isOwner);

        // Both forms are one weapon: while the thrown short axe is out, neither form can be used.
        public static bool IsAxeOut(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<AncientFireAxeThrown>()] > 0;
        }

        // Holding on a tree is chopping, not charging. MouseWorld is the rework's synced cursor, so
        // remote clients reach the same answer.
        private static bool IsAimingAtTree(Player player)
        {
            Vector2 mouseWorld = player.GetModPlayer<BroadswordReworkPlayer>().MouseWorld;
            Tile mouseTile = Framing.GetTileSafely(mouseWorld);
            bool aimingAtTree = mouseTile.HasTile && Main.tileAxe[mouseTile.TileType];
            bool treeInReach = player.Distance(mouseWorld) <= ChopCheckRange;
            return aimingAtTree && treeInReach;
        }

        // Owner only. Removes this player's live melee slash VFX (tsorcGlobalItem spawns one on the
        // swing's first frame) and optionally starts a new one that tracks the swing from here on,
        // reading the current overhand/underhand flip.
        protected static void ReplaceSlashVfx(Player player, bool spawnFresh)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            int slashType = ModContent.ProjectileType<Projectiles.VFX.Slash>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile slash = Main.projectile[i];

                if (slash.active && slash.type == slashType && slash.owner == player.whoAmI)
                {
                    slash.Kill();
                }
            }

            if (spawnFresh)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.Center, Vector2.Zero, slashType, 0, 1, player.whoAmI);
            }
        }

        public override bool AltFunctionUse(Player player) => true;

        // No auto-swing, even with vanilla's "auto reuse all weapons" setting: every charge attack
        // needs a fresh press, so a held button after an attack can't roll straight into another
        // charge. Chopping trees keeps auto-swing (it never charges).
        public override bool? CanAutoReuseItem(Player player)
        {
            return IsAimingAtTree(player);
        }

        public override bool CanUseItem(Player player)
        {
            if (IsAxeOut(player))
            {
                return false;
            }

            if (player.altFunctionUse != 2)
            {
                return true;
            }

            // Right click: swap the held item to the other form in place, keeping prefix and favorite.
            // Slot 58 is the cursor item, which isn't a real inventory slot to swap.
            bool canSwap = player.whoAmI == Main.myPlayer && player.selectedItem != 58;

            if (canSwap)
            {
                Item heldItem = player.inventory[player.selectedItem];
                int prefix = heldItem.prefix;
                heldItem.ChangeItemType(OtherFormType);

                if (prefix != 0)
                {
                    heldItem.Prefix(prefix);
                }

                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.3f }, player.Center);

                for (int i = 0; i < 12; i++)
                {
                    Dust flare = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Torch, 0f, -1.5f, 80, default, 1.4f);
                    flare.noGravity = true;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, player.selectedItem);
                }
            }

            return false;
        }

        public override void UseAnimation(Player player)
        {
            // Every swing starts plain; HoldItem upgrades it if the button stays down.
            heldState = HeldAttackState.None;
            swingDamageMult = 1f;
            chargeTicks = 0;
            axeLeftHand = false;

            if (player.mount.Active || IsAimingAtTree(player))
            {
                return;
            }

            heldState = HeldAttackState.Charging;
        }

        // Runs after vanilla has decremented itemAnimation and before the use style / melee hit code,
        // so pinning itemAnimation at its max here holds the swing on its first (raised) frame.
        // The owner decides, pays and spawns; other clients run the same machine off synced
        // controls/velocity purely for the pose, dust and sound.
        public override void HoldItem(Player player)
        {
            // Empty hand while the short axe is thrown: the arm still plays the swing motion, but
            // noUseGraphic hides the axe and noMelee stops both the hitbox and the rework's slash
            // overlay. Every client runs this, and thrown projectiles sync, so everyone agrees.
            bool axeOut = IsAxeOut(player);

            if (axeOut)
            {
                axeLeftHand = false;
            }

            bool handEmpty = axeLeftHand || axeOut;
            Item.noUseGraphic = handEmpty;

            // Pinned holds (charging, slam fall) are noMelee too. Pinning itemAnimation at its max
            // makes vanilla's ItemAnimationJustStarted true every tick, and tsorcGlobalItem spawns a
            // slash VFX on JustStarted — without this a new slash crept out of the pose every tick.
            // noMelee blocks that spawn and the rework's slash overlay. Releases re-enable it and
            // spawn their own slash (ReplaceSlashVfx).
            bool pinnedHold = heldState == HeldAttackState.Charging || heldState == HeldAttackState.SlamFalling;
            Item.noMelee = handEmpty || pinnedHold;

            // Vanilla draws the in-use item from a clone snapshotted at swing start
            // (lastVisualizedSelectedItem), so the hide flag has to reach that copy too.
            if (player.lastVisualizedSelectedItem.type == Item.type)
            {
                player.lastVisualizedSelectedItem.noUseGraphic = handEmpty;
            }

            if (Main.dedServ || heldState == HeldAttackState.None)
            {
                return;
            }

            bool isOwner = player.whoAmI == Main.myPlayer;

            if (heldState != HeldAttackState.Charging)
            {
                UpdateAfterRelease(player, isOwner);
                return;
            }

            bool interrupted = player.itemAnimation == 0 || player.mount.Active || player.CCed;

            if (interrupted)
            {
                heldState = HeldAttackState.None;
                return;
            }

            if (!player.controlUseItem)
            {
                // The swing is live from this tick (noMelee was set above while still Charging).
                Item.noMelee = handEmpty;

                if (chargeTicks >= ChargeTicksRequired)
                {
                    OnChargeReleased(player, isOwner);
                }
                else
                {
                    // Released early: the frozen swing carries on as a normal attack. The pinned hold
                    // blocked the usual swing-start slash, so spawn one now; it reads this swing's flip.
                    ReplaceSlashVfx(player, spawnFresh: true);
                    heldState = HeldAttackState.None;
                }

                return;
            }

            // Hold on the start pose of whichever way the rework's alternating swing was going
            // (overhand or underhand); held attacks keep that direction. The rework deals no damage
            // while itemAnimation is at max.
            chargeTicks++;
            player.itemAnimation = player.itemAnimationMax;

            if (OnChargeTick(player, isOwner))
            {
                return;
            }

            // The pending swing keeps tracking the cursor while held (rework faces the player to it).
            if (Item.TryGetGlobalItem(out ItemMeleeAttackAiming chargeAiming))
            {
                chargeAiming.AttackDirection = player.LookDirection();
            }

            // Axe-head glow that builds with the charge, so the player can read the timing.
            float chargeProgress = MathHelper.Clamp(chargeTicks / (float)ChargeTicksRequired, 0f, 1f);
            int glowDustType = ChargeFuelled(player) ? DustID.Torch : DustID.Smoke;

            if (Main.rand.NextFloat() < 0.25f + chargeProgress * 0.6f)
            {
                Vector2 glowPosition = AxeHeadPosition(player) + Main.rand.NextVector2Circular(10f, 10f);
                Vector2 glowVelocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -1.2f);
                Dust glow = Dust.NewDustPerfect(glowPosition, glowDustType, glowVelocity, 80, default, 0.9f + chargeProgress * 0.7f);
                glow.noGravity = true;
            }

            // Fully charged cue: chime + a flare off the axe head.
            if (chargeTicks == ChargeTicksRequired)
            {
                SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.3f }, player.Center);
                Vector2 flareCenter = AxeHeadPosition(player);

                for (int i = 0; i < 16; i++)
                {
                    Vector2 flareVelocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                    Dust flare = Dust.NewDustPerfect(flareCenter, glowDustType, flareVelocity, 60, default, 1.4f);
                    flare.noGravity = true;
                }
            }
        }

        // Axe head in the rework's swing pose. MeleeAnimation stores itemRotation as the weapon angle
        // + 45deg (+90deg more when facing left); undo that and walk out from the grip (itemLocation).
        protected Vector2 AxeHeadPosition(Player player)
        {
            float weaponRotation = player.itemRotation - MathHelper.PiOver4;

            if (player.direction < 0)
            {
                weaponRotation -= MathHelper.PiOver2;
            }

            float headDistance = AxeHeadReach * player.GetAdjustedItemScale(Item);
            return player.itemLocation + weaponRotation.ToRotationVector2() * headDistance;
        }

        // Whether the current swing is the rework's underhand (rising) variant; false = overhand.
        protected bool IsUnderhandSwing()
        {
            return Item.TryGetGlobalItem(out QuickSlashMeleeAnimation slashAnimation) && slashAnimation.IsAttackFlipped;
        }

        // Called by the thrown axe when its owner walks over it while holding either form: a real
        // (damaging) quick underhand swing toward the facing direction, started by hand because no
        // item use began — so the rework's aim/flip and the slash VFX are set up here too.
        public void CatchThrownAxe(Player player)
        {
            heldState = HeldAttackState.None;
            swingDamageMult = 1f;
            axeLeftHand = false;
            Item.noUseGraphic = false;
            Item.noMelee = false;

            // Still finishing the throw's follow-through (caught right after throwing): just reappear.
            if (player.itemAnimation > 0)
            {
                return;
            }

            if (Item.TryGetGlobalItem(out QuickSlashMeleeAnimation slashAnimation))
            {
                slashAnimation.IsAttackFlipped = true;
            }

            if (Item.TryGetGlobalItem(out ItemMeleeAttackAiming catchAiming))
            {
                catchAiming.AttackDirection = new Vector2(player.direction, 0f);
            }

            // itemTime matches so the swing doesn't fire the item's shoot/UseItem mid-flourish.
            // Fresh snapshot (see HoldItem) so the drawn axe is this one, visible again.
            player.lastVisualizedSelectedItem = Item.Clone();
            player.itemAnimationMax = CatchSwingTicks;
            player.itemAnimation = CatchSwingTicks;
            player.itemTimeMax = CatchSwingTicks;
            player.itemTime = CatchSwingTicks;

            SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.2f }, player.Center);
            ReplaceSlashVfx(player, spawnFresh: true);
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= swingDamageMult;
        }

        public override void MeleeEffects(Terraria.Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }

    /// <summary>
    /// Long form. Held attacks keep the rework's alternating direction. Ground: hold GroundChargeTicks
    /// then release for a heavy swing + Owl Father's ground fire wave (underhand also lifts the player
    /// RisingSlashTiles). Air: hold AirSlamHoldTicks — overhand plunges into a slam that erupts the wave
    /// on landing, underhand launches a rising slash upward. Held attacks cost mana; without it they
    /// still hit harder but release no wave.
    /// </summary>
    public class AncientFireAxe : AncientFireAxeForm
    {
        public const int GroundChargeTicks = 60;
        public const int AirSlamHoldTicks = 20;
        public const int HeldAttackManaCost = 10;
        public const float HeldDamageMult = 1.5f;
        public const float HeldDamageMultNoMana = 1.25f;
        // Direct axe hits (both forms) vs. the fire effects (wave, columns, thrown axe, burst).
        public const int MeleeOnFireTicks = 12 * 60;
        public const int FireOnFireTicks = 3 * 60;

        // Ground release: the wave launches once the downswing reaches this much of its timeline.
        // The rework's swing curve is front-loaded — 0.3 of the time is half the arc, blade crossing the front.
        private const float FireWaveSwingProgress = 0.3f;
        private const float FireWaveStartOffset = 20f;

        // Slam: 10px/tick is vanilla's default maxFallSpeed, so the clamp can't shave it back down.
        private const float SlamFallSpeed = 10f;
        // Downswing starts when the ground is this many ticks of fall away. With the 16-tick strike,
        // 8 ticks in is the rework's 0.5 progress (~75% of the arc), so the blade is coming down on landing.
        private const int SlamStrikeLeadTicks = 8;
        private const int SlamStrikeTicks = 16;
        // Safety valves: bottomless fall, or the strike started but we slid off the ledge it saw.
        private const int SlamMaxFallTicks = 120;
        private const int SlamMaxStrikeTicks = 30;
        // Pixels below the feet that still count as "standing" for the air-slam check.
        private const int GroundedProbeHeight = 8;

        // Underhand held swings lift the player this many tiles. Launch speed from v = sqrt(2*g*h) with
        // vanilla's default player gravity (0.4 px/tick^2): 8 tiles = 128px -> ~10.1 px/tick upward.
        public const int RisingSlashTiles = 8;
        private static readonly float RisingSlashLaunchSpeed = (float)Math.Sqrt(2f * Player.defaultGravity * RisingSlashTiles * 16f);

        private int slamTicks;
        private bool heldAttackPaid;
        // ChargedSwing's ground fire wave: on for the ground release, off for the airborne rising slash.
        private bool releaseFireWave;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GreatFireAxe));

        protected override int ChargeTicksRequired => GroundChargeTicks;
        protected override int OtherFormType => ModContent.ItemType<AncientFireAxeShort>();
        // 72x64 sprite, diagonal ~96px.
        protected override float AxeHeadReach => 70f;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            GroundChargeTicks / 60f,
            HeldAttackManaCost,
            (int)Math.Round((HeldDamageMult - 1f) * 100f),
            (int)Math.Round((HeldDamageMultNoMana - 1f) * 100f),
            AncientFireAxeFireWave.TravelTiles,
            FireOnFireTicks / 60,
            RisingSlashTiles);

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 30;
            Item.width = 72;
            Item.height = 64;
            Item.knockBack = 10f;
            Item.DamageType = DamageClass.Melee;
            Item.axe = 15; // Same as axe of the night
            Item.useAnimation = 37;
            Item.useTime = 37;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = false; // see CanAutoReuseItem: held attacks need a fresh press
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        protected override bool ChargeFuelled(Player player)
        {
            return player.statMana >= HeldAttackManaCost;
        }

        protected override bool OnChargeTick(Player player, bool isOwner)
        {
            // velocity.Y alone flickers non-zero while walking down slopes, so also require no
            // floor (platforms count) within a few pixels of the feet.
            bool movingVertically = player.velocity.Y != 0f;
            bool floorUnderFeet = Collision.SolidCollision(player.BottomLeft, player.width, GroundedProbeHeight, true);
            bool airborne = movingVertically && !floorUnderFeet;

            if (!airborne || chargeTicks < AirSlamHoldTicks)
            {
                return false;
            }

            BeginHeldAttack(player, isOwner);

            // Aimed level: the overhand arc then ends pointing at the ground, the underhand one overhead.
            if (Item.TryGetGlobalItem(out ItemMeleeAttackAiming airAiming))
            {
                airAiming.AttackDirection = new Vector2(player.direction, 0f);
            }

            if (IsUnderhandSwing())
            {
                // Underhand turn: the rising slash carries the player up instead of diving. It swings
                // right away (no release needed, same as the slam) and has no ground to put a wave on.
                if (isOwner)
                {
                    player.RemoveAllGrapplingHooks();
                    player.velocity.Y = -RisingSlashLaunchSpeed;
                }

                releaseFireWave = false;
                heldState = HeldAttackState.ChargedSwing;
                // Leaving the pinned hold: the swing hits from here (HoldItem set noMelee while Charging).
                Item.noMelee = false;
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.2f }, player.Center);
                ReplaceSlashVfx(player, spawnFresh: true);
                return true;
            }

            if (isOwner)
            {
                player.RemoveAllGrapplingHooks();
                player.velocity.Y = SlamFallSpeed;
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.55f, Pitch = -0.3f }, player.Center);
            slamTicks = 0;
            heldState = HeldAttackState.SlamFalling;
            return true;
        }

        protected override void OnChargeReleased(Player player, bool isOwner)
        {
            BeginHeldAttack(player, isOwner);

            // Underhand held swing: lift the player a few tiles as the axe comes up.
            if (isOwner && IsUnderhandSwing())
            {
                player.velocity.Y = -RisingSlashLaunchSpeed;
            }

            releaseFireWave = true;
            heldState = HeldAttackState.ChargedSwing;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.4f }, player.Center);
            ReplaceSlashVfx(player, spawnFresh: true);
        }

        protected override void UpdateAfterRelease(Player player, bool isOwner)
        {
            if (heldState == HeldAttackState.ChargedSwing)
            {
                float swingProgress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
                bool bladeCrossedFront = swingProgress >= FireWaveSwingProgress || player.itemAnimation == 0;

                if (bladeCrossedFront)
                {
                    if (isOwner && heldAttackPaid && releaseFireWave)
                    {
                        SpawnFireWave(player);
                    }

                    heldState = HeldAttackState.None;
                }

                return;
            }

            if (heldState == HeldAttackState.SlamFalling)
            {
                slamTicks++;
                player.itemAnimation = player.itemAnimationMax;

                // Look SlamStrikeLeadTicks of fall below the feet (platforms count) for the ground.
                int probeHeight = (int)(SlamFallSpeed * SlamStrikeLeadTicks);
                bool alreadyLanded = player.velocity.Y == 0f;
                bool groundAhead = alreadyLanded || Collision.SolidCollision(player.BottomLeft, player.width, probeHeight, true);
                bool fellTooLong = slamTicks >= SlamMaxFallTicks;

                if (!groundAhead && !fellTooLong)
                {
                    if (isOwner)
                    {
                        player.velocity.Y = Math.Max(player.velocity.Y, SlamFallSpeed);
                        // Re-anchoring the fall start every tick means the plunge never deals fall damage.
                        player.fallStart = (int)(player.position.Y / 16f);
                    }

                    Vector2 trailPosition = AxeHeadPosition(player) + Main.rand.NextVector2Circular(8f, 8f);
                    Dust trail = Dust.NewDustPerfect(trailPosition, DustID.Torch, new Vector2(0f, -2f), 80, default, 1.3f);
                    trail.noGravity = true;
                    return;
                }

                // Release the downswing on a shortened timeline. No return: a slam that is already on
                // the ground resolves its impact below in this same tick.
                player.itemAnimationMax = SlamStrikeTicks;
                player.itemAnimation = SlamStrikeTicks;
                slamTicks = 0;
                heldState = HeldAttackState.SlamStriking;
                // Leaving the pinned fall: the downswing hits (HoldItem set noMelee while SlamFalling).
                Item.noMelee = false;
                ReplaceSlashVfx(player, spawnFresh: true);
            }

            if (heldState == HeldAttackState.SlamStriking)
            {
                slamTicks++;
                bool landed = player.velocity.Y == 0f;

                if (landed)
                {
                    // Same landing thud Owl Father's leap slams use.
                    SoundEngine.PlaySound(
                        new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                            with { Volume = 0.42f, Pitch = 0.18f },
                        player.Bottom);
                    UsefulFunctions.ScreenShake(player.Bottom, 3.25f, 9, distanceFalloff: 560f);

                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 emberVelocity = new Vector2(Main.rand.NextFloat(-4.2f, 4.2f), Main.rand.NextFloat(-4.8f, -0.7f));
                        Vector2 emberPosition = player.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f);
                        Dust ember = Dust.NewDustPerfect(emberPosition, DustID.Torch, emberVelocity, 80, default, Main.rand.NextFloat(1.0f, 1.55f));
                        ember.noGravity = true;
                    }

                    if (isOwner && heldAttackPaid)
                    {
                        SpawnFireWave(player);
                    }

                    heldState = HeldAttackState.None;
                    return;
                }

                if (slamTicks >= SlamMaxStrikeTicks)
                {
                    heldState = HeldAttackState.None;
                    return;
                }

                if (isOwner)
                {
                    player.velocity.Y = Math.Max(player.velocity.Y, SlamFallSpeed);
                    player.fallStart = (int)(player.position.Y / 16f);
                }
            }
        }

        // Commits a held attack: owner pays the mana, everyone sets the damage bonus it earned.
        // Other clients can't see the payment, so they guess from the synced mana for cosmetics only.
        private void BeginHeldAttack(Player player, bool isOwner)
        {
            if (isOwner)
            {
                heldAttackPaid = player.CheckMana(HeldAttackManaCost, pay: true);
            }
            else
            {
                heldAttackPaid = player.statMana >= HeldAttackManaCost;
            }

            if (heldAttackPaid)
            {
                player.manaRegenDelay = (int)player.maxRegenDelay;
                swingDamageMult = HeldDamageMult;
            }
            else
            {
                swingDamageMult = HeldDamageMultNoMana;
            }
        }

        // Owner only. Launches the ground wave from just in front of the player's feet, in the
        // direction they face; it carries the held-attack damage bonus.
        private void SpawnFireWave(Player player)
        {
            int direction = player.direction;
            float startX = player.Bottom.X + direction * FireWaveStartOffset;

            if (!PuppetGroundDustWave.TryFindGroundY(startX, player.Bottom.Y, out float groundY))
            {
                return;
            }

            int damage = (int)(player.GetWeaponDamage(Item) * HeldDamageMult);
            float knockback = player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack);
            Vector2 spawnPosition = new Vector2(startX, groundY - 20f);
            Vector2 velocity = new Vector2(direction * AncientFireAxeFireWave.TravelSpeed, 0f);

            Projectile.NewProjectile(player.GetSource_ItemUse(Item), spawnPosition, velocity,
                ModContent.ProjectileType<AncientFireAxeFireWave>(), damage, knockback, player.whoAmI);
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, MeleeOnFireTicks, false);
        }
    }

    /// <summary>
    /// Short form (the axe's original sprite). Keeps the old on-hit bouncing fireball. Hold
    /// ThrowChargeTicks, release, and the overhead swing hurls the axe: it spins toward the cursor,
    /// bursts into flame on the first thing it hits, then sticks in the ground or wall until the
    /// player walks over it to pick it back up.
    /// </summary>
    public class AncientFireAxeShort : AncientFireAxeForm
    {
        public const int ThrowChargeTicks = 45;
        public const float ThrownDamageMult = 1.5f;
        public const float ThrowSpeed = 14f;

        // Let go of the axe once the overhead swing is this far through its timeline — arm up and forward.
        private const float ThrowReleaseProgress = 0.2f;

        // The item's own AncientFireAxe.png (the original short sprite), shared on purpose.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(AncientFireAxe));

        protected override int ChargeTicksRequired => ThrowChargeTicks;
        protected override int OtherFormType => ModContent.ItemType<AncientFireAxe>();
        // 50x42 sprite, diagonal ~65px.
        protected override float AxeHeadReach => 48f;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            ThrowChargeTicks / 60f,
            (int)Math.Round((ThrownDamageMult - 1f) * 100f),
            AncientFireAxeFireBurst.WidthTiles,
            AncientFireAxe.FireOnFireTicks / 60);

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 30;
            Item.width = 50;
            Item.height = 42;
            Item.knockBack = 10f;
            Item.DamageType = DamageClass.Melee;
            Item.axe = 15;
            Item.useAnimation = 37;
            Item.useTime = 37;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = false; // see CanAutoReuseItem: held attacks need a fresh press
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        protected override void OnChargeReleased(Player player, bool isOwner)
        {
            heldState = HeldAttackState.ThrowSwing;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.3f }, player.Center);
        }

        protected override void UpdateAfterRelease(Player player, bool isOwner)
        {
            if (heldState != HeldAttackState.ThrowSwing)
            {
                return;
            }

            float swingProgress = 1f - player.itemAnimation / (float)player.itemAnimationMax;
            bool armForward = swingProgress >= ThrowReleaseProgress || player.itemAnimation == 0;

            if (!armForward)
            {
                return;
            }

            // The hand is empty from here on; the arm finishes the swing motion without the axe, its
            // hitbox or any slash (set now rather than waiting for next tick's HoldItem).
            axeLeftHand = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            player.lastVisualizedSelectedItem.noUseGraphic = true;
            ReplaceSlashVfx(player, spawnFresh: false);
            heldState = HeldAttackState.None;
            SoundEngine.PlaySound(SoundID.Item19 with { Volume = 0.8f, Pitch = -0.2f }, player.Center);

            if (!isOwner)
            {
                return;
            }

            Vector2 throwDirection = player.LookDirection();
            Vector2 spawnPosition = player.MountedCenter + throwDirection * 12f;
            int damage = (int)(player.GetWeaponDamage(Item) * ThrownDamageMult);
            float knockback = player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack);

            Projectile.NewProjectile(player.GetSource_ItemUse(Item), spawnPosition, throwDirection * ThrowSpeed,
                ModContent.ProjectileType<AncientFireAxeThrown>(), damage, knockback, player.whoAmI);
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, AncientFireAxe.MeleeOnFireTicks, false);
            Projectile.NewProjectileDirect(Projectile.InheritSource(Item), player.Center, UsefulFunctions.Aim(player.Center, Main.MouseWorld, 7.5f), ModContent.ProjectileType<AncientFireAxeFireball>(), (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer, Item.crit + player.GetTotalCritChance(DamageClass.Melee));
        }
    }
}
