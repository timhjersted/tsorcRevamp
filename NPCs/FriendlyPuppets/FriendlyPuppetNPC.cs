using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Base for player-owned puppet summons (Mimic Tear and later fixed-weapon summons).
    /// Design: tsorcDocs/MimicTear_FriendlyPuppet_Design.md.
    ///
    /// The body is a friendly NPC (server-authoritative movement). Weapon use is NOT re-implemented: on the
    /// owner's client the puppet borrows the owner's own <see cref="Player.ItemCheck"/> through
    /// <see cref="FriendlyPuppetSwap"/>, so every weapon swings, shoots, costs stamina and draws exactly as it
    /// does for the player. A separate display <see cref="Player"/> clone is what actually gets drawn.
    ///
    /// NPC.ai[0] = owner player index (set at spawn, synced by vanilla).
    /// </summary>
    public abstract class FriendlyPuppetNPC : ModNPC
    {
        // Dust-free, sprite-free NPC texture: the visible body is the display clone drawn in PreDraw.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        /// <summary>Vanilla stamina regen applies its gain once every 4th tick (staminaResourceRegenTimer > 3).</summary>
        private const int StaminaRegenTickInterval = 4;

        public int OwnerIndex => (int)NPC.ai[0];

        /// <summary>Drawn instead of the NPC. Built lazily on every client from the owner's resolved equipment.</summary>
        protected Player DisplayClone;

        /// <summary>The puppet's own per-use state, loaded into the owner for the duration of each swap.</summary>
        internal FriendlyPuppetUseState UseState;

        /// <summary>The puppet's own stamina pool. Max/regen are copied from the owner; spending happens
        /// through the owner's real stamina code while the pool is swapped in.</summary>
        internal float StaminaCurrent;
        internal bool StaminaInitialized;
        private int _staminaRegenDelay;
        private int _staminaRegenTimer;

        /// <summary>The puppet's own mana pool (max = the owner's statManaMax2). Weapons spend it through the
        /// owner's real mana code while it's swapped into owner.statMana. Owner-client only.</summary>
        internal int ManaCurrent;
        private int _manaRegenDelay;
        private float _manaRegenCount;

        /// <summary>Vanilla Player.UpdateManaRegen adds manaRegen to manaRegenCount each tick and grants 1 mana
        /// per 120 accumulated.</summary>
        private const float VanillaManaRegenCountPerPoint = 120f;

        /// <summary>The item the puppet is holding right now. Null or air = nothing to use.</summary>
        protected abstract Item ActiveWeapon { get; }

        /// <summary>Read access for the projectile-AI swap, which runs outside this class.</summary>
        internal Item CurrentWeapon => ActiveWeapon;

        /// <summary>
        /// Owner-client only, once per tick. Return true to hold "use" this tick and set where it aims.
        /// Runs before the swap, so <see cref="Main.MouseWorld"/> here is still the owner's real cursor.
        /// </summary>
        protected abstract bool ChooseAttack(Player owner, out Vector2 aimWorld);

        /// <summary>
        /// Fraction of the owner's own damage the puppet deals with a weapon (applied in
        /// <see cref="FriendlyPuppetGlobalItem.ModifyWeaponDamage"/>). ESTIMATE — first-pass value, tune from play.
        /// Stamina already caps how often it attacks, so its real output lands below this share of the player's.
        /// </summary>
        public virtual float DamageShare => 0.5f;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true });
        }

        public override void SetDefaults()
        {
            // Player-sized hitbox so the display clone's feet sit on NPC.Bottom.
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.friendly = true;
            NPC.dontTakeDamage = true;
            NPC.immortal = true;
            NPC.lifeMax = 100;
            NPC.damage = 0;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 0f;
            NPC.netAlways = true;
        }

        public override bool CheckActive()
        {
            // Lifetime is owned by the summon (owner leaving / dismissal), never by vanilla's off-screen despawn.
            return false;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            return false;
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            // Player weapons never hit a friendly puppet. Hostile projectiles fall through to vanilla, which hits
            // friendly NPCs (Projectile.Damage: npc.friendly && !dontTakeDamageFromHostiles) — that only matters
            // for subclasses that turn off dontTakeDamage (the Mimic Tear has HP).
            bool playerProjectile = projectile.friendly && !projectile.hostile;

            if (playerProjectile)
            {
                return false;
            }

            return null;
        }

        /// <summary>Ticks before a decoyed enemy's redirected hit can land again (FriendlyPuppetAggroPlayer). Per machine.</summary>
        internal int DecoyHitCooldown;

        public override void AI()
        {
            if (DecoyHitCooldown > 0)
            {
                DecoyHitCooldown--;
            }

            // Owner left the world: despawn (server decides; vanilla syncs the deactivation).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

                if (!ownerIndexValid || !Main.player[OwnerIndex].active)
                {
                    NPC.active = false;

                    // A deactivated NPC never reaches the netUpdate send, so sync the removal directly.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }
            }
        }

        /// <summary>
        /// Owner-client only. Called from <see cref="FriendlyPuppetOwnerPlayer.PostUpdate"/>, i.e. after the
        /// owner's own ItemCheck and before anything is drawn.
        /// </summary>
        internal void RunOwnerTick(Player owner)
        {
            EnsureDisplayClone(owner);

            tsorcRevampStaminaPlayer ownerStamina = owner.GetModPlayer<tsorcRevampStaminaPlayer>();

            if (!StaminaInitialized)
            {
                StaminaCurrent = ownerStamina.staminaResourceMax2;
                ManaCurrent = owner.statManaMax2;
                StaminaInitialized = true;
            }

            // Captured BEFORE the subclass hook so stamina it spends there (the Mimic Tear's dodge roll) also
            // counts as "spent this tick" and pauses regen, exactly like a weapon swing.
            float staminaBefore = StaminaCurrent;

            UpdateOwnerSide(owner);

            // Between uses the puppet faces the way it moves (NPC.direction, set by the movement AI); during a
            // use, the borrowed ItemCheck turns it toward its aim and the result is kept until the use ends.
            if (UseState.ItemAnimation == 0 && NPC.direction != 0)
            {
                UseState.Direction = NPC.direction;
            }

            // Decide (owner's real cursor is still in place here), then run the borrowed ItemCheck.
            Item weapon = ActiveWeapon;
            bool hasWeapon = weapon != null && !weapon.IsAir;
            Vector2 aimWorld = NPC.Center + new Vector2(NPC.direction * 100f, 0f);
            bool wantsUse = false;

            if (hasWeapon)
            {
                wantsUse = ChooseAttack(owner, out aimWorld);
            }

            int manaBefore = ManaCurrent;

            if (hasWeapon)
            {
                FriendlyPuppetSwap.RunAsPuppet(owner, this, weapon, wantsUse, aimWorld, owner.ItemCheck);
            }

            // Mana regen mirrors the owner: the puppet regenerates at the owner's CURRENT vanilla regen rate, and
            // only while the owner's own regen is running — so the mod's class rules (Bearer of the Curse pinned
            // to zero outside bonfires, Unkindled's weak regen) carry over without being re-implemented. Casting
            // starts the puppet's own vanilla-length delay (owner.maxRegenDelay). APPROXIMATION: the owner casting
            // also pauses the puppet's regen, because the owner's rate reads 0 during the owner's delay.
            bool spentMana = ManaCurrent < manaBefore;

            if (spentMana)
            {
                _manaRegenDelay = (int)owner.maxRegenDelay;
            }
            else if (_manaRegenDelay > 0)
            {
                _manaRegenDelay--;
            }
            else if (owner.manaRegenDelay <= 0f && ManaCurrent < owner.statManaMax2)
            {
                _manaRegenCount += owner.manaRegen;

                while (_manaRegenCount >= VanillaManaRegenCountPerPoint)
                {
                    _manaRegenCount -= VanillaManaRegenCountPerPoint;
                    ManaCurrent = Math.Min(owner.statManaMax2, ManaCurrent + 1);
                }
            }

            // Mimic stamina regen: same "spending pauses regen" rule as the player (SpendRegenDelay),
            // gain = the owner's computed per-interval gain, no regen mid-swing.
            bool spentThisTick = StaminaCurrent < staminaBefore;

            if (spentThisTick)
            {
                _staminaRegenDelay = ownerStamina.SpendRegenDelay;
            }
            else if (_staminaRegenDelay > 0)
            {
                _staminaRegenDelay--;
            }
            else if (UseState.ItemAnimation == 0)
            {
                _staminaRegenTimer++;

                if (_staminaRegenTimer >= StaminaRegenTickInterval)
                {
                    _staminaRegenTimer = 0;
                    StaminaCurrent = Math.Min(ownerStamina.staminaResourceMax2, StaminaCurrent + ownerStamina.staminaResourceGain);
                }
            }

            // Pose the clone from the copied-out use state, then let vanilla pick body/leg frames for it.
            // Velocity drives PlayerFrame's walk/jump frames.
            DisplayClone.position = NPC.position;
            DisplayClone.velocity = NPC.velocity;
            DisplayClone.direction = UseState.Direction;
            DisplayClone.inventory[0] = hasWeapon ? weapon : new Item();
            DisplayClone.selectedItem = 0;
            DisplayClone.lastVisualizedSelectedItem = UseState.LastVisualizedItem ?? DisplayClone.inventory[0];
            DisplayClone.itemAnimation = UseState.ItemAnimation;
            DisplayClone.itemAnimationMax = UseState.ItemAnimationMax;
            DisplayClone.itemTime = UseState.ItemTime;
            DisplayClone.itemTimeMax = UseState.ItemTimeMax;
            DisplayClone.itemRotation = UseState.ItemRotation;
            DisplayClone.itemLocation = UseState.ItemLocation;
            DisplayClone.itemWidth = UseState.ItemWidth;
            DisplayClone.itemHeight = UseState.ItemHeight;
            DisplayClone.compositeFrontArm = UseState.CompositeFrontArm;
            DisplayClone.compositeBackArm = UseState.CompositeBackArm;
            DisplayClone.heldProj = UseState.HeldProj;
            DisplayClone.channel = UseState.Channel;
            DisplayClone.PlayerFrame();
            DecorateDisplayClone(DisplayClone);

            SendOwnerStateIfNeeded();
        }

        /// <summary>Minion slots this puppet occupies on its owner (reserved each tick by
        /// <see cref="FriendlyPuppetOwnerPlayer"/>). 0 = free.</summary>
        public virtual float GetMinionSlotsUsed(Player owner)
        {
            return 0f;
        }

        /// <summary>Owner-client only, once per tick before the attack decision. For owner-side state such as
        /// the mimic's Cerulean drinking (its mana pool lives on the owner's client).</summary>
        protected virtual void UpdateOwnerSide(Player owner)
        {
        }

        /// <summary>Every client, after PlayerFrame has posed the clone: per-subclass overlays such as the flask
        /// in hand and the drinking arm pose.</summary>
        protected virtual void DecorateDisplayClone(Player clone)
        {
        }

        /// <summary>
        /// Builds the display clone once from the owner's RESOLVED equip slots (vanity already applied by
        /// vanilla), then forces every dye shader to Reflective Obsidian and greys out skin/hair/eyes, since
        /// dyes don't reach skin. Frozen after that (design: visuals frozen at summon).
        /// </summary>
        protected void EnsureDisplayClone(Player owner)
        {
            if (DisplayClone != null)
            {
                return;
            }

            Player clone = new Player();
            clone.active = true;
            clone.whoAmI = 0;
            clone.gravDir = 1f;
            clone.Male = owner.Male;
            clone.skinVariant = owner.skinVariant;
            clone.hair = owner.hair;
            clone.hairDye = 0;
            clone.wearsRobe = owner.wearsRobe;

            // Gunmetal body colours: the "liquid metal" read comes from the dye; these stop bare skin and
            // cloth breaking it up.
            Color gunmetalDark = new Color(38, 40, 46);
            Color gunmetalMid = new Color(62, 65, 74);
            clone.skinColor = gunmetalMid;
            clone.hairColor = gunmetalDark;
            clone.eyeColor = new Color(150, 155, 170);
            clone.shirtColor = gunmetalDark;
            clone.underShirtColor = gunmetalMid;
            clone.pantsColor = gunmetalDark;
            clone.shoeColor = gunmetalDark;

            clone.head = owner.head;
            clone.body = owner.body;
            clone.legs = owner.legs;
            clone.handon = owner.handon;
            clone.handoff = owner.handoff;
            clone.back = owner.back;
            clone.front = owner.front;
            clone.shoe = owner.shoe;
            clone.waist = owner.waist;
            clone.shield = owner.shield;
            clone.neck = owner.neck;
            clone.face = owner.face;
            clone.faceHead = owner.faceHead;
            clone.faceFlower = owner.faceFlower;
            clone.balloon = owner.balloon;
            clone.balloonFront = owner.balloonFront;
            clone.backpack = owner.backpack;
            clone.tail = owner.tail;
            clone.beard = owner.beard;
            clone.wings = owner.wings;
            clone.wingTimeMax = Math.Max(1, owner.wingTimeMax);

            int obsidianShader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveObsidianDye);
            clone.cHead = obsidianShader;
            clone.cBody = obsidianShader;
            clone.cLegs = obsidianShader;
            clone.cHandOn = obsidianShader;
            clone.cHandOff = obsidianShader;
            clone.cBack = obsidianShader;
            clone.cFront = obsidianShader;
            clone.cShoe = obsidianShader;
            clone.cWaist = obsidianShader;
            clone.cShield = obsidianShader;
            clone.cShieldFallback = obsidianShader;
            clone.cNeck = obsidianShader;
            clone.cFace = obsidianShader;
            clone.cFaceHead = obsidianShader;
            clone.cFaceFlower = obsidianShader;
            clone.cBalloon = obsidianShader;
            clone.cBalloonFront = obsidianShader;
            clone.cBackpack = obsidianShader;
            clone.cTail = obsidianShader;
            clone.cBeard = obsidianShader;
            clone.cWings = obsidianShader;

            // Dyes only reach equipment; without this, every bit of exposed skin (hands, face, gaps in the armor)
            // draws as bare skin and the copy reads as naked under a dyed shell. skinDyePacked is vanilla's skin
            // shader slot, packed as an ARMOR shader so it's the same Obsidian material as the armor.
            clone.skinDyePacked = PlayerDrawHelper.PackShader(obsidianShader, PlayerDrawHelper.ShaderConfiguration.ArmorShader);

            clone.position = NPC.position;
            clone.direction = NPC.direction == 0 ? 1 : NPC.direction;
            UseState.Direction = clone.direction;
            UseState.HeldProj = -1;

            DisplayClone = clone;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

            if (!ownerIndexValid)
            {
                return false;
            }

            EnsureDisplayClone(Main.player[OwnerIndex]);

            // Non-owner clients never run the swap. They walk the clone from NPC.velocity and, while the owner is
            // streaming a pose (mid-use / drinking), copy that pose exactly — the owner already ran the real
            // ItemCheck + PlayerFrame for it.
            if (OwnerIndex != Main.myPlayer)
            {
                DisplayClone.position = NPC.position;
                DisplayClone.velocity = NPC.velocity;

                if (NPC.direction != 0)
                {
                    DisplayClone.direction = NPC.direction;
                }

                DisplayClone.itemAnimation = 0;
                DisplayClone.heldProj = -1;
                DisplayClone.compositeFrontArm.enabled = false;
                DisplayClone.compositeBackArm.enabled = false;
                DisplayClone.PlayerFrame();

                if (_remotePoseActive)
                {
                    ApplyRemotePose(DisplayClone);
                }

                DecorateDisplayClone(DisplayClone);
            }

            Vector2 drawPosition = DisplayClone.position + DisplayDrawOffset;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, DisplayClone, drawPosition, 0f, Vector2.Zero, 0f, 1f);
            return false;
        }

        /// <summary>Offset the clone is drawn at (the Mimic Tear's rise out of its pool). Position only — the
        /// NPC's hitbox doesn't move.</summary>
        protected virtual Vector2 DisplayDrawOffset => Vector2.Zero;

        // ── Aggro (FriendlyPuppetAggro.cs) ──────────────────────────────────────────────────────────

        /// <summary>Whether enemies may switch their attention to this puppet. Off for invulnerable puppets.</summary>
        public virtual bool DrawsAggro => false;

        /// <summary>Per-moment veto, e.g. while still forming out of its summon pool.</summary>
        public virtual bool CanDrawAggroNow => true;

        // ── Owner → server → other clients: status flags, the streamed pose, subclass extras ────────

        /// <summary>Owner-side status bits the SERVER needs for decisions it can't make itself (the owner holds the
        /// stamina/mana pools). Meaning is defined by the subclass. Set on the owner client; in single player the
        /// server reads it from this same instance, in multiplayer it arrives via <see cref="ReceiveOwnerState"/>.</summary>
        internal byte OwnerFlags;

        /// <summary>Subclass hook: something worth streaming besides a weapon use (e.g. a Cerulean drink).</summary>
        protected virtual bool HasOwnerActivity => false;

        /// <summary>Subclass hook: extra owner-side state written after the pose. Must be read back in the same order
        /// by <see cref="ReadOwnerExtras"/> — on the server (decisions) and on other clients (display).</summary>
        protected virtual void WriteOwnerExtras(BinaryWriter writer)
        {
        }

        protected virtual void ReadOwnerExtras(BinaryReader reader)
        {
        }

        /// <summary>Owner state packets go out every tick while a pose is active, once more when it ends, when
        /// the flags change, and otherwise every this many ticks.</summary>
        private const int OwnerStateHeartbeatTicks = 60;

        private byte _lastSentFlags;
        private bool _lastSentPoseActive;
        private int _ownerStateHeartbeat;

        // Remote (non-owner) copy of the owner's streamed pose.
        private bool _remotePoseActive;
        private int _remoteDirection;
        private int _remoteItemType;
        private Item _remoteItem;
        private int _remoteItemAnimation;
        private int _remoteItemAnimationMax;
        private float _remoteItemRotation;
        private Vector2 _remoteItemLocationOffset;
        private int _remoteBodyFrameY;
        private Player.CompositeArmData _remoteFrontArm;
        private Player.CompositeArmData _remoteBackArm;
        private int _remoteHeldProjIndex = -1;

        /// <summary>
        /// Non-owner clients: projectile index → (puppet NPC index + 1) for held projectiles a puppet is holding,
        /// so their AI can run attached to the puppet instead of the owner (see FriendlyPuppetSwapSystem).
        /// </summary>
        internal static readonly int[] RemoteHeldProjectilePuppet = new int[Main.maxProjectiles];

        /// <summary>Owner client, multiplayer only: stream status + pose (see <see cref="OwnerStateHeartbeatTicks"/>).</summary>
        private void SendOwnerStateIfNeeded()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool poseActive = UseState.ItemAnimation > 0 || UseState.HeldProj >= 0 || HasOwnerActivity;
            bool flagsChanged = OwnerFlags != _lastSentFlags;
            _ownerStateHeartbeat++;
            bool heartbeatDue = _ownerStateHeartbeat >= OwnerStateHeartbeatTicks;
            bool shouldSend = poseActive || _lastSentPoseActive || flagsChanged || heartbeatDue;

            if (!shouldSend)
            {
                return;
            }

            // The payload goes in a length-prefixed blob, so the server can forward it (or skip it, if the NPC is
            // gone) without understanding the subclass extras.
            MemoryStream payloadStream = new MemoryStream();
            BinaryWriter payload = new BinaryWriter(payloadStream);
            payload.Write(OwnerFlags);
            payload.Write(poseActive);

            if (poseActive)
            {
                Player clone = DisplayClone;
                Vector2 locationOffset = clone.itemLocation - NPC.position;
                int heldProjIdentity = -1;

                if (UseState.HeldProj >= 0 && UseState.HeldProj < Main.maxProjectiles && Main.projectile[UseState.HeldProj].active)
                {
                    heldProjIdentity = Main.projectile[UseState.HeldProj].identity;
                }

                payload.Write((sbyte)clone.direction);
                payload.Write(clone.inventory[0].type);
                payload.Write((short)clone.itemAnimation);
                payload.Write((short)clone.itemAnimationMax);
                payload.Write(clone.itemRotation);
                payload.Write((short)locationOffset.X);
                payload.Write((short)locationOffset.Y);
                payload.Write((short)clone.bodyFrame.Y);
                payload.Write(clone.compositeFrontArm.enabled);
                payload.Write((byte)clone.compositeFrontArm.stretch);
                payload.Write(clone.compositeFrontArm.rotation);
                payload.Write(clone.compositeBackArm.enabled);
                payload.Write((byte)clone.compositeBackArm.stretch);
                payload.Write(clone.compositeBackArm.rotation);
                payload.Write(heldProjIdentity);
            }

            WriteOwnerExtras(payload);
            payload.Flush();
            byte[] payloadBytes = payloadStream.ToArray();

            ModPacket packet = ModContent.GetInstance<FriendlyPuppetSwapSystem>().Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncFriendlyPuppetOwnerState);
            packet.Write((short)NPC.whoAmI);
            packet.Write((ushort)payloadBytes.Length);
            packet.Write(payloadBytes);
            packet.Send();

            _lastSentFlags = OwnerFlags;
            _lastSentPoseActive = poseActive;
            _ownerStateHeartbeat = 0;
        }

        /// <summary>Packet <c>SyncFriendlyPuppetOwnerState</c>: on the server, apply then forward to the other
        /// clients; on a client, apply (flags, pose for display, extras).</summary>
        public static void ReceiveOwnerState(BinaryReader reader, int sender)
        {
            int npcIndex = reader.ReadInt16();
            int length = reader.ReadUInt16();
            byte[] payloadBytes = reader.ReadBytes(length);

            bool npcIndexValid = npcIndex >= 0 && npcIndex < Main.maxNPCs;
            FriendlyPuppetNPC puppet = null;

            if (npcIndexValid && Main.npc[npcIndex].active)
            {
                puppet = Main.npc[npcIndex].ModNPC as FriendlyPuppetNPC;
            }

            // Only the owner may speak for its puppet.
            bool fromOwner = puppet != null && (Main.netMode != NetmodeID.Server || puppet.OwnerIndex == sender);

            if (fromOwner)
            {
                puppet.ApplyOwnerState(payloadBytes);
            }

            if (Main.netMode == NetmodeID.Server && fromOwner)
            {
                ModPacket forward = ModContent.GetInstance<FriendlyPuppetSwapSystem>().Mod.GetPacket();
                forward.Write(tsorcPacketID.SyncFriendlyPuppetOwnerState);
                forward.Write((short)npcIndex);
                forward.Write((ushort)payloadBytes.Length);
                forward.Write(payloadBytes);
                forward.Send(-1, sender);
            }
        }

        private void ApplyOwnerState(byte[] payloadBytes)
        {
            BinaryReader payload = new BinaryReader(new MemoryStream(payloadBytes));
            OwnerFlags = payload.ReadByte();
            bool poseActive = payload.ReadBoolean();

            // Forget the previous held-projectile mapping; a new one is set below if the pose still holds one.
            bool hadHeldProj = _remoteHeldProjIndex >= 0 && _remoteHeldProjIndex < Main.maxProjectiles;

            if (hadHeldProj && RemoteHeldProjectilePuppet[_remoteHeldProjIndex] == NPC.whoAmI + 1)
            {
                RemoteHeldProjectilePuppet[_remoteHeldProjIndex] = 0;
            }

            _remoteHeldProjIndex = -1;
            _remotePoseActive = poseActive;

            if (poseActive)
            {
                _remoteDirection = payload.ReadSByte();
                _remoteItemType = payload.ReadInt32();
                _remoteItemAnimation = payload.ReadInt16();
                _remoteItemAnimationMax = payload.ReadInt16();
                _remoteItemRotation = payload.ReadSingle();
                float offsetX = payload.ReadInt16();
                float offsetY = payload.ReadInt16();
                _remoteItemLocationOffset = new Vector2(offsetX, offsetY);
                _remoteBodyFrameY = payload.ReadInt16();
                bool frontEnabled = payload.ReadBoolean();
                Player.CompositeArmStretchAmount frontStretch = (Player.CompositeArmStretchAmount)payload.ReadByte();
                float frontRotation = payload.ReadSingle();
                bool backEnabled = payload.ReadBoolean();
                Player.CompositeArmStretchAmount backStretch = (Player.CompositeArmStretchAmount)payload.ReadByte();
                float backRotation = payload.ReadSingle();
                int heldProjIdentity = payload.ReadInt32();

                _remoteFrontArm = new Player.CompositeArmData(frontEnabled, frontStretch, frontRotation);
                _remoteBackArm = new Player.CompositeArmData(backEnabled, backStretch, backRotation);

                if (heldProjIdentity >= 0)
                {
                    int projectileIndex = Projectile.GetByUUID(OwnerIndex, heldProjIdentity);
                    bool projectileIndexValid = projectileIndex >= 0 && projectileIndex < Main.maxProjectiles;

                    if (projectileIndexValid)
                    {
                        _remoteHeldProjIndex = projectileIndex;
                        RemoteHeldProjectilePuppet[projectileIndex] = NPC.whoAmI + 1;
                    }
                }
            }

            ReadOwnerExtras(payload);
        }

        /// <summary>Non-owner clients: copy the owner's streamed pose onto the clone after PlayerFrame.</summary>
        private void ApplyRemotePose(Player clone)
        {
            if (_remoteItem == null || _remoteItem.type != _remoteItemType)
            {
                _remoteItem = new Item();

                if (_remoteItemType > 0)
                {
                    _remoteItem.SetDefaults(_remoteItemType);
                }
            }

            clone.direction = _remoteDirection == 0 ? 1 : _remoteDirection;
            clone.inventory[0] = _remoteItem;
            clone.selectedItem = 0;
            clone.lastVisualizedSelectedItem = _remoteItem;
            clone.itemAnimation = _remoteItemAnimation;
            clone.itemAnimationMax = _remoteItemAnimationMax;
            clone.itemRotation = _remoteItemRotation;
            clone.itemLocation = NPC.position + _remoteItemLocationOffset;
            clone.bodyFrame.Y = _remoteBodyFrameY;
            clone.compositeFrontArm = _remoteFrontArm;
            clone.compositeBackArm = _remoteBackArm;
            clone.heldProj = _remoteHeldProjIndex;
        }
    }

    /// <summary>
    /// A friendly puppet's persistent per-use state: everything <see cref="Player.ItemCheck"/> reads or
    /// advances between ticks. Lives on the puppet; loaded into the owner during a swap, copied back after.
    /// </summary>
    internal struct FriendlyPuppetUseState
    {
        public int Direction;
        public int ItemAnimation;
        public int ItemAnimationMax;
        public int ItemTime;
        public int ItemTimeMax;
        public int ReuseDelay;
        public int AttackCD;
        public int ToolTime;
        public float ItemRotation;
        public Vector2 ItemLocation;
        public int ItemWidth;
        public int ItemHeight;
        public bool Channel;
        public int HeldProj;
        public int ItemUsesThisAnimation;
        public Item LastVisualizedItem;
        public Player.CompositeArmData CompositeFrontArm;
        public Player.CompositeArmData CompositeBackArm;

        /// <summary>Last tick's "use" input, so non-autoReuse weapons see a release between uses.</summary>
        public bool PreviousControlUseItem;

        /// <summary>Where the last ItemCheck aimed; reused when the puppet's projectiles run their AI later in the tick.</summary>
        public Vector2 AimWorld;

        /// <summary>The stamina code detects a new use by itemAnimation rising above this (tsorcRevampPlayer.PreItemCheck).</summary>
        public int StaminaOldItemAnimation;
    }

    /// <summary>
    /// Runs a friendly puppet's weapon use through the owner's real <see cref="Player.ItemCheck"/>.
    ///
    /// Vanilla ItemCheck only fully runs for Main.myPlayer (67 gates), so a fake Player can't call it. Instead,
    /// on the owner's client: save the owner's per-use state, load the puppet's, run ItemCheck, copy the result
    /// back to the puppet, restore the owner — all inside one update tick, before draw, so the player never
    /// sees it. <see cref="Active"/> is what every leak guard keys on.
    /// </summary>
    internal static class FriendlyPuppetSwap
    {
        /// <summary>True only for the duration of a borrowed ItemCheck. Guards: ammo/mana (FriendlyPuppetGlobalItem),
        /// packets 13/41 (FriendlyPuppetSwapSystem), projectile tagging, the stamina EMA, and the per-tick
        /// timers in the mod's PreItemCheck hooks (dodge roll, Estus, Cerulean).</summary>
        public static bool Active;

        /// <summary>The puppet whose use is running (for projectile tagging / debugging).</summary>
        public static FriendlyPuppetNPC Current;

        // Player.ItemUsesThisAnimation has a private setter; restoring it needs reflection.
        private static readonly Action<Player, int> SetItemUsesThisAnimation = CreateItemUsesSetter();

        private static Action<Player, int> CreateItemUsesSetter()
        {
            PropertyInfo property = typeof(Player).GetProperty(nameof(Player.ItemUsesThisAnimation));
            MethodInfo setter = property.GetSetMethod(true);
            return (Action<Player, int>)Delegate.CreateDelegate(typeof(Action<Player, int>), setter);
        }

        /// <summary>
        /// Everything about the owner that a borrowed ItemCheck could change, captured before and put back
        /// after. Life/mana/buffs/immunity/lifeSteal are restored wholesale: nothing during a puppet's use may
        /// benefit (or cost) the player — weapon debuffs on enemies are unaffected because they live on the NPC.
        /// </summary>
        private struct OwnerSnapshot
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Direction;
            public float GravDir;
            public int SelectedItem;
            public Item SlotItem;
            public Item LastVisualizedItem;
            public int ItemAnimation;
            public int ItemAnimationMax;
            public int ItemTime;
            public int ItemTimeMax;
            public int ReuseDelay;
            public int AttackCD;
            public int ToolTime;
            public float ItemRotation;
            public Vector2 ItemLocation;
            public int ItemWidth;
            public int ItemHeight;
            public bool Channel;
            public int HeldProj;
            public int ItemUsesThisAnimation;
            public Player.CompositeArmData CompositeFrontArm;
            public Player.CompositeArmData CompositeBackArm;
            public Rectangle BodyFrame;
            public Rectangle LegFrame;
            public Rectangle HeadFrame;
            public bool ControlUseItem;
            public bool ReleaseUseItem;
            public bool ControlUseTile;
            public bool ReleaseUseTile;
            public bool MouseInterface;
            public int StatLife;
            public int StatMana;
            public float ManaRegenDelay;
            public bool Immune;
            public bool ImmuneNoBlink;
            public int ImmuneTime;
            public float LifeSteal;
            public int[] BuffType;
            public int[] BuffTime;
            public float StaminaCurrent;
            public int StaminaOldItemAnimation;
            public Vector2 BroadswordMouseWorld;
            public int MouseX;
            public int MouseY;
            public bool MouseLeft;
            public bool MouseLeftRelease;

            public static OwnerSnapshot Capture(Player owner)
            {
                OwnerSnapshot snapshot = new OwnerSnapshot();
                snapshot.Position = owner.position;
                snapshot.Velocity = owner.velocity;
                snapshot.Direction = owner.direction;
                snapshot.GravDir = owner.gravDir;
                snapshot.SelectedItem = owner.selectedItem;
                snapshot.SlotItem = owner.inventory[0];
                snapshot.LastVisualizedItem = owner.lastVisualizedSelectedItem;
                snapshot.ItemAnimation = owner.itemAnimation;
                snapshot.ItemAnimationMax = owner.itemAnimationMax;
                snapshot.ItemTime = owner.itemTime;
                snapshot.ItemTimeMax = owner.itemTimeMax;
                snapshot.ReuseDelay = owner.reuseDelay;
                snapshot.AttackCD = owner.attackCD;
                snapshot.ToolTime = owner.toolTime;
                snapshot.ItemRotation = owner.itemRotation;
                snapshot.ItemLocation = owner.itemLocation;
                snapshot.ItemWidth = owner.itemWidth;
                snapshot.ItemHeight = owner.itemHeight;
                snapshot.Channel = owner.channel;
                snapshot.HeldProj = owner.heldProj;
                snapshot.ItemUsesThisAnimation = owner.ItemUsesThisAnimation;
                snapshot.CompositeFrontArm = owner.compositeFrontArm;
                snapshot.CompositeBackArm = owner.compositeBackArm;
                snapshot.BodyFrame = owner.bodyFrame;
                snapshot.LegFrame = owner.legFrame;
                snapshot.HeadFrame = owner.headFrame;
                snapshot.ControlUseItem = owner.controlUseItem;
                snapshot.ReleaseUseItem = owner.releaseUseItem;
                snapshot.ControlUseTile = owner.controlUseTile;
                snapshot.ReleaseUseTile = owner.releaseUseTile;
                snapshot.MouseInterface = owner.mouseInterface;
                snapshot.StatLife = owner.statLife;
                snapshot.StatMana = owner.statMana;
                snapshot.ManaRegenDelay = owner.manaRegenDelay;
                snapshot.Immune = owner.immune;
                snapshot.ImmuneNoBlink = owner.immuneNoBlink;
                snapshot.ImmuneTime = owner.immuneTime;
                snapshot.LifeSteal = owner.lifeSteal;
                snapshot.BuffType = (int[])owner.buffType.Clone();
                snapshot.BuffTime = (int[])owner.buffTime.Clone();
                snapshot.StaminaCurrent = owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                snapshot.StaminaOldItemAnimation = owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation;
                snapshot.BroadswordMouseWorld = owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld;
                snapshot.MouseX = Main.mouseX;
                snapshot.MouseY = Main.mouseY;
                snapshot.MouseLeft = Main.mouseLeft;
                snapshot.MouseLeftRelease = Main.mouseLeftRelease;
                return snapshot;
            }

            public void Restore(Player owner)
            {
                owner.position = Position;
                owner.velocity = Velocity;
                owner.direction = Direction;
                owner.gravDir = GravDir;
                owner.selectedItem = SelectedItem;
                owner.inventory[0] = SlotItem;
                owner.lastVisualizedSelectedItem = LastVisualizedItem;
                owner.itemAnimation = ItemAnimation;
                owner.itemAnimationMax = ItemAnimationMax;
                owner.itemTime = ItemTime;
                owner.itemTimeMax = ItemTimeMax;
                owner.reuseDelay = ReuseDelay;
                owner.attackCD = AttackCD;
                owner.toolTime = ToolTime;
                owner.itemRotation = ItemRotation;
                owner.itemLocation = ItemLocation;
                owner.itemWidth = ItemWidth;
                owner.itemHeight = ItemHeight;
                owner.channel = Channel;
                owner.heldProj = HeldProj;
                SetItemUsesThisAnimation(owner, ItemUsesThisAnimation);
                owner.compositeFrontArm = CompositeFrontArm;
                owner.compositeBackArm = CompositeBackArm;
                owner.bodyFrame = BodyFrame;
                owner.legFrame = LegFrame;
                owner.headFrame = HeadFrame;
                owner.controlUseItem = ControlUseItem;
                owner.releaseUseItem = ReleaseUseItem;
                owner.controlUseTile = ControlUseTile;
                owner.releaseUseTile = ReleaseUseTile;
                owner.mouseInterface = MouseInterface;
                owner.statLife = StatLife;
                owner.statMana = StatMana;
                owner.manaRegenDelay = ManaRegenDelay;
                owner.immune = Immune;
                owner.immuneNoBlink = ImmuneNoBlink;
                owner.immuneTime = ImmuneTime;
                owner.lifeSteal = LifeSteal;
                Array.Copy(BuffType, owner.buffType, BuffType.Length);
                Array.Copy(BuffTime, owner.buffTime, BuffTime.Length);
                owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = StaminaCurrent;
                owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation = StaminaOldItemAnimation;
                owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld = BroadswordMouseWorld;
                Main.mouseX = MouseX;
                Main.mouseY = MouseY;
                Main.mouseLeft = MouseLeft;
                Main.mouseLeftRelease = MouseLeftRelease;
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/> with the owner temporarily "being" <paramref name="puppet"/>: holding
        /// <paramref name="weapon"/>, standing at the puppet, aiming at <paramref name="aimWorld"/>. Owner client only.
        /// Two callers: the puppet's per-tick ItemCheck (<see cref="FriendlyPuppetNPC.RunOwnerTick"/>) and the AI of
        /// projectiles the puppet owns (<see cref="FriendlyPuppetSwapSystem"/>) — held-projectile weapons (Phantasm,
        /// spears, yoyos, channelled guns) re-read <c>Main.player[owner]</c> every tick, so their AI needs the same swap.
        /// Slot 0 of the owner's inventory temporarily holds the puppet's weapon (ItemCheck reads the held item from the
        /// inventory) — the finally block guarantees the player's real item goes back even if a weapon's hook throws.
        /// </summary>
        public static void RunAsPuppet(Player owner, FriendlyPuppetNPC puppet, Item weapon, bool wantsUse, Vector2 aimWorld, Action body)
        {
            if (owner.whoAmI != Main.myPlayer || owner.dead || Active)
            {
                return;
            }

            OwnerSnapshot ownerState = OwnerSnapshot.Capture(owner);
            ref FriendlyPuppetUseState useState = ref puppet.UseState;

            Active = true;
            Current = puppet;

            try
            {
                // ── Load the puppet into the owner ─────────────────────────────────────────────
                // Position: ItemCheck places itemLocation, hitboxes, projectiles, sounds and dust relative
                // to the player, so moving the owner onto the puppet is what makes all of those land there.
                owner.position = puppet.NPC.Bottom - new Vector2(owner.width * 0.5f, owner.height);
                owner.velocity = Vector2.Zero;
                owner.direction = useState.Direction == 0 ? 1 : useState.Direction;
                owner.gravDir = 1f;
                owner.selectedItem = 0;
                owner.inventory[0] = weapon;
                owner.lastVisualizedSelectedItem = useState.LastVisualizedItem ?? weapon;
                owner.itemAnimation = useState.ItemAnimation;
                owner.itemAnimationMax = useState.ItemAnimationMax;
                owner.itemTime = useState.ItemTime;
                owner.itemTimeMax = useState.ItemTimeMax;
                owner.reuseDelay = useState.ReuseDelay;
                owner.attackCD = useState.AttackCD;
                owner.toolTime = useState.ToolTime;
                owner.itemRotation = useState.ItemRotation;
                owner.itemLocation = useState.ItemLocation;
                owner.itemWidth = useState.ItemWidth;
                owner.itemHeight = useState.ItemHeight;
                owner.channel = useState.Channel;
                owner.heldProj = useState.HeldProj;
                SetItemUsesThisAnimation(owner, useState.ItemUsesThisAnimation);

                // Use input: hold while wanted; "release" whenever last tick wasn't held, so
                // non-autoReuse weapons can start a new use. Tile/right-click input is always off.
                owner.controlUseItem = wantsUse;
                owner.releaseUseItem = !useState.PreviousControlUseItem;
                owner.controlUseTile = false;
                owner.releaseUseTile = false;
                owner.mouseInterface = false;
                Main.mouseLeft = wantsUse;
                Main.mouseLeftRelease = !useState.PreviousControlUseItem;

                // Aim: ItemCheck and the broadsword rework read the cursor. These are the game's copy of the
                // mouse coordinates, not the OS cursor, and are restored before draw.
                Main.mouseX = (int)(aimWorld.X - Main.screenPosition.X);
                Main.mouseY = (int)(aimWorld.Y - Main.screenPosition.Y);
                owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld = aimWorld;

                // Stamina and mana: the puppet's own pools, charged by the owner's real per-item cost code.
                // (Both are restored to the owner's values by the snapshot afterwards.)
                owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = puppet.StaminaCurrent;
                owner.statMana = puppet.ManaCurrent;
                owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation = useState.StaminaOldItemAnimation;

                // ── Run ───────────────────────────────────────────────────────────────────────
                body();

                // ── Copy the result back to the puppet ────────────────────────────────────────
                useState.Direction = owner.direction;
                useState.ItemAnimation = owner.itemAnimation;
                useState.ItemAnimationMax = owner.itemAnimationMax;
                useState.ItemTime = owner.itemTime;
                useState.ItemTimeMax = owner.itemTimeMax;
                useState.ReuseDelay = owner.reuseDelay;
                useState.AttackCD = owner.attackCD;
                useState.ToolTime = owner.toolTime;
                useState.ItemRotation = owner.itemRotation;
                useState.ItemLocation = owner.itemLocation;
                useState.ItemWidth = owner.itemWidth;
                useState.ItemHeight = owner.itemHeight;
                useState.Channel = owner.channel;
                useState.HeldProj = owner.heldProj;
                useState.ItemUsesThisAnimation = owner.ItemUsesThisAnimation;
                useState.LastVisualizedItem = owner.lastVisualizedSelectedItem;
                useState.CompositeFrontArm = owner.compositeFrontArm;
                useState.CompositeBackArm = owner.compositeBackArm;
                useState.PreviousControlUseItem = wantsUse;
                useState.AimWorld = aimWorld;
                useState.StaminaOldItemAnimation = owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation;
                puppet.StaminaCurrent = owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                puppet.ManaCurrent = owner.statMana;
            }
            finally
            {
                ownerState.Restore(owner);
                Active = false;
                Current = null;
            }
        }
    }

    /// <summary>Drives the owner's puppets once per tick, after the owner's own ItemCheck, before draw.</summary>
    public class FriendlyPuppetOwnerPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.ModNPC is not FriendlyPuppetNPC puppet)
                {
                    continue;
                }

                if (puppet.OwnerIndex != Player.whoAmI)
                {
                    continue;
                }

                // Minion slots: vanilla zeroes slotsMinions late in Player.Update (just before ItemCheck) and each
                // minion projectile re-adds its own slots when IT updates, killing itself if there's no room. A
                // puppet is an NPC, so its owner reserves its slots here — after the reset, before the projectiles
                // run — and ordinary minions then fit themselves into what's left.
                Player.slotsMinions += puppet.GetMinionSlotsUsed(Player);

                puppet.RunOwnerTick(Player);
            }
        }
    }

    /// <summary>
    /// Multiplayer leak guard. Aimed shots (useStyle 5) send packet 13 (owner position + controls) and 41
    /// (item animation) for the owner from INSIDE ItemCheck_Shoot. During a swap those would show the owner
    /// jumping to the puppet and aiming for a frame on other screens, so they're dropped; the owner's real
    /// state syncs normally after the restore. Any other self-packet sent mid-swap is logged once.
    /// </summary>
    public class FriendlyPuppetSwapSystem : ModSystem
    {
        private static readonly bool[] LoggedPacketTypes = new bool[256];

        public override void Load()
        {
            On_NetMessage.SendData += FilterSwapPackets;
            On_Projectile.AI += RunPuppetProjectileAI;
        }

        /// <summary>
        /// Held-projectile weapons (Phantasm, spears, yoyos, channelled guns) re-attach to <c>Main.player[owner]</c>,
        /// check <c>player.channel</c> and aim at the mouse inside their AI every tick. For a projectile the puppet owns,
        /// that AI runs inside the same swap as the puppet's ItemCheck, so it follows the puppet instead of the player.
        /// Wraps <see cref="Projectile.AI"/> (PreAI + AI + PostAI) so the restore can't be skipped.
        /// </summary>
        private static void RunPuppetProjectileAI(On_Projectile.orig_AI orig, Projectile projectile)
        {
            bool ownedByLocalPlayer = projectile.owner == Main.myPlayer && !Main.dedServ;

            // ── Other clients: a held projectile the owner's pose stream says a puppet is holding ──
            // Its AI here only needs to stay attached to the puppet for display, so a light swap of the owner's
            // position/facing/use flags is enough (the real swap only exists on the owner's client).
            int remotePuppetSlot = 0;

            if (!ownedByLocalPlayer && !Main.dedServ)
            {
                remotePuppetSlot = FriendlyPuppetNPC.RemoteHeldProjectilePuppet[projectile.whoAmI];
            }

            if (remotePuppetSlot > 0)
            {
                NPC remotePuppetNpc = Main.npc[remotePuppetSlot - 1];
                Player remoteOwner = Main.player[projectile.owner];
                // Projectile indices get reused, so also require the same owner as the puppet.
                bool remotePuppetValid = remotePuppetNpc.active
                    && remotePuppetNpc.ModNPC is FriendlyPuppetNPC remotePuppet
                    && remotePuppet.OwnerIndex == projectile.owner;

                if (!remotePuppetValid)
                {
                    FriendlyPuppetNPC.RemoteHeldProjectilePuppet[projectile.whoAmI] = 0;
                    orig(projectile);
                    return;
                }

                Vector2 savedPosition = remoteOwner.position;
                Vector2 savedVelocity = remoteOwner.velocity;
                int savedDirection = remoteOwner.direction;
                int savedItemAnimation = remoteOwner.itemAnimation;
                int savedItemTime = remoteOwner.itemTime;
                bool savedChannel = remoteOwner.channel;
                int savedHeldProj = remoteOwner.heldProj;

                try
                {
                    remoteOwner.position = remotePuppetNpc.Bottom - new Vector2(remoteOwner.width * 0.5f, remoteOwner.height);
                    remoteOwner.velocity = remotePuppetNpc.velocity;
                    remoteOwner.direction = remotePuppetNpc.direction == 0 ? 1 : remotePuppetNpc.direction;
                    remoteOwner.itemAnimation = Math.Max(2, remoteOwner.itemAnimation);
                    remoteOwner.itemTime = Math.Max(2, remoteOwner.itemTime);
                    remoteOwner.channel = true;
                    remoteOwner.heldProj = projectile.whoAmI;
                    orig(projectile);
                }
                finally
                {
                    remoteOwner.position = savedPosition;
                    remoteOwner.velocity = savedVelocity;
                    remoteOwner.direction = savedDirection;
                    remoteOwner.itemAnimation = savedItemAnimation;
                    remoteOwner.itemTime = savedItemTime;
                    remoteOwner.channel = savedChannel;
                    remoteOwner.heldProj = savedHeldProj;
                }

                return;
            }

            int puppetIndex = -1;

            if (ownedByLocalPlayer && !FriendlyPuppetSwap.Active)
            {
                puppetIndex = projectile.GetGlobalProjectile<FriendlyPuppetProjectile>().PuppetNpcIndex;
            }

            bool puppetIndexValid = puppetIndex >= 0 && puppetIndex < Main.maxNPCs;
            FriendlyPuppetNPC puppet = null;

            if (puppetIndexValid && Main.npc[puppetIndex].active)
            {
                puppet = Main.npc[puppetIndex].ModNPC as FriendlyPuppetNPC;
            }

            if (puppet == null || puppet.OwnerIndex != projectile.owner)
            {
                orig(projectile);
                return;
            }

            Item weapon = puppet.CurrentWeapon;
            Player owner = Main.player[projectile.owner];

            // RunAsPuppet refuses to swap a dead owner, and would then skip the body — the AI must still run.
            if (weapon == null || weapon.IsAir || owner.dead)
            {
                orig(projectile);
                return;
            }

            // Same input the puppet's ItemCheck used this tick, so channel stays held while it's attacking.
            bool wantsUse = puppet.UseState.PreviousControlUseItem;
            Vector2 aimWorld = puppet.UseState.AimWorld;
            FriendlyPuppetSwap.RunAsPuppet(owner, puppet, weapon, wantsUse, aimWorld, () => orig(projectile));
        }

        private static void FilterSwapPackets(On_NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            bool aboutOwner = number == Main.myPlayer;

            if (FriendlyPuppetSwap.Active && aboutOwner)
            {
                bool positionOrAnimationPacket = msgType == MessageID.PlayerControls || msgType == MessageID.ShotAnimationAndSound;

                if (positionOrAnimationPacket)
                {
                    return;
                }

                bool knownType = msgType >= 0 && msgType < LoggedPacketTypes.Length;

                if (knownType && !LoggedPacketTypes[msgType])
                {
                    LoggedPacketTypes[msgType] = true;
                    ModContent.GetInstance<FriendlyPuppetSwapSystem>().Mod.Logger.Warn($"[FriendlyPuppetSwap] packet {msgType} about the owner was sent during a puppet swap (not filtered).");
                }
            }

            orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
    }

    /// <summary>
    /// Puppet-use rules applied while <see cref="FriendlyPuppetSwap.Active"/>: damage = the owner's damage with the
    /// BETTER of their class bonus or their summon bonus, times the puppet's share; ammo is free (ammo TYPE
    /// still comes from the owner's inventory). Projectile damage derives from weapon damage at spawn, so the damage
    /// rule covers melee and projectiles alike.
    /// </summary>
    public class FriendlyPuppetGlobalItem : GlobalItem
    {
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!FriendlyPuppetSwap.Active || FriendlyPuppetSwap.Current == null)
            {
                return;
            }

            // Best-of bonus: a warrior's mimic scales with melee gear, a summoner's mimic swinging the same sword
            // scales with summon gear. Compared as applied totals on the weapon's base damage; the summon side only
            // wins when it is strictly higher, and then lifts the incoming modifier by that ratio (which keeps any
            // item-specific modifiers other hooks already added).
            float baseDamage = Math.Max(1, item.damage);
            float classDamage = player.GetTotalDamage(item.DamageType).ApplyTo(baseDamage);
            float summonDamage = player.GetTotalDamage(DamageClass.Summon).ApplyTo(baseDamage);
            float bonusRatio = 1f;

            if (classDamage > 0f && summonDamage > classDamage)
            {
                bonusRatio = summonDamage / classDamage;
            }

            float share = FriendlyPuppetSwap.Current.DamageShare;
            damage *= bonusRatio * share;
        }

        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!FriendlyPuppetSwap.Active)
            {
                return;
            }

            // Same best-of rule for crit chance. Vanilla summon crit is usually 0, so this rarely changes anything.
            float summonCrit = player.GetTotalCritChance(DamageClass.Summon) + item.crit;

            if (summonCrit > crit)
            {
                crit = summonCrit;
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            if (FriendlyPuppetSwap.Active)
            {
                return false;
            }

            return base.CanConsumeAmmo(weapon, ammo, player);
        }

    }

    /// <summary>
    /// Tags every projectile a puppet creates — directly during a swap, or as a child/on-hit spawn of a
    /// tagged projectile — so later phases can override its damage and keep it out of the player's EMA.
    /// Healing projectiles (vampire/spirit heal) from tagged sources are removed: puppet hits never heal the player.
    /// </summary>
    public class FriendlyPuppetProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool FromFriendlyPuppet;

        /// <summary>NPC index of the puppet this projectile belongs to, or -1. Lets its AI run inside that puppet's
        /// swap. Local to the owner's client (OnSpawn only runs where the projectile was created).</summary>
        public int PuppetNpcIndex = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            bool spawnedDuringSwap = FriendlyPuppetSwap.Active;
            Entity parentEntity = null;

            // EntitySource_OnHit derives from EntitySource_Parent (Entity = the attacker), so this also
            // catches on-hit spawns such as vampire-heal projectiles.
            if (source is EntitySource_Parent parentSource)
            {
                parentEntity = parentSource.Entity;
            }

            FriendlyPuppetProjectile parentTag = null;

            if (parentEntity is Projectile parentProjectile)
            {
                parentTag = parentProjectile.GetGlobalProjectile<FriendlyPuppetProjectile>();
            }

            bool parentIsTagged = parentTag != null && parentTag.FromFriendlyPuppet;

            if (!spawnedDuringSwap && !parentIsTagged)
            {
                return;
            }

            FromFriendlyPuppet = true;

            // Spawned inside a swap → the puppet running it; spawned by a tagged projectile → inherit its puppet.
            if (spawnedDuringSwap && FriendlyPuppetSwap.Current != null)
            {
                PuppetNpcIndex = FriendlyPuppetSwap.Current.NPC.whoAmI;
            }
            else if (parentIsTagged)
            {
                PuppetNpcIndex = parentTag.PuppetNpcIndex;
            }

            bool isHealProjectile = projectile.type == ProjectileID.VampireHeal || projectile.type == ProjectileID.SpiritHeal;

            if (isHealProjectile)
            {
                projectile.active = false;
            }
        }
    }
}
