using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;
using tsorcRevamp.Systems;
using tsorcRevamp.Systems.ArcaneSorcery;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// The Mimic Tear: a reflective copy of its owner that walks with them, fights with their weapons (through the
    /// owner's own ItemCheck — see <see cref="FriendlyPuppetSwap"/>), and has their max HP, defense, stamina, mana,
    /// Estus and Cerulean. Design: tsorcDocs/MimicTear_FriendlyPuppet_Design.md.
    ///
    /// Authority (CLAUDE.md multiplayer rule):
    ///   server  — target pick, active weapon slot, movement (SmartFighter4 waypoint mode), HP, Estus decisions/heals.
    ///   owner   — weapon use, stamina, mana pool, Cerulean (the mana pool only exists on the owner's client).
    ///   clients — the display clone, flask-in-hand and drinking pose, sounds.
    ///
    /// ai[0] = owner, ai[1] = target NPC index (-1 none), ai[2] = active loadout slot, ai[3] = <see cref="MimicState"/>.
    /// </summary>
    public class MimicTear : FriendlyPuppetNPC
    {
        public enum MimicState
        {
            Fight = 0,
            SwitchingWeapon = 1,
            DrinkingEstus = 2,
            /// <summary>Rising out of its liquid-metal pool: invulnerable, no aggro, no moving or attacking.</summary>
            Summoning = 3,
        }

        /// <summary>Loadout slot 0 = the owner's held weapon (tracked live); 1 and 2 = hotbar slots 1 and 2,
        /// snapshotted at summon. Design §5.</summary>
        private const int LoadoutSize = 3;

        // ── Tuning (ESTIMATE — first pass, tune in play) ────────────────────────────────────────
        /// <summary>Lowering one weapon and raising the next: no attacks for this long. Design §5.</summary>
        private const int WeaponSwitchTicks = 20;
        /// <summary>How often the weapon choice is re-evaluated, so it doesn't flap at a range boundary.</summary>
        private const int WeaponChoiceIntervalTicks = 30;
        private const int RetargetIntervalTicks = 15;
        /// <summary>Enemies further than this from the OWNER are ignored: the mimic fights near you, not off-screen.</summary>
        private const float TargetLeashFromOwner = 45f * 16f;
        /// <summary>Enemies further than this from the mimic are not picked up.</summary>
        private const float TargetAcquireRange = 35f * 16f;
        /// <summary>Following: anywhere 6–12 tiles from the owner it stands still; outside that it walks to 9 tiles.</summary>
        private const float FollowNearDistance = 6f * 16f;
        private const float FollowFarDistance = 12f * 16f;
        private const float FollowSettleDistance = 9f * 16f;
        /// <summary>Summoned this far behind the owner (3 tiles), so the rise is in view.</summary>
        private const float SummonOffset = 48f;
        /// <summary>Summon: pool spreads for SummonPoolTicks, then the body rises out of it until SummonTicks (1.5 s).</summary>
        private const int SummonTicks = 90;
        private const int SummonPoolTicks = 24;
        /// <summary>Pool half-width at full spread (px).</summary>
        private const float SummonPoolRadius = 22f;
        /// <summary>Winded below 15% of max stamina; recovered (fights again) at 60%. Hysteresis so it doesn't flicker.</summary>
        private const float WindedEnterFraction = 0.15f;
        private const float WindedExitFraction = 0.6f;
        /// <summary>While winded with a target, hold at least this far from it (25 tiles).</summary>
        private const float WindedBackOffDistance = 25f * 16f;
        /// <summary><see cref="FriendlyPuppetNPC.OwnerFlags"/> bit: the owner-side stamina pool says "winded".</summary>
        private const byte OwnerFlagWinded = 1;
        /// <summary>Past this distance from the owner, teleport back to them (60 tiles).</summary>
        private const float TeleportToOwnerDistance = 60f * 16f;
        /// <summary>Close enough to the engagement spot that it stops walking and just fights.</summary>
        private const float EngagementSlack = 12f;
        /// <summary>Drink Estus below this fraction of max HP.</summary>
        private const float EstusDrinkLifeFraction = 0.4f;
        /// <summary>Drink Cerulean below this fraction of max mana, if the weapon in hand uses mana.</summary>
        private const float CeruleanDrinkManaFraction = 0.3f;
        /// <summary>Drinking slows the player by 10% per tick (tsorcRevampEstusPlayer.UpdateDrinkingEstus); same here.</summary>
        private const float DrinkingVelocityRetention = 0.9f;
        /// <summary>The flask and arm pose appear 5% into the drink, matching the player (UpdateDrinkingEstus).</summary>
        private const float DrinkPoseStartFraction = 0.05f;

        // Engagement bands per weapon kind (pixels): where it likes to stand, and the furthest it will attack from.
        private const float SpearLikePreferred = 80f;
        private const float SpearLikeMaximum = 220f;
        private const float WhipPreferred = 90f;
        private const float WhipMaximum = 170f;
        private const float RangedPreferred = 280f;
        private const float RangedMaximum = 640f;
        private const float SwordBeamMaximum = 400f;
        /// <summary>The movement waypoint is only re-sent to SF4 once the desired spot moves this far (2 tiles).</summary>
        private const float WaypointRefreshDistance = 32f;
        /// <summary>Lead aiming is capped so a slow projectile doesn't aim absurdly far ahead.</summary>
        private const float MaxLeadTicks = 30f;

        /// <summary>The owner's stats when summoned (design: frozen at summon). Synced in SendExtraAI.</summary>
        private MimicStats _stats;
        private int _estusCharges;

        // Server-only.
        private readonly int[] _serverLoadoutTypes = new int[LoadoutSize];
        private Vector2 _heldWaypoint;
        private bool _hasHeldWaypoint;
        private int _retargetTimer;
        private int _weaponChoiceTimer;
        private int _estusHealTicksLeft;
        private float _estusHealPerTick;
        private float _estusHealAccumulator;

        // All machines: local timer for the current state (reset when ai[3] changes), used for drink pose/progress.
        private MimicState _lastSeenState = MimicState.Fight;
        private int _stateTimer;

        // Owner-client only.
        private readonly Item[] _ownerLoadout = new Item[LoadoutSize];
        private bool _ownerLoadoutInitialized;
        private bool _ceruleanInitialized;
        private int _ceruleanCharges;
        private int _ceruleanChargesMax;
        private int _ceruleanDrinkTimer = -1;
        private int _ceruleanDrinkTicks;
        private int _ceruleanRestoreTicksLeft;
        private float _ceruleanManaPerTick;
        private float _ceruleanAccumulator;

        public MimicState State
        {
            get
            {
                return (MimicState)(int)NPC.ai[3];
            }
            set
            {
                NPC.ai[3] = (int)value;
            }
        }

        private int TargetIndex => (int)NPC.ai[1];

        private int ActiveSlot => (int)NPC.ai[2];

        private bool IsDrinkingCerulean => _ceruleanDrinkTimer >= 0;

        protected override Item ActiveWeapon
        {
            get
            {
                bool slotValid = ActiveSlot >= 0 && ActiveSlot < LoadoutSize;

                if (!slotValid)
                {
                    return null;
                }

                return _ownerLoadout[ActiveSlot];
            }
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            // Mortal, unlike the base puppet. HP/defense come from the owner at spawn (SpawnFor / ReceiveExtraAI).
            NPC.dontTakeDamage = false;
            NPC.immortal = false;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 0f;
            NPC.ai[1] = -1f;
        }

        // ── Spawning ────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Owner client: snapshot the owner's stats and ask for a mimic. Single player spawns directly; a
        /// multiplayer client sends the snapshot to the server (Estus numbers aren't synced, so the server can't
        /// read them itself — and clients can't spawn NPCs).
        /// </summary>
        public static void RequestSpawn(Player owner)
        {
            tsorcRevampEstusPlayer estus = owner.GetModPlayer<tsorcRevampEstusPlayer>();

            MimicStats stats = new MimicStats();
            stats.LifeMax = owner.statLifeMax2;
            stats.Defense = owner.statDefense;
            stats.EstusChargesMax = estus.EstusChargesMax;
            stats.EstusHeal = estus.EstusHealthGain + estus.EstusHealthGainBonus;
            stats.EstusDrinkTicks = (int)(estus.EstusDrinkTimerMax * 60f);
            stats.EstusHealTicks = (int)estus.EstusHealingTimerMax;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                SpawnFor(owner.whoAmI, stats);
                return;
            }

            ModPacket packet = ModContent.GetInstance<global::tsorcRevamp.tsorcRevamp>().GetPacket();
            packet.Write(tsorcPacketID.RequestFriendlyPuppetSpawn);
            stats.Write(packet);
            packet.Send();
        }

        /// <summary>Server: a client asked for its mimic (see <see cref="RequestSpawn"/>).</summary>
        public static void ReceiveSpawnRequest(BinaryReader reader, int sender)
        {
            MimicStats stats = MimicStats.Read(reader);

            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }

            SpawnFor(sender, stats);
        }

        private static void SpawnFor(int ownerIndex, MimicStats stats)
        {
            Player owner = Main.player[ownerIndex];
            int mimicType = ModContent.NPCType<MimicTear>();

            // One mimic per owner.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC existing = Main.npc[i];
                bool ownedMimic = existing.active && existing.type == mimicType && (int)existing.ai[0] == ownerIndex;

                if (!ownedMimic)
                {
                    continue;
                }

                existing.active = false;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }

            int spawnX = (int)(owner.Center.X - owner.direction * SummonOffset);
            int spawnY = (int)owner.Bottom.Y;
            int npcIndex = NPC.NewNPC(owner.GetSource_FromThis(), spawnX, spawnY, mimicType, 0, ownerIndex, -1f, 0f, (int)MimicState.Summoning);

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                return;
            }

            NPC npc = Main.npc[npcIndex];
            MimicTear mimic = (MimicTear)npc.ModNPC;
            mimic.ApplyStats(stats);
            npc.life = npc.lifeMax;
            npc.direction = owner.direction;

            // Server's view of the loadout (types only — the owner client holds the real Item clones).
            Item heldItem = owner.HeldItem;
            Item slotOneItem = owner.inventory[0];
            Item slotTwoItem = owner.inventory[1];
            mimic._serverLoadoutTypes[0] = IsUsableWeapon(heldItem, owner) ? heldItem.type : 0;
            mimic._serverLoadoutTypes[1] = IsUsableWeapon(slotOneItem, owner) ? slotOneItem.type : 0;
            mimic._serverLoadoutTypes[2] = IsUsableWeapon(slotTwoItem, owner) ? slotTwoItem.type : 0;
            npc.ai[2] = FirstFilledSlot(mimic._serverLoadoutTypes);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
            }
        }

        private void ApplyStats(MimicStats stats)
        {
            _stats = stats;
            _estusCharges = stats.EstusChargesMax;
            NPC.lifeMax = Math.Max(1, stats.LifeMax);
            NPC.defense = stats.Defense;
            NPC.defDefense = stats.Defense;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            _stats.Write(writer);
            writer.Write((byte)_estusCharges);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            MimicStats stats = MimicStats.Read(reader);
            int estusCharges = reader.ReadByte();
            ApplyStats(stats);
            _estusCharges = estusCharges;
        }

        // ── AI (server decides; every machine advances the local state timer) ──────────────────────

        public override void AI()
        {
            base.AI();

            if (!NPC.active)
            {
                return;
            }

            // Local per-state timer on every machine (drives the drink pose); ai[3] itself is server-synced.
            MimicState state = State;

            if (state != _lastSeenState)
            {
                bool finishedEstus = _lastSeenState == MimicState.DrinkingEstus && !Main.dedServ;
                bool startedSummon = state == MimicState.Summoning && !Main.dedServ;
                bool finishedSummon = _lastSeenState == MimicState.Summoning && !Main.dedServ;

                if (finishedEstus)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item3, NPC.Center);
                }

                if (startedSummon)
                {
                    SoundEngine.PlaySound(SoundID.Splash with { Pitch = -0.6f, Volume = 0.8f }, NPC.Bottom);
                }

                if (finishedSummon)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.4f, Volume = 0.6f }, NPC.Center);
                }

                _lastSeenState = state;
                _stateTimer = 0;
            }
            else
            {
                _stateTimer++;
            }

            // ── Summon: forming out of a liquid-metal pool (every machine) ──
            // Drawn behind tiles, so the ground hides whatever part of the body is still below the surface while
            // DisplayDrawOffset raises it; invulnerable until fully formed.
            bool summoning = state == MimicState.Summoning;
            NPC.dontTakeDamage = summoning;
            NPC.behindTiles = summoning;

            if (summoning && !Main.dedServ)
            {
                // Pool: spreads over SummonPoolTicks, holds, then pulls in over the last quarter as the body leaves it.
                float spread = Math.Min(1f, _stateTimer / (float)SummonPoolTicks);
                float shrinkStart = SummonTicks * 0.75f;
                float shrink = 1f - Math.Clamp((_stateTimer - shrinkStart) / (SummonTicks - shrinkStart), 0f, 1f);
                float poolRadius = SummonPoolRadius * spread * shrink;

                for (int i = 0; i < 3; i++)
                {
                    float poolX = Main.rand.NextFloat(-poolRadius, poolRadius);
                    Vector2 poolPosition = NPC.Bottom + new Vector2(poolX, -2f);
                    Dust poolDust = Dust.NewDustPerfect(poolPosition, DustID.Asphalt, new Vector2(poolX * 0.01f, -0.15f), 0, default, 1.4f);
                    poolDust.noGravity = true;
                }

                // Reflective glints on the pool, and drips running off the body as it rises.
                if (Main.rand.NextBool(4))
                {
                    Vector2 glintPosition = NPC.Bottom + new Vector2(Main.rand.NextFloat(-poolRadius, poolRadius), -3f);
                    Dust glint = Dust.NewDustPerfect(glintPosition, DustID.SilverCoin, Vector2.Zero, 0, default, 0.8f);
                    glint.noGravity = true;
                }

                bool rising = _stateTimer >= SummonPoolTicks;

                if (rising && Main.rand.NextBool(2))
                {
                    float risenHeight = NPC.height - DisplayDrawOffset.Y;
                    Vector2 dripPosition = NPC.Bottom + new Vector2(Main.rand.NextFloat(-NPC.width * 0.5f, NPC.width * 0.5f), -Main.rand.NextFloat(0f, risenHeight));
                    Dust drip = Dust.NewDustPerfect(dripPosition, DustID.Asphalt, new Vector2(0f, 1.5f), 0, default, 1.1f);
                    drip.noGravity = false;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player owner = Main.player[OwnerIndex];

            // Owner died: the mimic goes with them (its lifetime is a buff, cleared on death — design §2).
            if (owner.dead)
            {
                NPC.active = false;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                }

                return;
            }

            if (summoning)
            {
                NPC.velocity.X = 0f;

                if (_stateTimer >= SummonTicks)
                {
                    State = MimicState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            // ── Teleport back if left far behind ──
            float distanceToOwner = Vector2.Distance(NPC.Center, owner.Center);

            if (distanceToOwner > TeleportToOwnerDistance)
            {
                NPC.Bottom = owner.Bottom;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
            }

            // ── Loadout slot 0 follows the owner's held weapon (last valid one kept) ──
            Item heldItem = owner.HeldItem;

            if (IsUsableWeapon(heldItem, owner))
            {
                _serverLoadoutTypes[0] = heldItem.type;
            }

            // ── Target: the owner's minion target first, else the nearest threat near the owner ──
            NPC target = null;
            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxNPCs;

            if (targetIndexValid)
            {
                target = Main.npc[TargetIndex];
            }

            bool targetStillValid = target != null
                && UsefulFunctions.IsHostileThreat(target)
                && Vector2.Distance(target.Center, owner.Center) <= TargetLeashFromOwner;

            if (!targetStillValid)
            {
                target = null;
            }

            _retargetTimer--;

            if (_retargetTimer <= 0)
            {
                _retargetTimer = RetargetIntervalTicks;
                NPC chosenTarget = null;
                int ownerTargetIndex = owner.MinionAttackTargetNPC;
                bool ownerTargetIndexValid = ownerTargetIndex >= 0 && ownerTargetIndex < Main.maxNPCs;

                if (ownerTargetIndexValid)
                {
                    NPC ownerTarget = Main.npc[ownerTargetIndex];
                    bool ownerTargetUsable = UsefulFunctions.IsHostileThreat(ownerTarget)
                        && Vector2.Distance(ownerTarget.Center, owner.Center) <= TargetLeashFromOwner;

                    if (ownerTargetUsable)
                    {
                        chosenTarget = ownerTarget;
                    }
                }

                if (chosenTarget == null)
                {
                    float closestDistance = TargetAcquireRange;

                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC candidate = Main.npc[i];

                        if (!UsefulFunctions.IsHostileThreat(candidate))
                        {
                            continue;
                        }

                        float distanceFromOwner = Vector2.Distance(candidate.Center, owner.Center);

                        if (distanceFromOwner > TargetLeashFromOwner)
                        {
                            continue;
                        }

                        float distanceFromMimic = Vector2.Distance(candidate.Center, NPC.Center);

                        if (distanceFromMimic < closestDistance)
                        {
                            closestDistance = distanceFromMimic;
                            chosenTarget = candidate;
                        }
                    }
                }

                target = chosenTarget;
            }

            int newTargetIndex = -1;

            if (target != null)
            {
                newTargetIndex = target.whoAmI;
            }

            if (newTargetIndex != TargetIndex)
            {
                NPC.ai[1] = newTargetIndex;
                NPC.netUpdate = true;
            }

            // ── Estus: heal-over-time from a finished drink ──
            if (_estusHealTicksLeft > 0)
            {
                _estusHealTicksLeft--;
                _estusHealAccumulator += _estusHealPerTick;
                int wholeHeal = (int)_estusHealAccumulator;

                if (wholeHeal > 0)
                {
                    _estusHealAccumulator -= wholeHeal;
                    NPC.life = Math.Min(NPC.lifeMax, NPC.life + wholeHeal);
                }

                // Life only reaches clients through NPC syncs; every 10 ticks is plenty for a health bar.
                if (_estusHealTicksLeft % 10 == 0)
                {
                    NPC.netUpdate = true;
                }
            }

            // ── State machine ──
            if (state == MimicState.DrinkingEstus)
            {
                NPC.velocity.X *= DrinkingVelocityRetention;

                if (_stateTimer >= _stats.EstusDrinkTicks)
                {
                    _estusCharges--;
                    _estusHealTicksLeft = Math.Max(1, _stats.EstusHealTicks);
                    _estusHealPerTick = _stats.EstusHeal / (float)_estusHealTicksLeft;
                    _estusHealAccumulator = 0f;
                    NPC.HealEffect(_stats.EstusHeal);
                    State = MimicState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (state == MimicState.SwitchingWeapon)
            {
                if (_stateTimer >= WeaponSwitchTicks)
                {
                    State = MimicState.Fight;
                    NPC.netUpdate = true;
                }
            }

            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool canStartEstus = state == MimicState.Fight && lowLife && _estusCharges > 0 && !IsDrinkingCerulean && _estusHealTicksLeft == 0;

            if (canStartEstus)
            {
                State = MimicState.DrinkingEstus;
                NPC.netUpdate = true;
                return;
            }

            // ── Weapon choice: prefer the held weapon (slot 0) whenever it reaches, else any slot that does ──
            _weaponChoiceTimer--;

            if (target != null && state == MimicState.Fight && _weaponChoiceTimer <= 0)
            {
                _weaponChoiceTimer = WeaponChoiceIntervalTicks;
                float targetDistance = Vector2.Distance(NPC.Center, target.Center);
                int bestSlot = -1;

                for (int slot = 0; slot < LoadoutSize; slot++)
                {
                    int itemType = _serverLoadoutTypes[slot];

                    if (itemType <= 0)
                    {
                        continue;
                    }

                    GetEngagementRange(ContentSamples.ItemsByType[itemType], out _, out float slotMaximum);

                    if (targetDistance <= slotMaximum)
                    {
                        bestSlot = slot;
                        break;
                    }
                }

                int currentSlot = ActiveSlot;
                bool currentSlotFilled = currentSlot >= 0 && currentSlot < LoadoutSize && _serverLoadoutTypes[currentSlot] > 0;
                bool currentReaches = false;

                if (currentSlotFilled)
                {
                    GetEngagementRange(ContentSamples.ItemsByType[_serverLoadoutTypes[currentSlot]], out _, out float currentMaximum);
                    currentReaches = targetDistance <= currentMaximum;
                }

                bool betterSlotExists = bestSlot >= 0 && bestSlot != currentSlot;
                bool shouldSwitch = betterSlotExists && (!currentReaches || bestSlot == 0);

                if (!currentSlotFilled)
                {
                    int fallbackSlot = FirstFilledSlot(_serverLoadoutTypes);
                    shouldSwitch = fallbackSlot != currentSlot;
                    bestSlot = fallbackSlot;
                }

                if (shouldSwitch)
                {
                    NPC.ai[2] = bestSlot;
                    State = MimicState.SwitchingWeapon;
                    NPC.netUpdate = true;
                }
            }

            // ── Movement (SmartFighter4 waypoint mode; npc.target is only used for LOS logging there) ──
            NPC.target = OwnerIndex;
            float topSpeed = Math.Max(owner.maxRunSpeed, owner.accRunSpeed);
            float acceleration = owner.runAcceleration;

            if (IsDrinkingCerulean)
            {
                NPC.velocity.X *= DrinkingVelocityRetention;
                return;
            }

            Vector2 waypoint;
            float stopDistance;

            // Winded (owner-side stamina below the threshold, OwnerFlags): back off from the target and let the pool
            // refill, like a player who's run dry. The owner client also refuses attacks while winded (ChooseAttack).
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (target != null)
            {
                // Stand at the weapon's preferred distance (or the back-off distance), on our own side of the target.
                int activeType = 0;
                bool activeSlotValid = ActiveSlot >= 0 && ActiveSlot < LoadoutSize;

                if (activeSlotValid)
                {
                    activeType = _serverLoadoutTypes[ActiveSlot];
                }

                float preferredDistance = RangedPreferred;

                if (activeType > 0)
                {
                    GetEngagementRange(ContentSamples.ItemsByType[activeType], out preferredDistance, out _);
                }

                if (winded)
                {
                    preferredDistance = Math.Max(preferredDistance, WindedBackOffDistance);
                }

                int sideOfTarget = Math.Sign(NPC.Center.X - target.Center.X);

                if (sideOfTarget == 0)
                {
                    sideOfTarget = -owner.direction;
                }

                waypoint = target.Center + new Vector2(sideOfTarget * preferredDistance, 0f);
                stopDistance = EngagementSlack;
            }
            else
            {
                // Follow 6–12 tiles from the owner: anywhere in that band it just stands; outside it, it walks to
                // 9 tiles. It keeps to whichever side it's on, except that when the owner is walking and the mimic
                // is AHEAD of them, it drops back behind.
                float offsetFromOwner = NPC.Center.X - owner.Center.X;
                float gapToOwner = Math.Abs(offsetFromOwner);
                int sideOfOwner = Math.Sign(offsetFromOwner);

                if (sideOfOwner == 0)
                {
                    sideOfOwner = -owner.direction;
                }

                bool ownerWalking = Math.Abs(owner.velocity.X) > 1f;
                bool aheadOfOwner = sideOfOwner == owner.direction;

                if (ownerWalking && aheadOfOwner)
                {
                    sideOfOwner = -owner.direction;
                }

                bool inFollowBand = gapToOwner >= FollowNearDistance
                    && gapToOwner <= FollowFarDistance
                    && Math.Abs(NPC.Center.Y - owner.Center.Y) < FollowFarDistance
                    && sideOfOwner == Math.Sign(offsetFromOwner);

                if (inFollowBand)
                {
                    NPC.velocity.X *= 0.8f;
                    return;
                }

                waypoint = owner.Center + new Vector2(sideOfOwner * FollowSettleDistance, 0f);
                stopDistance = EngagementSlack;
            }

            float horizontalGap = Math.Abs(waypoint.X - NPC.Center.X);
            bool closeEnough = horizontalGap <= stopDistance && Math.Abs(waypoint.Y - NPC.Center.Y) < 64f;

            if (closeEnough)
            {
                NPC.velocity.X *= 0.8f;

                // Face the target while holding position (the next attack turns it anyway).
                if (target != null)
                {
                    int facing = Math.Sign(target.Center.X - NPC.Center.X);

                    if (facing != 0)
                    {
                        NPC.direction = facing;
                    }
                }

                return;
            }

            // SF4 discards its path and replans whenever the waypoint moves > 2px (RunWaypointMovement: newWaypoint),
            // so a waypoint that tracks a walking owner/enemy would run A* every tick. Hold it until the desired
            // spot drifts WaypointRefreshDistance away.
            bool waypointDrifted = !_hasHeldWaypoint || Vector2.Distance(waypoint, _heldWaypoint) > WaypointRefreshDistance;

            if (waypointDrifted)
            {
                _heldWaypoint = waypoint;
                _hasHeldWaypoint = true;
            }

            SmartFighter4AI.Run(NPC, topSpeed, acceleration, doorBreakingDamage: 0, movementWaypoint: _heldWaypoint);
        }

        // ── Owner side: loadout, Cerulean, attack decision ─────────────────────────────────────────

        protected override void UpdateOwnerSide(Player owner)
        {
            // Hotbar slots 1 and 2 are cloned once; slot 0 follows the held weapon (last valid one kept, so holding
            // a pickaxe or a potion doesn't disarm the mimic).
            if (!_ownerLoadoutInitialized)
            {
                _ownerLoadoutInitialized = true;

                if (IsUsableWeapon(owner.inventory[0], owner))
                {
                    _ownerLoadout[1] = owner.inventory[0].Clone();
                }

                if (IsUsableWeapon(owner.inventory[1], owner))
                {
                    _ownerLoadout[2] = owner.inventory[1].Clone();
                }
            }

            // ── Winded: its own stamina pool (owner-side) ran low → stop attacking, and tell the server (OwnerFlags) so
            // its movement backs off. Hysteresis: winded at 15%, fighting again at 60%. Only for classes that pay
            // weapon stamina — a Classic player's mimic never tires, same as the player.
            bool usesWeaponStamina = owner.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina;
            float staminaMax = owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (!usesWeaponStamina)
            {
                winded = false;
            }
            else if (!winded && StaminaCurrent <= staminaMax * WindedEnterFraction)
            {
                winded = true;
            }
            else if (winded && StaminaCurrent >= staminaMax * WindedExitFraction)
            {
                winded = false;
            }

            if (winded)
            {
                OwnerFlags = (byte)(OwnerFlags | OwnerFlagWinded);
            }
            else
            {
                OwnerFlags = (byte)(OwnerFlags & ~OwnerFlagWinded);
            }

            Item heldItem = owner.HeldItem;
            bool heldIsNew = _ownerLoadout[0] == null || _ownerLoadout[0].type != heldItem.type;

            if (IsUsableWeapon(heldItem, owner) && heldIsNew)
            {
                _ownerLoadout[0] = heldItem.Clone();
            }

            // ── Cerulean (mana pool is owner-side, so the whole flask lives here) ──
            CeruleanFlaskPlayer cerulean = owner.GetModPlayer<CeruleanFlaskPlayer>();

            if (!_ceruleanInitialized)
            {
                _ceruleanInitialized = true;
                _ceruleanChargesMax = cerulean.CeruleanChargesMax;
                _ceruleanCharges = _ceruleanChargesMax;
            }

            if (_ceruleanRestoreTicksLeft > 0)
            {
                _ceruleanRestoreTicksLeft--;
                _ceruleanAccumulator += _ceruleanManaPerTick;
                int wholeMana = (int)_ceruleanAccumulator;

                if (wholeMana > 0)
                {
                    _ceruleanAccumulator -= wholeMana;
                    ManaCurrent = Math.Min(owner.statManaMax2, ManaCurrent + wholeMana);
                }
            }

            if (IsDrinkingCerulean)
            {
                _ceruleanDrinkTimer++;

                if (_ceruleanDrinkTimer >= _ceruleanDrinkTicks)
                {
                    // Same restore formula as the player's flask (CeruleanFlaskPlayer.UpdateDrinkingCerulean), from
                    // the owner's live flask stats, spread over the owner's restoration window.
                    ArcaneSorceryPlayer arcane = owner.GetModPlayer<ArcaneSorceryPlayer>();
                    float flatGain = cerulean.CeruleanManaGain * arcane.CeruleanFlatManaGainMult;
                    float totalGain = (flatGain + cerulean.CeruleanManaGainMaxManaBonus) * cerulean.CeruleanManaGainManaRegenBonus * cerulean.CeruleanRestorationTimerBonus;
                    int restoreTicks = Math.Max(1, (int)cerulean.CeruleanRestorationTimerMax);

                    _ceruleanCharges--;
                    _ceruleanRestoreTicksLeft = restoreTicks;
                    _ceruleanManaPerTick = totalGain / restoreTicks;
                    _ceruleanAccumulator = 0f;
                    _ceruleanDrinkTimer = -1;

                    CombatText.NewText(NPC.Hitbox, CombatText.HealMana, (int)totalGain);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item3, NPC.Center);
                }

                return;
            }

            Item weapon = ActiveWeapon;
            bool weaponUsesMana = weapon != null && !weapon.IsAir && weapon.mana > 0;
            bool lowMana = ManaCurrent < owner.statManaMax2 * CeruleanDrinkManaFraction;
            bool canStartCerulean = State == MimicState.Fight
                && UseState.ItemAnimation == 0
                && weaponUsesMana
                && lowMana
                && _ceruleanCharges > 0
                && _ceruleanRestoreTicksLeft == 0;

            if (canStartCerulean)
            {
                _ceruleanDrinkTimer = 0;
                _ceruleanDrinkTicks = Math.Max(1, (int)(cerulean.CeruleanDrinkTimerMax * 60f));
            }
        }

        protected override bool ChooseAttack(Player owner, out Vector2 aimWorld)
        {
            aimWorld = NPC.Center + new Vector2(NPC.direction * 100f, 0f);

            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (State != MimicState.Fight || IsDrinkingCerulean || winded)
            {
                return false;
            }

            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxNPCs;

            if (!targetIndexValid)
            {
                return false;
            }

            NPC target = Main.npc[TargetIndex];

            if (!UsefulFunctions.IsHostileThreat(target))
            {
                return false;
            }

            Item weapon = ActiveWeapon;

            if (weapon == null || weapon.IsAir)
            {
                return false;
            }

            GetEngagementRange(weapon, out _, out float maximumDistance);
            float distance = Vector2.Distance(NPC.Center, target.Center);

            if (distance > maximumDistance)
            {
                return false;
            }

            // Ranged weapons need a clear line; melee swings don't (the SF4 path walks it into reach).
            bool rangedWeapon = maximumDistance > SpearLikeMaximum;

            if (rangedWeapon && !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            // Lead moving targets by projectile travel time, capped.
            aimWorld = target.Center;

            if (weapon.shootSpeed > 0f)
            {
                float travelTicks = Math.Min(MaxLeadTicks, distance / weapon.shootSpeed);
                aimWorld += target.velocity * travelTicks;
            }

            return true;
        }

        // ── Aggro, network extras, display ──────────────────────────────────────────────────────

        public override bool DrawsAggro => true;

        public override bool CanDrawAggroNow => State != MimicState.Summoning;

        protected override bool HasOwnerActivity => IsDrinkingCerulean;

        /// <summary>Cerulean lives on the owner's client; the server needs "drinking" to stop moving, other clients
        /// need the timer and charges to draw the flask.</summary>
        protected override void WriteOwnerExtras(BinaryWriter writer)
        {
            writer.Write((short)_ceruleanDrinkTimer);
            writer.Write((short)_ceruleanDrinkTicks);
            writer.Write((byte)_ceruleanCharges);
            writer.Write((byte)_ceruleanChargesMax);
        }

        protected override void ReadOwnerExtras(BinaryReader reader)
        {
            _ceruleanDrinkTimer = reader.ReadInt16();
            _ceruleanDrinkTicks = reader.ReadInt16();
            _ceruleanCharges = reader.ReadByte();
            _ceruleanChargesMax = reader.ReadByte();
        }

        /// <summary>The rise: fully sunk (hidden behind the ground) while the pool spreads, then eases up out of it.</summary>
        protected override Vector2 DisplayDrawOffset
        {
            get
            {
                if (State != MimicState.Summoning)
                {
                    return Vector2.Zero;
                }

                float riseProgress = (_stateTimer - SummonPoolTicks) / (float)(SummonTicks - SummonPoolTicks);
                riseProgress = Math.Clamp(riseProgress, 0f, 1f);

                // Ease-out cubic: fast out of the pool, settling as it finishes forming.
                float remaining = 1f - riseProgress;
                float eased = 1f - remaining * remaining * remaining;
                float sinkDepth = (1f - eased) * NPC.height;
                return new Vector2(0f, sinkDepth);
            }
        }

        protected override void DecorateDisplayClone(Player clone)
        {
            // The flask draw layer (tsorcRevampPlayerHeldDrawLayers) reads the drawn player's own Estus/Cerulean
            // ModPlayer state, so setting the CLONE's copies puts the flask in the mimic's hand.
            tsorcRevampEstusPlayer cloneEstus = clone.GetModPlayer<tsorcRevampEstusPlayer>();
            bool drinkingEstus = State == MimicState.DrinkingEstus;
            float estusDrinkSeconds = Math.Max(1, _stats.EstusDrinkTicks) / 60f;
            cloneEstus.IsDrinking = drinkingEstus;
            cloneEstus.EstusDrinkTimer = drinkingEstus ? _stateTimer / 60f : 0f;
            cloneEstus.EstusDrinkTimerMax = estusDrinkSeconds;
            cloneEstus.EstusChargesCurrent = _estusCharges;
            cloneEstus.EstusChargesMax = Math.Max(1, _stats.EstusChargesMax);

            CeruleanFlaskPlayer cloneCerulean = clone.GetModPlayer<CeruleanFlaskPlayer>();
            float ceruleanDrinkSeconds = Math.Max(1, _ceruleanDrinkTicks) / 60f;
            cloneCerulean.IsDrinking = IsDrinkingCerulean;
            cloneCerulean.CeruleanDrinkTimer = IsDrinkingCerulean ? _ceruleanDrinkTimer / 60f : 0f;
            cloneCerulean.CeruleanDrinkTimerMax = ceruleanDrinkSeconds;
            cloneCerulean.CeruleanChargesCurrent = _ceruleanCharges;
            cloneCerulean.CeruleanChargesMax = Math.Max(1, _ceruleanChargesMax);

            // Raised drinking arm, same frame and start point as the player's flasks.
            bool estusPose = drinkingEstus && _stateTimer >= _stats.EstusDrinkTicks * DrinkPoseStartFraction;
            bool ceruleanPose = IsDrinkingCerulean && _ceruleanDrinkTimer >= _ceruleanDrinkTicks * DrinkPoseStartFraction;

            if (estusPose || ceruleanPose)
            {
                clone.bodyFrame = PlayerFrames.Use2.ToRectangle();
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            int dustCount = 4;

            if (NPC.life <= 0)
            {
                dustCount = 40;
            }

            // Dark liquid-metal splash.
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Asphalt, hit.HitDirection * 2f, -2f, 0, default, 1.2f);
                dust.noGravity = NPC.life <= 0;
            }
        }

        // ── Helpers (shared by the server's weapon choice/movement and the owner's attack decision) ─────

        /// <summary>
        /// Whether the mimic may wield this item. Design §5 filter: a real weapon (damage, not ammo/consumable),
        /// not a minion/sentry staff (recursion), not a pure utility tool, and — if it fires ammo — the owner has some.
        /// </summary>
        private static bool IsUsableWeapon(Item item, Player owner)
        {
            if (item == null || item.IsAir || item.damage <= 0 || item.useStyle == ItemUseStyleID.None)
            {
                return false;
            }

            if (item.consumable || item.ammo != AmmoID.None || item.healLife > 0 || item.healMana > 0)
            {
                return false;
            }

            bool shootsWhip = item.shoot > ProjectileID.None && ProjectileID.Sets.IsAWhip[item.shoot];
            bool summonStaff = item.DamageType.CountsAsClass(DamageClass.Summon) && !shootsWhip;

            if (summonStaff)
            {
                return false;
            }

            bool isTool = item.pick > 0 || item.axe > 0 || item.hammer > 0;
            bool weaponClassifiedTool = global::tsorcRevamp.tsorcRevamp.WeaponClassifiedTools != null && global::tsorcRevamp.tsorcRevamp.WeaponClassifiedTools.Contains(item.type);

            if (isTool && !weaponClassifiedTool)
            {
                return false;
            }

            if (item.useAmmo > 0 && !owner.HasAmmo(item))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Where the mimic likes to stand with this weapon and the furthest it will attack from (pixels).
        /// Melee swing reach is measured from the item's sprite; everything that fires gets a ranged band.
        /// </summary>
        private static void GetEngagementRange(Item item, out float preferred, out float maximum)
        {
            bool shootsWhip = item.shoot > ProjectileID.None && ProjectileID.Sets.IsAWhip[item.shoot];

            if (shootsWhip)
            {
                preferred = WhipPreferred;
                maximum = WhipMaximum;
                return;
            }

            bool isMelee = item.DamageType.CountsAsClass(DamageClass.Melee);
            bool firesProjectile = item.shoot > ProjectileID.None;

            // Spears, yoyos, flails, boomerangs: melee damage, but the hit comes from a projectile, not the swing.
            if (isMelee && item.noMelee && firesProjectile)
            {
                preferred = SpearLikePreferred;
                maximum = SpearLikeMaximum;
                return;
            }

            if (isMelee && !item.noMelee)
            {
                // Swing reach ≈ the sprite's diagonal at its use scale, plus a little for the hitbox edge.
                float spriteDiagonal = (float)Math.Sqrt(item.width * item.width + item.height * item.height) * item.scale;
                preferred = spriteDiagonal * 0.6f;
                maximum = spriteDiagonal + 16f;

                // Sword beams and similar: still prefers to stand close, but can attack from further out.
                if (firesProjectile)
                {
                    maximum = SwordBeamMaximum;
                }

                return;
            }

            preferred = RangedPreferred;
            maximum = RangedMaximum;
        }

        private static int FirstFilledSlot(int[] loadoutTypes)
        {
            for (int slot = 0; slot < loadoutTypes.Length; slot++)
            {
                if (loadoutTypes[slot] > 0)
                {
                    return slot;
                }
            }

            return 0;
        }
    }

    /// <summary>The owner's stats at summon time. Travels in the spawn request and in SendExtraAI.</summary>
    internal struct MimicStats
    {
        public int LifeMax;
        public int Defense;
        public int EstusChargesMax;
        public int EstusHeal;
        public int EstusDrinkTicks;
        public int EstusHealTicks;

        public void Write(BinaryWriter writer)
        {
            writer.Write(LifeMax);
            writer.Write(Defense);
            writer.Write((byte)EstusChargesMax);
            writer.Write(EstusHeal);
            writer.Write((short)EstusDrinkTicks);
            writer.Write((short)EstusHealTicks);
        }

        public static MimicStats Read(BinaryReader reader)
        {
            MimicStats stats = new MimicStats();
            stats.LifeMax = reader.ReadInt32();
            stats.Defense = reader.ReadInt32();
            stats.EstusChargesMax = reader.ReadByte();
            stats.EstusHeal = reader.ReadInt32();
            stats.EstusDrinkTicks = reader.ReadInt16();
            stats.EstusHealTicks = reader.ReadInt16();
            return stats;
        }
    }

    /// <summary>/mimic — summon a Mimic Tear (until the summon item exists). Chat command: runs on the owner's client,
    /// which snapshots its own stats and, in multiplayer, asks the server to spawn it.</summary>
    public class MimicTearCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "mimic";

        public override string Description => "Summon a Mimic Tear: a reflective copy of you with your HP, defense, Estus, Cerulean and weapons (held + hotbar 1/2). /mimictest clear removes it.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (caller.Player == null)
            {
                return;
            }

            MimicTear.RequestSpawn(caller.Player);
            caller.Reply("Mimic Tear summoned.", Color.Lime);
        }
    }
}
