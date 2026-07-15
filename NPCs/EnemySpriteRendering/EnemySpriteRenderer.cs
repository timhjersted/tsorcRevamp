using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.EnemySpriteRendering
{
    public enum EnemySpritePose
    {
        Natural,
        Carry,
        ThrowTelegraph,
        ThrowRelease,
        MagicTelegraph,
        MagicRelease,
        // Retained only for backward compatibility with packet IDs and older callers; treated
        // exactly like MagicTelegraph by the renderer now. New code should not use it.
        MagicAim
    }

    public enum EnemyHeldItemStyle
    {
        Generic,
        Spear,
        Bomb,
        MagicBall
    }

    /// <summary>
    /// Renders a humanoid NPC by driving a hidden puppet <see cref="Player"/> through the
    /// vanilla player draw pipeline. Designed to be reused by every player-rendered enemy
    /// (RedKnightTest, Black Knight, Great Red Knight, eventually Artorias / Gwyn).
    ///
    /// ENTRY POINTS — exactly two from the host NPC:
    /// <list type="bullet">
    /// <item><c>Tick(NPC)</c> — call ONCE at the end of <see cref="ModNPC.AI"/>. Advances
    /// timers/lerps/walk-frames at the 60 Hz game tick.</item>
    /// <item><c>Draw(NPC, sb, screenPos, color)</c> — call from <see cref="ModNPC.PreDraw"/>.
    /// Pure render; never mutates state.</item>
    /// </list>
    ///
    /// POSE MODEL — every tick the host states one natural pose plus optional timed overrides:
    /// <list type="bullet">
    /// <item><c>SetNaturalPose(...)</c> — resting/idle pose. Cleared back to Natural each
    /// Tick(), so the host must re-state it every tick it wants something else.</item>
    /// <item><c>StartActivePose(pose, item, duration, ...)</c> — timed override (throw release,
    /// magic release, etc.). Use <see cref="StartActivePoseAndSync"/> from server-side AI to
    /// also fire the pose on MP clients via packet.</item>
    /// </list>
    ///
    /// ROTATION CONVENTION — <c>weaponRotation</c> is the arm angle as if facing right:
    /// <list type="bullet">
    /// <item>0 = arm horizontal forward (holding weapon out in front)</item>
    /// <item>-π/2 = arm raised straight up overhead</item>
    /// <item>+π/4 = arm forward and 45° below horizontal (typical release pose)</item>
    /// <item>+π/2 = arm hanging straight down</item>
    /// </list>
    /// Standardized windup curve used by Carry → Throw/Magic Telegraph → Release:
    /// <c>0 (forward) → -π/2 (overhead) → +π/4 (release at 45°)</c>.
    /// Body row (Use1-Use4) is derived from this angle each tick so the puppet's arm sprite
    /// matches the held weapon's rotation — matches the approach used in <see cref="Puppets.PuppetNPC"/>.
    /// </summary>
    public class EnemySpriteRenderer
    {
        private const int FrameHeight = 56;
        private const int WeaponAnimMax = 22;

        internal static EnemySpriteRenderer DrawingRendererFor;
        internal static NPC DrawingNPC;

        private readonly int headArmorItemType;
        private readonly int bodyArmorItemType;
        private readonly int legsArmorItemType;
        private readonly Dictionary<int, Item> itemCache = new();
        private readonly Dictionary<string, Texture2D> textureCache = new();

        private Player puppet;
        private Vector2 handleNorm = new(0.10f, 0.85f);

        // Wings — populated lazily on SetWings.
        private int wingsItemType = -1;
        private int wingSlotCached;
        private EnemyFlightController flightRef;

        // ── Natural (resting) pose for THIS tick ────────────────────────────────────
        private EnemySpritePose naturalPose = EnemySpritePose.Natural;
        private int naturalHeldItemType = -1;
        private bool naturalWeaponVisible;
        private EnemyHeldItemStyle naturalHeldStyle = EnemyHeldItemStyle.Generic;
        private string naturalHeldTexturePath;

        // ── Active (timed override) pose ────────────────────────────────────────────
        private EnemySpritePose activePose;
        private int activeTimer;
        private int activeDuration = 1;
        private int activeHeldItemType = -1;
        private bool activeWeaponVisible;
        private EnemyHeldItemStyle activeHeldStyle = EnemyHeldItemStyle.Generic;
        private string activeHeldTexturePath;

        // ── Resolved state for current tick (read by Draw) ──────────────────────────
        private EnemySpritePose resolvedPose = EnemySpritePose.Natural;
        private int resolvedHeldItemType = -1;
        private bool resolvedWeaponVisible;
        private EnemyHeldItemStyle resolvedHeldStyle = EnemyHeldItemStyle.Generic;
        private string resolvedHeldTexturePath;
        private int resolvedPoseTimer;
        private int resolvedPoseDuration = 1;

        private EnemySpritePose lastResolvedPose = EnemySpritePose.Natural;
        private int lastResolvedHeldItemType = -1;

        private float frameCounter;
        private float weaponRotation;
        private int weaponAnim;

        // ── Standardized windup constants ───────────────────────────────────────────
        // ArmForward: holding weapon out in front (start of Carry, start of Telegraph).
        // ArmOverhead: peak windup (end of Telegraph, start of Release).
        // ArmReleaseEnd: 45° below horizontal — end of the release arc.
        private const float ArmForward     =  0f;
        private const float ArmOverhead    = -MathHelper.PiOver2;   // -π/2
        private const float ArmReleaseEnd  =  MathHelper.PiOver4;   // +π/4 = 45° down-forward

        internal Color LayerDrawColor { get; private set; }

        public EnemySpriteRenderer(int headArmorItemType, int bodyArmorItemType, int legsArmorItemType)
        {
            this.headArmorItemType = headArmorItemType;
            this.bodyArmorItemType = bodyArmorItemType;
            this.legsArmorItemType = legsArmorItemType;
        }

        public void SetHandleNorm(Vector2 value) => handleNorm = value;

        public void SetWings(int wingsItemType, EnemyFlightController flight = null)
        {
            this.wingsItemType = wingsItemType;
            this.flightRef = flight;
            if (wingsItemType > 0)
            {
                var probe = new Item();
                probe.SetDefaults(wingsItemType);
                wingSlotCached = probe.wingSlot;
            }
            else
            {
                wingSlotCached = 0;
            }
            if (puppet != null)
            {
                puppet.wings = wingSlotCached;
                puppet.wingTimeMax = 200;
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Tick-rate API (called from ModNPC.AI)
        // ────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Declare the natural / resting pose for THIS AI tick. Re-state every tick the host
        /// wants something other than <see cref="EnemySpritePose.Natural"/>; the renderer
        /// clears back to natural-idle automatically each tick.
        /// </summary>
        public void SetNaturalPose(EnemySpritePose pose,
                                   int heldItemType = -1,
                                   bool weaponVisible = false,
                                   EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic,
                                   string heldTexturePath = null)
        {
            naturalPose = pose;
            naturalHeldItemType = heldItemType;
            naturalWeaponVisible = weaponVisible && heldItemType > 0;
            naturalHeldStyle = heldItemStyle;
            naturalHeldTexturePath = heldTexturePath;
        }

        /// <summary>
        /// LOCAL-ONLY active pose. Called by the packet handler on MP clients receiving a
        /// <see cref="tsorcPacketID.SyncEnemyActivePose"/> packet. AI code on the server /
        /// in single-player should call <see cref="StartActivePoseAndSync"/> instead.
        /// </summary>
        public void StartActivePose(EnemySpritePose pose,
                                    int heldItemType,
                                    int durationTicks,
                                    bool weaponVisible = true,
                                    EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic,
                                    string heldTexturePath = null,
                                    float? spearTargetRotation = null) // kept for packet compat; unused
        {
            _ = spearTargetRotation; // explicit ignore — kept in signature so packet decoder still passes it
            activePose = pose;
            activeTimer = Math.Max(durationTicks, 1);
            activeDuration = Math.Max(durationTicks, 1);
            activeHeldItemType = heldItemType;
            activeWeaponVisible = weaponVisible && heldItemType > 0;
            activeHeldStyle = heldItemStyle;
            activeHeldTexturePath = heldTexturePath;
        }

        /// <summary>
        /// Server / single-player wrapper around <see cref="StartActivePose"/> that ALSO
        /// broadcasts a <see cref="tsorcPacketID.SyncEnemyActivePose"/> packet so MP
        /// clients fire the same active pose locally. The host <see cref="ModNPC"/>
        /// must implement <see cref="IEnemySpriteRendererHost"/> for clients to locate
        /// the renderer when the packet arrives.
        /// </summary>
        public void StartActivePoseAndSync(NPC npc,
                                           EnemySpritePose pose,
                                           int heldItemType,
                                           int durationTicks,
                                           bool weaponVisible = true,
                                           EnemyHeldItemStyle heldItemStyle = EnemyHeldItemStyle.Generic,
                                           string heldTexturePath = null)
        {
            StartActivePose(pose, heldItemType, durationTicks, weaponVisible, heldItemStyle, heldTexturePath);

            if (Main.netMode != NetmodeID.Server || npc == null)
            {
                return;
            }

            var packet = ModContent.GetInstance<tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.SyncEnemyActivePose);
            packet.Write(npc.whoAmI);
            packet.Write((byte)pose);
            packet.Write(heldItemType);
            packet.Write((short)durationTicks);
            packet.Write(weaponVisible);
            packet.Write((byte)heldItemStyle);
            packet.Write(heldTexturePath ?? string.Empty);
            // Kept for packet compatibility — no longer used by the renderer.
            packet.Write(false);
            packet.Send();
        }

        public void Tick(NPC npc)
        {
            // 1. Resolve effective pose for this tick.
            if (activeTimer > 0)
            {
                resolvedPose = activePose;
                resolvedHeldItemType = activeHeldItemType;
                resolvedWeaponVisible = activeWeaponVisible;
                resolvedHeldStyle = activeHeldStyle;
                resolvedHeldTexturePath = activeHeldTexturePath;
                resolvedPoseTimer = activeTimer;
                resolvedPoseDuration = activeDuration;
                activeTimer--;
            }
            else
            {
                resolvedPose = naturalPose;
                resolvedHeldItemType = naturalHeldItemType;
                resolvedWeaponVisible = naturalWeaponVisible;
                resolvedHeldStyle = naturalHeldStyle;
                resolvedHeldTexturePath = naturalHeldTexturePath;
                resolvedPoseTimer = 0;
                resolvedPoseDuration = 1;
            }

            // 2. Reseed weaponAnim on every entry to a swing/release pose so the vanilla
            //    item-use arc plays from frame 0.
            bool poseChanged = resolvedPose != lastResolvedPose
                            || resolvedHeldItemType != lastResolvedHeldItemType;
            if (poseChanged && IsArmAnimatedPose(resolvedPose))
            {
                weaponAnim = WeaponAnimMax;
            }
            if (weaponAnim > 0)
            {
                weaponAnim--;
            }

            // 3. Lerp weapon rotation toward target for the current pose.
            TickWeaponRotation();

            // 4. Walk-cycle frame counter — 60 Hz now.
            bool onGround = npc.velocity.Y == 0f;
            bool moving = Math.Abs(npc.velocity.X) > 0.15f;
            if (onGround && moving)
            {
                frameCounter += Math.Abs(npc.velocity.X) * 0.55f;
                if (frameCounter >= 14f)
                {
                    frameCounter = 0f;
                }
            }

            lastResolvedPose = resolvedPose;
            lastResolvedHeldItemType = resolvedHeldItemType;

            // 5. Reset natural pose to idle for next tick.
            naturalPose = EnemySpritePose.Natural;
            naturalHeldItemType = -1;
            naturalWeaponVisible = false;
            naturalHeldStyle = EnemyHeldItemStyle.Generic;
            naturalHeldTexturePath = null;
        }

        private static bool IsArmAnimatedPose(EnemySpritePose pose)
        {
            return pose == EnemySpritePose.Carry
                || pose == EnemySpritePose.ThrowTelegraph
                || pose == EnemySpritePose.ThrowRelease
                || pose == EnemySpritePose.MagicTelegraph
                || pose == EnemySpritePose.MagicRelease
                || pose == EnemySpritePose.MagicAim;
        }

        private void TickWeaponRotation()
        {
            // Progress 0 → 1 across the active pose's duration. For natural poses
            // (timer = 0) we just lerp toward the target each tick for a smooth approach.
            float progress = resolvedPoseDuration > 0
                ? MathHelper.Clamp(1f - (float)resolvedPoseTimer / resolvedPoseDuration, 0f, 1f)
                : 1f;

            switch (resolvedPose)
            {
                case EnemySpritePose.Carry:
                    // Hold weapon out forward — gentle lerp toward ArmForward each tick.
                    weaponRotation = MathHelper.Lerp(weaponRotation, ArmForward, 0.20f);
                    break;

                case EnemySpritePose.ThrowTelegraph:
                case EnemySpritePose.MagicTelegraph:
                case EnemySpritePose.MagicAim: // legacy — same curve as MagicTelegraph
                    // Wind-up: arm rises from current rotation to fully overhead. For natural
                    // (untimed) telegraphs we lerp smoothly each tick; for timed active
                    // telegraphs we drive directly off the phase progress so the arm reaches
                    // overhead exactly at the end of the phase.
                    if (resolvedPoseTimer > 0)
                    {
                        weaponRotation = MathHelper.Lerp(ArmForward, ArmOverhead, progress);
                    }
                    else
                    {
                        weaponRotation = MathHelper.Lerp(weaponRotation, ArmOverhead, 0.12f);
                    }
                    break;

                case EnemySpritePose.ThrowRelease:
                case EnemySpritePose.MagicRelease:
                    // Release: arm snaps from overhead through forward and finishes 45° down.
                    // First 25% of the duration is the fast forward whip; remaining 75% is the
                    // follow-through hold near the release angle.
                    if (progress < 0.25f)
                    {
                        weaponRotation = MathHelper.Lerp(ArmOverhead, ArmReleaseEnd, progress / 0.25f);
                    }
                    else
                    {
                        weaponRotation = MathHelper.Lerp(ArmReleaseEnd, ArmReleaseEnd + 0.10f, (progress - 0.25f) / 0.75f);
                    }
                    break;

                default: // Natural
                    // Idle drift toward a relaxed slight-down angle.
                    weaponRotation = MathHelper.Lerp(weaponRotation, 0.35f, 0.10f);
                    break;
            }
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Render-rate API (called from ModNPC.PreDraw)
        // ────────────────────────────────────────────────────────────────────────────

        public bool Draw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (puppet == null)
            {
                InitPuppet();
            }

            SyncPuppet(npc);
            LayerDrawColor = drawColor;

            if (Main.netMode != NetmodeID.Server && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                LogDebug(npc);
            }

            DrawingRendererFor = this;
            DrawingNPC = npc;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, puppet, npc.position, 0f, Vector2.Zero);
            DrawingRendererFor = null;
            DrawingNPC = null;

            return false;
        }

        private void InitPuppet()
        {
            puppet = new Player
            {
                active = true,
                whoAmI = 0,
                gravDir = 1f,
                Male = true
            };

            puppet.armor[0] = new Item();
            puppet.armor[0].SetDefaults(headArmorItemType);
            puppet.armor[1] = new Item();
            puppet.armor[1].SetDefaults(bodyArmorItemType);
            puppet.armor[2] = new Item();
            puppet.armor[2].SetDefaults(legsArmorItemType);

            puppet.head = puppet.armor[0].headSlot;
            puppet.body = puppet.armor[1].bodySlot;
            puppet.legs = puppet.armor[2].legSlot;
        }

        // Pure copy from already-ticked state into the puppet.
        private void SyncPuppet(NPC npc)
        {
            puppet.position = npc.position;
            puppet.velocity = npc.velocity;
            puppet.direction = npc.direction;
            puppet.width = npc.width;
            puppet.height = npc.height;
            puppet.gravDir = 1f;
            puppet.selectedItem = 0;
            puppet.itemAnimationMax = WeaponAnimMax;

            // Wings
            if (wingsItemType > 0)
            {
                puppet.wings = wingSlotCached;
                if (flightRef != null && flightRef.IsAirborne)
                {
                    puppet.wingTime = puppet.wingTimeMax;
                    puppet.controlJump = flightRef.WingsActiveThisTick;
                    puppet.wingFrame = (int)(flightRef.WingAnimPhase * 4f) % 4;
                }
                else
                {
                    puppet.wingTime = 0;
                    puppet.controlJump = false;
                }
            }

            bool armPoseActive = IsArmAnimatedPose(resolvedPose);
            if (armPoseActive && resolvedHeldItemType > 0)
            {
                puppet.inventory[0] = GetCachedItem(resolvedHeldItemType);
                puppet.itemAnimation = Math.Max(weaponAnim, 1);
                puppet.itemRotation = npc.direction * weaponRotation;
            }
            else
            {
                puppet.inventory[0] = new Item();
                puppet.itemAnimation = 0;
                puppet.itemRotation = 0f;
            }

            SyncFrames(npc);
            // NOTE: no SetCompositeArmFront. We rely on bodyRow Use1–Use4 (set in SyncFrames)
            // to depict the arm pose. The vanilla composite-arm system fights our rotation
            // conventions for non-standard items, and PuppetNPC has shown that bodyRow
            // alone is sufficient for clean arm poses.
        }

        private Item GetCachedItem(int itemType)
        {
            if (!itemCache.TryGetValue(itemType, out Item item))
            {
                item = new Item();
                item.SetDefaults(itemType);
                item.noUseGraphic = true;
                itemCache[itemType] = item;
            }

            return item;
        }

        private void SyncFrames(NPC npc)
        {
            bool onGround = npc.velocity.Y == 0f;
            bool moving = Math.Abs(npc.velocity.X) > 0.15f;

            // Body row priority:
            //   Arm-animated poses (Carry / Telegraph / Release) → derive Use1-Use4 from
            //   the current weaponRotation so the puppet's arm sprite matches what we're
            //   drawing the weapon as. This is the same pitch formula PuppetNPC uses and
            //   is what keeps the arm and weapon from drifting apart visually.
            int bodyRow;
            if (IsArmAnimatedPose(resolvedPose))
            {
                // pitch = 1 at arm-up (rotation -π/2), 0 at arm-down (rotation +π/2), 0.5 at forward.
                float pitch = (1f - (float)Math.Sin(weaponRotation)) / 2f;
                if      (pitch > 0.95f) bodyRow = 1; // Use1 — arm fully up
                else if (pitch > 0.70f) bodyRow = 2; // Use2 — arm raised
                else if (pitch > 0.30f) bodyRow = 3; // Use3 — arm level forward
                else                    bodyRow = 4; // Use4 — arm down-forward
            }
            else if (!onGround)
            {
                bodyRow = 5;
            }
            else if (moving)
            {
                bodyRow = 6 + (int)frameCounter;
            }
            else
            {
                bodyRow = 0;
            }

            int legRow;
            if (!onGround) legRow = 5;
            else if (moving) legRow = 6 + (int)frameCounter;
            else legRow = 0;

            puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            puppet.legFrame  = new Rectangle(0, FrameHeight * legRow,  40, FrameHeight);
        }

        // ────────────────────────────────────────────────────────────────────────────
        // Held-weapon draw (called by EnemyWeaponDrawLayer during DrawPlayer)
        // ────────────────────────────────────────────────────────────────────────────

        // Hand position is looked up FROM THE BODY ROW (Use1–Use4) — these offsets match
        // where the vanilla player arm sprite's tip actually ends up in each Use frame.
        // (Same approach as PuppetNPC.GetHandPosition.) This guarantees the held weapon
        // visually pins to the rendered arm regardless of pitch/rotation math, eliminating
        // the "weapon hovering an inch off the hand" drift that math-derived positions had.
        private Vector2 GetHandPosition(NPC npc)
        {
            int bodyRow = puppet.bodyFrame.Y / FrameHeight;
            Vector2 puppetCenter = npc.position + new Vector2(10f, 20f);

            Vector2 offset = bodyRow switch
            {
                1 => new Vector2(-8f, -9f),  // Use1 — arm fully raised
                2 => new Vector2( 4f, -8f),  // Use2 — arm raised
                3 => new Vector2( 4f,  2f),  // Use3 — arm level forward
                4 => new Vector2( 4f,  7f),  // Use4 — arm dipped forward-down
                5 => new Vector2( 4f,  2f),  // Jump — fall back to level (legs jump independently)
                _ => new Vector2( 4f,  2f),  // Idle / walk — level reach
            };

            return puppetCenter + new Vector2(offset.X * npc.direction, offset.Y);
        }

        internal void DrawWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!resolvedWeaponVisible || resolvedHeldItemType <= 0 || puppet == null)
            {
                return;
            }

            Texture2D texture = GetHeldTexture();
            if (texture == null)
            {
                return;
            }

            NPC npc = DrawingNPC;
            if (npc == null || !npc.active)
            {
                return;
            }

            Vector2 handPosition = GetHandPosition(npc) - Main.screenPosition;
            bool centeredGrip = resolvedHeldStyle == EnemyHeldItemStyle.Bomb
                             || resolvedHeldStyle == EnemyHeldItemStyle.MagicBall;

            // Origin: where on the texture the "grip" sits.
            //   Spear / Bomb / MagicBall — grip at texture CENTER so the held sprite floats
            //   level with the hand and doesn't pivot wildly when the arm raises (the user
            //   doesn't want the spear "flailing" — keep it balanced on the hand).
            //   Generic items — handle-norm corner (Terraria-standard diagonal weapons).
            Vector2 origin;
            if (centeredGrip || resolvedHeldStyle == EnemyHeldItemStyle.Spear)
            {
                origin = texture.Bounds.Center.ToVector2();
            }
            else
            {
                float originX = npc.direction == 1
                    ? texture.Width * handleNorm.X
                    : texture.Width * (1f - handleNorm.X);
                origin = new Vector2(originX, texture.Height * handleNorm.Y);
            }

            SpriteEffects effects = npc.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // Style scale tuned to roughly match the corresponding projectile / item at NPC.scale 1.4.
            //   Spear  0.70 × 1.4 ≈ 0.98 → matches in-flight projectile (was 1.40 = too big)
            //   Bomb   0.58 × 1.4 ≈ 0.81
            //   Ball   0.85 × 0.75 × 1.4 ≈ 0.89
            float scale = resolvedHeldStyle switch
            {
                EnemyHeldItemStyle.Spear => 0.70f,
                EnemyHeldItemStyle.Bomb => 0.58f,
                EnemyHeldItemStyle.MagicBall => 0.85f * 0.75f,
                _ => 1.00f
            };

            // Draw rotation in world space. weaponRotation is "as if facing right".
            // For facing left we negate to mirror the arm angle.
            float worldArmAngle = npc.direction * weaponRotation;
            float rotation;
            if (resolvedHeldStyle == EnemyHeldItemStyle.Spear)
            {
                // Spear is held horizontal regardless of arm angle — gripped at the middle
                // so it stays balanced on the hand throughout the wind-up. (User: "spear
                // should always be horizontal flat while raising and throwing with hand to
                // avoid it flailing around.") The actual thrown projectile rotates with its
                // velocity; the HELD telegraph spear does not.
                // Spear texture points UP at rotation 0; +π/2 makes it point right when
                // facing right, -π/2 makes it point left when facing left.
                rotation = npc.direction == 1 ? MathHelper.PiOver2 : -MathHelper.PiOver2;
            }
            else
            {
                rotation = worldArmAngle;
            }

            drawInfo.DrawDataCache.Add(new DrawData(
                texture,
                handPosition,
                null,
                LayerDrawColor,
                rotation,
                origin,
                npc.scale * scale,
                effects,
                0));
        }

        private Texture2D GetHeldTexture()
        {
            if (!string.IsNullOrEmpty(resolvedHeldTexturePath))
            {
                if (!textureCache.TryGetValue(resolvedHeldTexturePath, out Texture2D texture))
                {
                    texture = ModContent.Request<Texture2D>(resolvedHeldTexturePath).Value;
                    textureCache[resolvedHeldTexturePath] = texture;
                }

                return texture;
            }

            return TextureAssets.Item[resolvedHeldItemType].Value;
        }

        // ── Debug log ────────────────────────────────────────────────────────────────
        // Logs only when something the player can see changes:
        //   pose, held item type, weapon visibility, body row, rotation crossing π/8 bands,
        //   on-ground / airborne, or movement starting / stopping.
        // This compresses the previous frame-by-frame dump into a sparse event timeline
        // that's actually readable when debugging animation issues.
        private EnemySpritePose _logLastPose = (EnemySpritePose)(-1);
        private int _logLastHeldItem = int.MinValue;
        private bool _logLastWeaponVisible;
        private int _logLastBodyRow = -1;
        private int _logLastRotBand = int.MinValue;
        private bool _logLastOnGround;
        private bool _logLastMoving;
        private float _logLastAi1 = float.NaN;
        private float _logLastAi2 = float.NaN;
        private bool _logHeaderWritten;

        private void LogDebug(NPC npc)
        {
            try
            {
                bool onGround = npc.velocity.Y == 0f;
                bool moving = Math.Abs(npc.velocity.X) > 0.15f;
                int bodyRow = puppet != null ? puppet.bodyFrame.Y / FrameHeight : -1;
                // Quantise rotation to 8 bands (π/8 each) so small lerps don't spam the log.
                int rotBand = (int)Math.Round(weaponRotation / (MathHelper.Pi / 8f));
                // Log ai[1]/ai[2] when they cross "interesting" boundaries (multiples of 30
                // is enough granularity for the RedKnight band system without flooding).
                int ai1Band = (int)(npc.ai[1] / 30f);
                int ai2Band = (int)(npc.ai[2] / 30f);
                int lastAi1Band = float.IsNaN(_logLastAi1) ? int.MinValue : (int)(_logLastAi1 / 30f);
                int lastAi2Band = float.IsNaN(_logLastAi2) ? int.MinValue : (int)(_logLastAi2 / 30f);

                bool changed = resolvedPose != _logLastPose
                            || resolvedHeldItemType != _logLastHeldItem
                            || resolvedWeaponVisible != _logLastWeaponVisible
                            || bodyRow != _logLastBodyRow
                            || rotBand != _logLastRotBand
                            || onGround != _logLastOnGround
                            || moving != _logLastMoving
                            || ai1Band != lastAi1Band
                            || ai2Band != lastAi2Band;

                if (!changed && _logHeaderWritten)
                {
                    return;
                }

                string separator = System.IO.Path.DirectorySeparatorChar.ToString();
                string logDir = Main.SavePath + separator + "Logs";
                System.IO.Directory.CreateDirectory(logDir);
                string logPath = logDir + separator + "tsorcRevamp-redknight.log";

                if (!_logHeaderWritten)
                {
                    string header = "[time] NPC# pose held vis bodyRow legRow rotation(rad/deg) wAnim onGround moving vel ai[1] ai[2] dist";
                    System.IO.File.AppendAllText(logPath, "----- session start " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -----" + Environment.NewLine);
                    System.IO.File.AppendAllText(logPath, header + Environment.NewLine);
                    _logHeaderWritten = true;
                }

                int legRow = puppet != null ? puppet.legFrame.Y / FrameHeight : -1;
                float dist = (npc.target >= 0 && npc.target < Main.maxPlayers)
                    ? npc.Distance(Main.player[npc.target].Center)
                    : -1f;

                string line = $"[{DateTime.Now:HH:mm:ss.fff}] " +
                              $"NPC#{npc.whoAmI} " +
                              $"pose={resolvedPose} " +
                              $"held={resolvedHeldItemType} " +
                              $"vis={(resolvedWeaponVisible ? 1 : 0)} " +
                              $"body={bodyRow} legs={legRow} " +
                              $"rot={weaponRotation:F2}rad/{MathHelper.ToDegrees(weaponRotation):F0}° " +
                              $"wAnim={weaponAnim} " +
                              $"ground={(onGround ? 1 : 0)} moving={(moving ? 1 : 0)} " +
                              $"vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) " +
                              $"ai[1]={npc.ai[1]:F0} ai[2]={npc.ai[2]:F0} dist={dist:F0}";

                System.IO.File.AppendAllText(logPath, line + Environment.NewLine);

                _logLastPose = resolvedPose;
                _logLastHeldItem = resolvedHeldItemType;
                _logLastWeaponVisible = resolvedWeaponVisible;
                _logLastBodyRow = bodyRow;
                _logLastRotBand = rotBand;
                _logLastOnGround = onGround;
                _logLastMoving = moving;
                _logLastAi1 = npc.ai[1];
                _logLastAi2 = npc.ai[2];
            }
            catch
            {
                // Safety: never crash the client due to a logging glitch.
            }
        }
    }
}
