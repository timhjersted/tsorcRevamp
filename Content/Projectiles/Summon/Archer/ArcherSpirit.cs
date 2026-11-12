using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Content.Items.Armor.Enemy.ManhunterSet;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.Content.Projectiles.Summon.Archer
{
    /// <summary>
    /// The Archer Spirit minion: the gold spirit of a Cloud Hunter. The body is an ordinary minion projectile (so minion
    /// slots, right-click targeting, Spirit Ashes and minion sacrifice all work unchanged); it is DRAWN as a player rig
    /// in the Man Hunter outfit, Reflective Gold dyed, holding a Shadewood Bow with a live string (ArcherSpiritBowLayer).
    /// </summary>
    public class ArcherSpirit : ModProjectile
    {
        private const int ArcherNavSearchRadius = 100;
        private const int AimWindupFrames = 12;
        private const int ShotCooldownTicks = 60;

        // Leash: start walking back past this distance from the owner, stop once this close again.
        private const float ReturnStartDistance = 480f;
        private const float ReturnStopDistance = 160f;
        // Beyond this the archer ignores every target and only returns (or teleports when stuck).
        private const float HardLeashDistance = 800f;
        // Aim range from the archer itself, with hysteresis so it does not flicker at the edge.
        private const float AimStartRange = 650f;
        private const float AimStopRange = 750f;
        // Targets must stay this close to the owner (box, per axis) so the archer does not wander off chasing.
        private const float OwnerTargetBox = 640f;

        // ── Arrow ────────────────────────────────────────────────────────────────────────────────────────────
        // ArcherSpiritArrow runs vanilla arrow AI: straight for 15t, then +0.1 px/t² gravity.
        private const float ArrowSpeed = 13f;
        private const float ArrowGravityDelayTicks = 15f;
        private const float ArrowGravity = 0.1f;
        // Half the target's vertical speed: a jump or a bob bends back within the flight time.
        private const float ArrowVerticalLeadFraction = 0.5f;
        private const int ArrowInterceptPasses = 4;

        // ── Rig pose (values copied from HunterArcherPuppet so the spirit carries and draws like the hunters) ─
        // Facing-space angles: 0 = level forward, + = down. At 1.2 rad the far forearm points down-forward and the
        // bow, perpendicular to it, lies almost flat at the waist with the string on top.
        private const float RestCarryAngle = 1.20f;
        // Bow angle easing per tick toward its target: quick raise (~5t), slower lower (~12t).
        private const float RaiseEase = 0.35f;
        private const float LowerEase = 0.15f;
        // String pull over the 12t windup: reach t0-3 (hand still at rest), pull t3-9, trembling hold t9-12.
        private const int PullStartTick = 3;
        private const int PullFullTick = 9;
        private const float FullPullPixels = 11f;
        // After a release: the string hand flicks back for 4t, then returns to the bow over 12t.
        private const int ReleaseFlickTicks = 4;
        private const int ReleaseReturnTicks = 12;

        // ── Shadewood Bow geometry (16x32, facing right; vanilla Item_923, same silhouette as the Wooden Bow) ─
        // Riser centre (13, 16); string inset at x2..3 / y6..25, limb tips at (3, 6) and (3, 26).
        private static readonly Vector2 BowGripTexel = new Vector2(13f, 16f);
        private static readonly Vector2 BowTopTipTexel = new Vector2(3f, 6f);
        private static readonly Vector2 BowBottomTipTexel = new Vector2(3f, 26f);
        private const float BowStringThickness = 2f;
        // The baked string strip cut out of the sprite while the live string is drawn (x2..3, y6..25).
        private static readonly Rectangle BowStringTexels = new Rectangle(2, 6, 2, 20);
        // Reflective Gold renders the string's dark sprite pixels pale gold; the live string matches that.
        private static readonly Color SpiritStringColor = new Color(255, 225, 140);

        public static int CalculateArrowDamage(Player player)
        {
            // Remove only BotC's Arcane Sorcery multiplier; gear's max-mana bonuses still count.
            float modeManaMultiplier = player.GetModPlayer<ArcaneSorceryPlayer>().ArcaneSorcerer
                ? 1f + ArcaneSorceryPlayer.MaxManaAmplifier / 100f
                : 1f;
            return (int)(player.statManaMax2 / modeManaMultiplier / 4f);
        }

        /// <summary>Set for exactly the duration of this spirit's DrawPlayer call, so ArcherSpiritBowLayer draws its bow.</summary>
        internal static ArcherSpirit DrawingSpirit;

        public int shootCooldown = 0;
        public int aimWindupTimer = 0;
        public int stuckTimer = 0;
        public int patrolTimer = 0;
        public int patrolDirection = 0;
        public int lastTargetWhoAmI = -1;
        public bool isAiming = false;
        public bool isBackingAway = false;
        public bool isReturning = false;

        // ── Pose state ───────────────────────────────────────────────────────────────────────────────────────
        // The owner's AI writes these; SendExtraAI carries them to other clients, which advance them locally between
        // packets. Every visual reads these, never the AI fields above, so remote players see the owner's real shots.
        private bool _poseAiming;
        private Vector2 _poseAimDirection = Vector2.UnitX;
        private int _poseWindup;
        private int _poseCooldown;
        // Incremented on every shot; a remote client that sees it change plays the release.
        private byte _shotCount;

        // Visual only, every non-dedicated machine.
        private Player _rig;
        private float _bowAngle = RestCarryAngle;
        private int _releaseTicks;
        private Vector2 _releasedStringPoint;
        // Draw-time only: the near hand's position this frame, set just before DrawPlayer for the bow layer.
        private Vector2 _frontHand;

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 48;
            Projectile.tileCollide = true;
            Projectile.minion = true;
            Projectile.scale = 1.0f;
            Projectile.minionSlots = 2f;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanCutTiles() => false;
        public override bool MinionContactDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            if (!CheckActive(player))
            {
                return;
            }

            // Capture actual physics velocity from last frame before AI updates it
            float oldVelocityX = Projectile.velocity.X;

            // Fall through platforms if the player is below us (default idle/return behavior)
            Projectile.shouldFallThrough = player.position.Y + player.height > Projectile.position.Y + Projectile.height;

            // Emit yellow light
            Lighting.AddLight(Projectile.Center, 0.8f, 0.75f, 0.4f);

            float distToPlayer = Vector2.Distance(Projectile.Center, player.Center);
            if (isReturning)
            {
                if (distToPlayer <= ReturnStopDistance)
                {
                    isReturning = false;
                }
            }
            else
            {
                if (distToPlayer > ReturnStartDistance)
                {
                    isReturning = true;
                }
            }

            NPC targetNPC = null;
            bool targetVisible = false;
            if (distToPlayer < HardLeashDistance)
            {
                targetNPC = FindTarget(player, out targetVisible);
            }

            // A target the archer can already shoot interrupts the walk back: it stops and fires instead of
            // walking past it. Only unseen targets wait until the archer is back near the owner.
            bool shootableNow = targetVisible && Vector2.Distance(targetNPC.Center, Projectile.Center) <= AimStartRange;
            bool returningToPlayer = isReturning && !shootableNow;
            bool isTryingToMove = false;
            bool firedThisTick = false;

            if (returningToPlayer)
            {
                isAiming = false;
                isBackingAway = false;
                aimWindupTimer = 0;
                isTryingToMove = true;

                bool navigating = SmartProjectileFighterAI.Run(Projectile, player, topSpeed: 4.1f, acceleration: 0.5f, navSearchRadius: ArcherNavSearchRadius, gravity: 0.4f);
                if (!navigating)
                {
                    // Move towards player horizontally (slower returning speed: 2.4f)
                    float horizDistToPlayer = Math.Abs(player.Center.X - Projectile.Center.X);
                    if (horizDistToPlayer > 12f)
                    {
                        int dir = player.Center.X > Projectile.Center.X ? 1 : -1;
                        Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, dir * 2.4f, 0.1f);
                        Projectile.spriteDirection = dir;
                    }
                    else
                    {
                        Projectile.velocity.X *= 0.8f;
                    }

                    if (TileCollisionInFront())
                    {
                        Projectile.velocity.Y = -7f;
                    }
                }
            }
            else
            {
                bool foundTarget = targetNPC != null;
                Vector2 targetCenter = foundTarget ? targetNPC.Center : Vector2.Zero;
                float distToTarget = foundTarget ? Vector2.Distance(targetCenter, Projectile.Center) : AimStopRange;

                // Update sticky target tracker
                if (foundTarget && targetNPC != null)
                {
                    lastTargetWhoAmI = targetNPC.whoAmI;
                    // Fall through platforms if the target is below us
                    Projectile.shouldFallThrough = targetNPC.position.Y + targetNPC.height > Projectile.position.Y + Projectile.height;
                }
                else
                {
                    lastTargetWhoAmI = -1;
                }

                if (foundTarget && targetNPC != null)
                {
                    int targetDir = targetCenter.X > Projectile.Center.X ? 1 : -1;

                    // Deadzone of 16 pixels to prevent rapid left-right wiggling/flipping when directly overlapping horizontally
                    float horizDist = Math.Abs(targetCenter.X - Projectile.Center.X);
                    if (horizDist > 16f)
                    {
                        Projectile.spriteDirection = targetDir;
                    }

                    bool canSee = targetVisible;

                    // Update aiming and backing-away states with hysteresis
                    if (!canSee)
                    {
                        isAiming = false;
                        isBackingAway = false;
                    }
                    else
                    {
                        // Hysteresis for aiming
                        if (isAiming)
                        {
                            if (distToTarget > AimStopRange)
                            {
                                isAiming = false;
                            }
                        }
                        else
                        {
                            if (distToTarget <= AimStartRange)
                            {
                                isAiming = true;
                            }
                        }

                        // Hysteresis for backing away
                        if (isBackingAway)
                        {
                            if (distToTarget > 220f)
                            {
                                isBackingAway = false;
                            }
                        }
                        else
                        {
                            if (distToTarget < 140f)
                            {
                                isBackingAway = true;
                            }
                        }
                    }

                    if (!isAiming)
                    {
                        aimWindupTimer = 0;
                        isTryingToMove = true;

                        bool navigating = SmartProjectileFighterAI.Run(Projectile, targetNPC, topSpeed: 2.4f, acceleration: 0.1f, navSearchRadius: ArcherNavSearchRadius, gravity: 0.4f);
                        if (!navigating)
                        {
                            // Walk towards target to close distance (slower walk speed: 2.4f)
                            float horizDistToTarget = Math.Abs(targetCenter.X - Projectile.Center.X);
                            if (horizDistToTarget > 12f)
                            {
                                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, targetDir * 2.4f, 0.1f);
                            }
                            else
                            {
                                Projectile.velocity.X *= 0.8f;
                            }
                            if (TileCollisionInFront())
                            {
                                Projectile.velocity.Y = -7f;
                            }
                        }
                    }
                    else
                    {
                        if (shootCooldown > 0)
                        {
                            shootCooldown--;
                            aimWindupTimer = 0;
                        }

                        bool readyToFire = shootCooldown <= 0;
                        if (readyToFire)
                        {
                            // Stop and visibly aim before firing. Backpedaling stays movement-only.
                            Projectile.velocity.X *= 0.65f;
                            if (Math.Abs(Projectile.velocity.X) < 0.1f)
                            {
                                Projectile.velocity.X = 0f;
                            }

                            if (Projectile.velocity.Y == 0f && Math.Abs(Projectile.velocity.X) <= 0.1f)
                            {
                                aimWindupTimer++;
                                if (aimWindupTimer >= AimWindupFrames)
                                {
                                    aimWindupTimer = 0;
                                    shootCooldown = ShotCooldownTicks;
                                    firedThisTick = true;

                                    if (Main.myPlayer == Projectile.owner)
                                    {
                                        // The arrow leaves the drawn bow grip, aimed to intercept: lead the target by the
                                        // flight time and lift the aim point by the arrow's drop after its straight phase.
                                        Vector2 origin = GetBowGrip(Projectile.spriteDirection, _bowAngle);
                                        Vector2 aimAt = targetCenter;

                                        for (int i = 0; i < ArrowInterceptPasses; i++)
                                        {
                                            float flightTicks = Vector2.Distance(origin, aimAt) / ArrowSpeed;
                                            Vector2 lead = new Vector2(targetNPC.velocity.X, targetNPC.velocity.Y * ArrowVerticalLeadFraction) * flightTicks;
                                            float fallingTicks = Math.Max(0f, flightTicks - ArrowGravityDelayTicks);
                                            // Discrete fall: sum of 0.1 * k for k = 1..n.
                                            float gravityDrop = ArrowGravity * fallingTicks * (fallingTicks + 1f) * 0.5f;
                                            aimAt = targetCenter + lead - new Vector2(0f, gravityDrop);
                                        }

                                        Vector2 shotDirection = (aimAt - origin).SafeNormalize(new Vector2(Projectile.spriteDirection, 0f));
                                        Vector2 shootVel = shotDirection * ArrowSpeed;
                                        int damage = CalculateArrowDamage(player);

                                        Projectile.NewProjectile(
                                            Projectile.GetSource_FromThis(),
                                            origin,
                                            shootVel,
                                            ModContent.ProjectileType<ArcherSpiritArrow>(),
                                            damage,
                                            2f,
                                            Projectile.owner
                                        );
                                    }

                                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item5, Projectile.Center);
                                }
                            }
                            else
                            {
                                aimWindupTimer = 0;
                            }
                        }
                        else if (isBackingAway)
                        {
                            // Back away from target, but never far enough to trip the return leash
                            int backDir = -targetDir;
                            float nextDistToPlayer = Vector2.Distance(Projectile.Center + new Vector2(backDir * 4f, 0f), player.Center);

                            if (nextDistToPlayer < ReturnStartDistance)
                            {
                                isTryingToMove = true;

                                // Back away speed (slower: 1.2f)
                                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, backDir * 1.2f, 0.1f);
                                if (TileCollisionInFront())
                                {
                                    Projectile.velocity.Y = -7f;
                                }
                            }
                            else
                            {
                                // Can't back away further without leaving player range, stand still
                                Projectile.velocity.X *= 0.8f;
                            }
                        }
                        else
                        {
                            // In shooting range, not backing away: plant feet
                            Projectile.velocity.X *= 0.8f;
                        }
                    }
                }
                else
                {
                    isAiming = false;
                    isBackingAway = false;
                    aimWindupTimer = 0;

                    // Idle behavior: random pacing / patrol within 20 tiles of player
                    patrolTimer--;
                    if (patrolTimer <= 0)
                    {
                        int roll = Main.rand.Next(3);
                        if (roll == 0)
                        {
                            patrolDirection = 0;
                            patrolTimer = Main.rand.Next(60, 180); // Idle for 1-3 seconds
                        }
                        else
                        {
                            patrolDirection = roll == 1 ? -1 : 1;
                            patrolTimer = Main.rand.Next(60, 120); // Move for 1-2 seconds
                        }
                    }

                    float offsetFromPlayer = Projectile.Center.X - player.Center.X;
                    if (patrolDirection == 1 && offsetFromPlayer > 320f)
                    {
                        patrolDirection = -1;
                    }
                    else if (patrolDirection == -1 && offsetFromPlayer < -320f)
                    {
                        patrolDirection = 1;
                    }

                    if (patrolDirection == 0)
                    {
                        Projectile.velocity.X *= 0.8f;
                    }
                    else
                    {
                        isTryingToMove = true;

                        // Patrol speed (slower: 1.2f)
                        Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, patrolDirection * 1.2f, 0.1f);
                        Projectile.spriteDirection = patrolDirection;

                        if (TileCollisionInFront())
                        {
                            Projectile.velocity.Y = -7f;
                        }
                    }
                }
            }

            // Stuck logic: Check oldVelocityX from the start of the frame so that
            // velocity adjustments made during AI() do not reset the stuckTimer.
            if (isTryingToMove && Math.Abs(oldVelocityX) < 0.1f)
            {
                stuckTimer++;
            }
            else
            {
                stuckTimer = 0;
            }

            if (stuckTimer >= 90) // 1.5 seconds
            {
                stuckTimer = 0;

                // Exiting location dusts
                for (int d = 0; d < 20; d++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 150, default, 1.2f);
                }

                Vector2 oldPos = Projectile.position;

                // Teleport to floor position lookup (within 5-15 tiles of player)
                Vector2 newPos = FindTeleportPosition(player);
                Projectile.position = newPos;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;

                // Arriving location dusts
                for (int d = 0; d < 20; d++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 150, default, 1.2f);
                }

                // Sound effects at old and new pos
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, oldPos);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
            }

            // Apply gravity at the very end of the AI loop unless disabled (e.g. during rope climb)
            if (!SmartProjectileFighterAI.IsGravityDisabled(Projectile))
            {
                Projectile.velocity.Y += 0.4f;
                if (Projectile.velocity.Y > 10f)
                {
                    Projectile.velocity.Y = 10f;
                }
            }

            // ── Pose: the owner publishes its real aim and shot timing; other clients advance the last packet ────
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (isOwner)
            {
                bool wasAiming = _poseAiming;
                _poseAiming = isAiming && targetNPC != null && !returningToPlayer;

                if (_poseAiming)
                {
                    _poseAimDirection = (targetNPC.Center - Projectile.Center).SafeNormalize(new Vector2(Projectile.spriteDirection, 0f));
                }

                _poseWindup = aimWindupTimer;
                _poseCooldown = shootCooldown;

                if (firedThisTick)
                {
                    _shotCount++;
                    _releaseTicks = ReleaseFlickTicks + ReleaseReturnTicks;
                }

                if (firedThisTick || wasAiming != _poseAiming)
                {
                    Projectile.netUpdate = true;
                }
            }
            else if (_poseAiming)
            {
                // Prediction between packets, mirroring the owner: cooldown first, then the windup while planted.
                if (_poseCooldown > 0)
                {
                    _poseCooldown--;
                }
                else if (_poseWindup < AimWindupFrames - 1)
                {
                    _poseWindup++;
                }
            }

            if (Main.dedServ)
            {
                return;
            }

            // ── Rig (visual only): once per tick, never per draw, or high frame rates would speed up the walk ──────
            if (_rig == null)
            {
                _rig = new Player();
                _rig.active = true;
                _rig.whoAmI = 0;
                _rig.gravDir = 1f;
                int[] armorTypes = { ModContent.ItemType<ManhunterHat>(), ModContent.ItemType<ManhunterJerkin>(), ModContent.ItemType<ManhunterBoots>() };

                for (int slot = 0; slot < 3; slot++)
                {
                    _rig.armor[slot] = new Item();
                    _rig.armor[slot].SetDefaults(armorTypes[slot]);
                    _rig.dye[slot] = new Item();
                    _rig.dye[slot].SetDefaults(ItemID.ReflectiveGoldDye);
                }
            }

            int facing = RigFacing;
            _rig.direction = facing;
            _rig.velocity = Projectile.velocity;
            _rig.position = Projectile.Bottom - new Vector2(_rig.width * 0.5f, _rig.height);
            _rig.PlayerFrame();
            // PlayerFrame re-derives the dye shaders; write them back so the whole outfit stays gold.
            _rig.cHead = _rig.dye[0].dye;
            _rig.cBody = _rig.dye[1].dye;
            _rig.cLegs = _rig.dye[2].dye;

            // Bow angle: eases up to the aim (quick) or back down to the waist carry (slower).
            float bowTarget = RestCarryAngle;
            float bowEase = LowerEase;

            if (_poseAiming)
            {
                float facingAimAngle = (float)Math.Atan2(_poseAimDirection.Y, _poseAimDirection.X * facing);
                bowTarget = MathHelper.Clamp(facingAimAngle, -MathHelper.PiOver2, MathHelper.PiOver2);
                bowEase = RaiseEase;
            }

            _bowAngle = MathHelper.Lerp(_bowAngle, bowTarget, bowEase);

            if (_releaseTicks > 0)
            {
                _releaseTicks--;
            }
        }

        // Facing: the synced aim while aiming (so every client turns together), otherwise the local walk direction.
        private int RigFacing
        {
            get
            {
                if (_poseAiming && Math.Abs(_poseAimDirection.X) > 0.05f)
                {
                    return Math.Sign(_poseAimDirection.X);
                }

                if (Projectile.spriteDirection == 0)
                {
                    return 1;
                }

                return Projectile.spriteDirection;
            }
        }

        // World position of the far hand holding the bow at facing-space angle bowAngle. Composite arm rotation: 0 =
        // hanging straight down, so pointing along the bow's aim is (angle - pi/2), mirrored by facing.
        private Vector2 GetBowGrip(int facing, float bowAngle)
        {
            if (_rig == null)
            {
                Vector2 fallbackDirection = new Vector2((float)Math.Cos(bowAngle) * facing, (float)Math.Sin(bowAngle));
                return Projectile.Center + fallbackDirection * 10f;
            }

            float backArmRotation = (bowAngle - MathHelper.PiOver2) * facing;
            return _rig.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, backArmRotation);
        }

        // A bow-sprite texel (facing right) in world space, held at the grip and turned to bowAngle.
        private Vector2 BowTexelToWorld(Vector2 texel, Vector2 grip, int facing, float bowAngle)
        {
            Vector2 offset = texel - BowGripTexel;

            if (facing < 0)
            {
                offset.X = -offset.X;
            }

            return grip + offset.RotatedBy(bowAngle * facing);
        }

        /// <summary>
        /// Picks what to shoot. Anything the archer has a clear line to always beats anything behind a wall, so a
        /// high-HP enemy out of sight can no longer pin it in place while visible enemies go unshot. Order:
        /// the owner's right-click target when visible, then the previous target while it stays visible
        /// (avoids flip-flopping), then the highest-HP visible enemy in range. With nothing visible it walks
        /// toward the nearest unseen enemy near the owner, and switches the moment one comes into view.
        /// </summary>
        private NPC FindTarget(Player owner, out bool visible)
        {
            NPC minionTarget = Projectile.OwnerMinionAttackTargetNPC;
            if (IsCandidate(minionTarget, owner) && CanSee(minionTarget))
            {
                visible = true;
                return minionTarget;
            }

            if (lastTargetWhoAmI >= 0 && lastTargetWhoAmI < Main.maxNPCs)
            {
                NPC previous = Main.npc[lastTargetWhoAmI];
                if (IsCandidate(previous, owner) && CanSee(previous))
                {
                    visible = true;
                    return previous;
                }
            }

            NPC bestVisible = null;
            NPC nearestHidden = null;
            float nearestHiddenDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!IsCandidate(npc, owner))
                {
                    continue;
                }

                if (CanSee(npc))
                {
                    if (bestVisible == null || npc.life > bestVisible.life)
                    {
                        bestVisible = npc;
                    }
                }
                else
                {
                    float distance = Vector2.Distance(npc.Center, Projectile.Center);
                    if (distance < nearestHiddenDistance)
                    {
                        nearestHiddenDistance = distance;
                        nearestHidden = npc;
                    }
                }
            }

            if (bestVisible != null)
            {
                visible = true;
                return bestVisible;
            }

            // The right-click target is still worth walking toward when nothing else can be shot.
            visible = false;
            return IsCandidate(minionTarget, owner) ? minionTarget : nearestHidden;
        }

        private bool IsCandidate(NPC npc, Player owner)
        {
            return npc != null
                && npc.CanBeChasedBy(this)
                && Math.Abs(npc.Center.X - owner.Center.X) < OwnerTargetBox
                && Math.Abs(npc.Center.Y - owner.Center.Y) < OwnerTargetBox
                && Vector2.Distance(npc.Center, Projectile.Center) < AimStopRange;
        }

        private bool CanSee(NPC npc)
        {
            return Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1);
        }

        private Vector2 FindTeleportPosition(Player player)
        {
            for (int attempts = 0; attempts < 30; attempts++)
            {
                int tileOffset = Main.rand.Next(5, 16);
                int side = Main.rand.NextBool() ? -1 : 1;
                int targetTileX = (int)(player.Center.X / 16f) + (tileOffset * side);
                int startTileY = (int)(player.Bottom.Y / 16f);

                // Scan around the player's height level
                for (int yOffset = -5; yOffset <= 5; yOffset++)
                {
                    int testY = startTileY + yOffset;
                    if (WorldGen.InWorld(targetTileX, testY))
                    {
                        Tile floorTile = Main.tile[targetTileX, testY];
                        if (floorTile.HasTile && Main.tileSolid[floorTile.TileType])
                        {
                            Tile space1 = Main.tile[targetTileX, testY - 1];
                            Tile space2 = Main.tile[targetTileX, testY - 2];
                            Tile space3 = Main.tile[targetTileX, testY - 3];

                            bool space1Empty = !space1.HasTile || !Main.tileSolid[space1.TileType];
                            bool space2Empty = !space2.HasTile || !Main.tileSolid[space2.TileType];
                            bool space3Empty = !space3.HasTile || !Main.tileSolid[space3.TileType];

                            if (space1Empty && space2Empty && space3Empty)
                            {
                                return new Vector2(targetTileX * 16f + 8f - Projectile.width / 2f, testY * 16f - Projectile.height);
                            }
                        }
                    }
                }
            }
            return player.Center - new Vector2(Projectile.width / 2f, Projectile.height);
        }

        private bool TileCollisionInFront()
        {
            if (Projectile.velocity.Y != 0f)
            {
                return false;
            }
            // Check tile in front at feet/bottom level (height of block is 16 pixels, check 8 pixels up)
            int checkX = Projectile.spriteDirection < 0 ? -12 : 12;
            Vector2 checkPos = Projectile.Bottom + new Vector2(checkX, -8f);
            Point tilePos = checkPos.ToTileCoordinates();
            return WorldGen.InWorld(tilePos.X, tilePos.Y) && Main.tile[tilePos.X, tilePos.Y].HasTile && Main.tileSolid[Main.tile[tilePos.X, tilePos.Y].TileType];
        }

        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<ArcherSpiritBuff>());
                return false;
            }

            if (owner.HasBuff(ModContent.BuffType<ArcherSpiritBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }

        // Owner -> other clients: the pose state above, in a fixed order.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_poseAiming);
            writer.Write(_poseAimDirection.X);
            writer.Write(_poseAimDirection.Y);
            writer.Write((byte)_poseWindup);
            writer.Write((byte)_poseCooldown);
            writer.Write(_shotCount);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _poseAiming = reader.ReadBoolean();
            _poseAimDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _poseWindup = reader.ReadByte();
            _poseCooldown = reader.ReadByte();
            byte shotCount = reader.ReadByte();

            // A new shot since the last packet: play the release here too.
            if (shotCount != _shotCount)
            {
                _releaseTicks = ReleaseFlickTicks + ReleaseReturnTicks;
            }

            _shotCount = shotCount;
        }

        // The legacy sheet (ArcherSpirit.png) is still autoloaded as the projectile texture but never drawn: the spirit is a
        // player rig drawn through the vanilla player renderer, with its bow added by ArcherSpiritBowLayer.
        public override bool PreDraw(ref Color lightColor)
        {
            if (_rig == null)
            {
                return false;
            }

            int facing = RigFacing;
            float backArmRotation = (_bowAngle - MathHelper.PiOver2) * facing;
            _rig.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);

            // The near hand reaches for one point: the resting bow riser, the drawn string, or the released flick.
            Vector2 grip = GetBowGrip(facing, _bowAngle);
            Vector2 stringPoint = FrontHandTarget(grip, facing);
            Vector2 fromShoulder = stringPoint - _rig.MountedCenter;
            float frontArmRotation = fromShoulder.ToRotation() - MathHelper.PiOver2;
            float reach = fromShoulder.Length();
            Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

            if (reach < 7f)
            {
                stretch = Player.CompositeArmStretchAmount.None;
            }
            else if (reach < 9f)
            {
                stretch = Player.CompositeArmStretchAmount.Quarter;
            }
            else if (reach < 11f)
            {
                stretch = Player.CompositeArmStretchAmount.ThreeQuarters;
            }

            _rig.SetCompositeArmFront(true, stretch, frontArmRotation);
            // The string ends where the arm sprite's hand really is, which can sit a pixel or two off the target.
            _frontHand = _rig.GetFrontHandPosition(stretch, frontArmRotation);

            DrawingSpirit = this;

            try
            {
                Main.PlayerRenderer.DrawPlayer(Main.Camera, _rig, _rig.position, 0f, Vector2.Zero, 0f, 1f);
            }
            finally
            {
                DrawingSpirit = null;
            }

            // Full draw: a soft gold sparkle at the nocked arrowhead.
            bool fullDraw = _poseAiming && _poseCooldown <= 0 && _poseWindup >= PullFullTick && _releaseTicks == 0;

            if (fullDraw && Main.GameUpdateCount % 6 == 0)
            {
                Vector2 aimDirection = new Vector2((float)Math.Cos(_bowAngle) * facing, (float)Math.Sin(_bowAngle));
                Dust sparkle = Dust.NewDustPerfect(grip + aimDirection * 6f, DustID.GoldFlame, Vector2.Zero, 100, default, 0.7f);
                sparkle.noGravity = true;
            }

            return false;
        }

        // Where the near (string) hand is, in world space: at the bow while resting, on the string while drawing (pulled
        // back along the aim by the windup), flicked back on release, then easing back to the bow.
        private Vector2 FrontHandTarget(Vector2 grip, int facing)
        {
            Vector2 topTip = BowTexelToWorld(BowTopTipTexel, grip, facing, _bowAngle);
            Vector2 bottomTip = BowTexelToWorld(BowBottomTipTexel, grip, facing, _bowAngle);
            Vector2 restString = (topTip + bottomTip) * 0.5f;
            Vector2 aimDirection = new Vector2((float)Math.Cos(_bowAngle) * facing, (float)Math.Sin(_bowAngle));

            if (_releaseTicks > ReleaseReturnTicks)
            {
                _releasedStringPoint = restString - aimDirection * (FullPullPixels + 3f) - new Vector2(0f, 3f);
                return _releasedStringPoint;
            }

            if (_releaseTicks > 0)
            {
                float back = 1f - _releaseTicks / (float)ReleaseReturnTicks;
                return Vector2.Lerp(_releasedStringPoint, grip, MathHelper.SmoothStep(0f, 1f, back));
            }

            bool pulling = _poseAiming && _poseCooldown <= 0;

            if (!pulling)
            {
                if (_poseAiming)
                {
                    // Bow raised between shots: hand resting on the string, ready to draw.
                    return restString;
                }

                return grip;
            }

            float pull = MathHelper.Clamp((_poseWindup - PullStartTick) / (float)(PullFullTick - PullStartTick), 0f, 1f);
            Vector2 drawn = restString - aimDirection * (FullPullPixels * pull);

            if (_poseWindup >= PullFullTick)
            {
                // Full-draw tremble, half a pixel either way every other tick.
                float tremble = 0.5f;

                if (Main.GameUpdateCount % 4 < 2)
                {
                    tremble = -0.5f;
                }

                drawn += aimDirection.RotatedBy(MathHelper.PiOver2) * tremble;
            }

            return drawn;
        }

        // Called by ArcherSpiritBowLayer inside DrawPlayer: the bow, its live string and the nocked arrow, all gold.
        internal void AddBowDrawData(ref PlayerDrawSet drawInfo)
        {
            int facing = RigFacing;
            Vector2 grip = GetBowGrip(facing, _bowAngle);
            Color light = Lighting.GetColor(grip.ToTileCoordinates());
            int goldShader = _rig.dye[1].dye;

            Texture2D bow = TextureAssets.Item[ItemID.ShadewoodBow].Value;
            Vector2 origin = BowGripTexel;
            SpriteEffects effects = SpriteEffects.None;

            if (facing < 0)
            {
                origin.X = bow.Width - BowGripTexel.X;
                effects = SpriteEffects.FlipHorizontally;
            }

            DrawData bowData = new DrawData(bow, grip - Main.screenPosition, null, light, _bowAngle * facing, origin, 1f, effects, 0);
            bowData.shader = goldShader;

            // At rest the sprite's own baked string is the string. While the hand holds it, the baked string strip is cut
            // out (PuppetNPC's four-piece limb split, same as the hunter puppets) and a live string is drawn instead.
            bool holdingString = _poseAiming && _poseCooldown <= 0 && _releaseTicks == 0;

            if (!holdingString)
            {
                drawInfo.DrawDataCache.Add(bowData);
                return;
            }

            PuppetNPC.AddBowLimbDraws(drawInfo.DrawDataCache, bowData, bow.Bounds, BowStringTexels);

            // Live string from both limb tips to the near hand. Never bowed outward past rest: early in the pull the hand
            // is still a few px in front of it.
            Vector2 topTip = BowTexelToWorld(BowTopTipTexel, grip, facing, _bowAngle);
            Vector2 bottomTip = BowTexelToWorld(BowBottomTipTexel, grip, facing, _bowAngle);
            Vector2 restString = (topTip + bottomTip) * 0.5f;
            Vector2 stringPoint = _frontHand;
            Vector2 aimDirection = new Vector2((float)Math.Cos(_bowAngle) * facing, (float)Math.Sin(_bowAngle));
            float pulledBack = Vector2.Dot(restString - stringPoint, aimDirection);

            if (pulledBack < 0f)
            {
                stringPoint = restString;
            }

            Color stringColor = SpiritStringColor.MultiplyRGBA(light);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2[] tips = { topTip, bottomTip };

            foreach (Vector2 tip in tips)
            {
                Vector2 span = stringPoint - tip;
                DrawData stringData = new DrawData(pixel, tip - Main.screenPosition, new Rectangle(0, 0, 1, 1), stringColor,
                    span.ToRotation(), new Vector2(0f, 0.5f), new Vector2(span.Length(), BowStringThickness), SpriteEffects.None, 0);
                drawInfo.DrawDataCache.Add(stringData);
            }

            // Nocked frostburn arrow: tail on the string, tip along the bow's aim (vanilla arrow sprites point up).
            Texture2D arrow = TextureAssets.Projectile[ProjectileID.FrostburnArrow].Value;
            float aimRotation = aimDirection.ToRotation();
            DrawData arrowData = new DrawData(arrow, stringPoint - Main.screenPosition, null, light,
                aimRotation + MathHelper.PiOver2, new Vector2(arrow.Width * 0.5f, arrow.Height - 2f), 1f, SpriteEffects.None, 0);
            arrowData.shader = goldShader;
            drawInfo.DrawDataCache.Add(arrowData);
        }
    }

    /// <summary>
    /// Draws the Archer Spirit's bow over its far arm and under its torso, the depth PuppetNPC uses for a back-hand
    /// weapon. Visible only while an ArcherSpirit is inside its own DrawPlayer call (ArcherSpirit.DrawingSpirit).
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class ArcherSpiritBowLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
            => new AfterParent(PlayerDrawLayers.Skin);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => ArcherSpirit.DrawingSpirit != null;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            ArcherSpirit spirit = ArcherSpirit.DrawingSpirit;

            if (spirit == null)
            {
                return;
            }

            spirit.AddBowDrawData(ref drawInfo);
        }
    }
}
