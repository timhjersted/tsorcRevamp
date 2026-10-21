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
    /// owner's own ItemCheck — see <see cref="MirrorPuppetSwap"/>), and has their max HP, defense, stamina, mana,
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
            /// <summary>Dodge roll: the player's roll (same length, speed and i-frames), paid from the mimic's stamina.</summary>
            Rolling = 4,
            /// <summary>Wants an Estus: stops attacking, makes space if crowded, then drinks (see the Estus block in AI).</summary>
            PreparingEstus = 5,
            /// <summary>Lifetime over (or dismissed): melting back into its pool. Invulnerable, no aggro, no actions.</summary>
            Dissolving = 6,
        }

        /// <summary>Minion slots the Mimic Tear takes: all the owner has, up to this many. Shown in the Mimic Rune tooltip.</summary>
        public const int MinionSlotCost = 3;
        /// <summary>Resummon cooldown, applied as the MimicTearCooldown debuff (cleared on death). Shown in the tooltip.</summary>
        public const int ResummonCooldownSeconds = 300;
        /// <summary>How long a Soul Mimic lasts before it dissolves on its own, counted from spawn. Shown in the tooltip.
        /// Equal to the cooldown on purpose: the rune comes off cooldown as the mimic it made runs out.</summary>
        public const int LifetimeSeconds = 300;
        /// <summary>Length of the dissolve (1 s).</summary>
        private const int DissolveTicks = 60;
        /// <summary>How long the MimicTearBuff lasts with no mimic alive — covers the spawn round-trip in multiplayer.
        /// The server also waits this long after spawning before it treats "owner has no buff" as a dismissal.</summary>
        public const int SummonBuffGraceTicks = 300;

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
        /// <summary>While winded with a target: closer than 3 tiles → step back to 4.5; at 3 tiles or more it just
        /// holds and catches its breath. A short breather, not a retreat.</summary>
        private const float WindedNearDistance = 3f * 16f;
        private const float WindedSettleDistance = 4.5f * 16f;
        /// <summary><see cref="MirrorPuppetNPC.OwnerFlags"/> bit: the owner-side stamina pool says "winded".</summary>
        private const byte OwnerFlagWinded = 1;
        /// <summary><see cref="MirrorPuppetNPC.OwnerFlags"/> bit: the owner-side pool has stamina for a roll. Same rule
        /// as the player (TryStartDodgeroll): any stamina above zero.</summary>
        private const byte OwnerFlagCanRoll = 2;
        /// <summary><see cref="MirrorPuppetNPC.OwnerFlags"/> bit: a weapon use is in progress (item animation running).
        /// The server won't start an Estus drink until it clears.</summary>
        private const byte OwnerFlagAttacking = 4;

        /// <summary>Estus: an enemy within 4 tiles is "crowding"; it backs off toward 6 before drinking (the 4–8 band),
        /// and gives up and drinks anyway after 2.5 s of failing to get clear.</summary>
        private const float EstusSpaceNearDistance = 4f * 16f;
        private const float EstusSpaceSettleDistance = 6f * 16f;
        private const int EstusSpaceTimeoutTicks = 150;
        /// <summary>Jump attack on an overhead target: launch speed is solved from the height, clamped to this range
        /// (9 px/tick ≈ an 8-tile jump at NPC gravity 0.3); targets higher than JumpAttackMaxHeight are left to a
        /// ranged weapon. One jump per JumpAttackCooldownTicks.</summary>
        private const float JumpAttackMinSpeed = 5f;
        private const float JumpAttackMaxSpeed = 9f;
        private const float JumpAttackMaxHeight = 9f * 16f;
        private const int JumpAttackCooldownTicks = 45;
        /// <summary>SmartFighter4's door value: non-zero lets it path through closed doors and open them (it opens,
        /// it doesn't destroy — see SmartFighter4AI.TryDoor). 4 is SF4's own default.</summary>
        private const int DoorOpenValue = 4;
        /// <summary>Knockback taken, as a fraction of a normal NPC's (0 = none). ESTIMATE.</summary>
        private const float KnockbackTaken = 0.6f;

        // ── Evasion (server). The roll's numbers (RollTicks, RollSpeed…) and the projectile look-ahead live in the shared
        // base (MirrorPuppetNPC) — the mirror invader rolls the same way. ──
        /// <summary>An enemy mid-swing within this range (5 tiles) is a melee threat.</summary>
        private const float MeleeThreatRange = 5f * 16f;
        /// <summary>Chance it reacts to a given threat. Below 1 on purpose: a mimic that dodges everything isn't a
        /// copy of a player. One decision per EvadeDecisionLockoutTicks, so a miss isn't re-rolled every tick. ESTIMATE.</summary>
        private const float EvadeChance = 0.75f;
        private const int EvadeDecisionLockoutTicks = 20;
        /// <summary>Jump-evade launch speed (px/tick upward) — about a 3.5-tile hop over a low shot.</summary>
        private const float JumpEvadeSpeed = 7.5f;
        /// <summary>Sidestep: run this far to the side (5 tiles) for this long instead of the normal waypoint.</summary>
        private const float StrafeDistance = 5f * 16f;
        private const int StrafeTicks = 24;
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
        private int _serverAgeTicks;
        private int _rollDirection;
        private int _rollCooldown;
        private int _evadeLockout;
        private int _strafeTicksLeft;
        private int _strafeDirection;
        private int _jumpAttackCooldown;

        // All machines: local timer for the current state (reset when ai[3] changes), used for drink pose/progress.
        private MimicState _lastSeenState = MimicState.Fight;
        private int _stateTimer;

        // Owner-client only.
        private MimicState _ownerLastState = MimicState.Summoning;
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
            NPC.knockBackResist = KnockbackTaken;

            // Ropes are a per-NPC opt-in for SmartFighter4 (doors are enabled by the value passed to Run).
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanUseRopes = true;
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

                bool startedDissolve = state == MimicState.Dissolving && !Main.dedServ;

                if (startedDissolve)
                {
                    SoundEngine.PlaySound(SoundID.Splash with { Pitch = -0.8f, Volume = 0.8f }, NPC.Bottom);
                }

                bool startedRoll = state == MimicState.Rolling && !Main.dedServ;

                if (startedRoll)
                {
                    // The player's own roll thud (tsorcRevampPlayer.TryStartDodgeroll).
                    SoundStyle rollSound = new SoundStyle("tsorcRevamp/Sounds/DarkSouls/roll1")
                    {
                        Volume = 0.1f,
                        PitchVariance = 0.4f
                    };
                    SoundEngine.PlaySound(rollSound, NPC.Center);
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
            // A roll is immune for its whole length, like the player's (isDodging gates every hit).
            // Dissolving is the same picture in reverse: it melts back down into the pool when its time is up.
            bool summoning = state == MimicState.Summoning;
            bool rolling = state == MimicState.Rolling;
            bool dissolving = state == MimicState.Dissolving;
            NPC.dontTakeDamage = summoning || rolling || dissolving;
            NPC.behindTiles = summoning || dissolving;

            if ((summoning || dissolving) && !Main.dedServ)
            {
                float poolRadius;

                if (summoning)
                {
                    // Pool: spreads over SummonPoolTicks, holds, then pulls in over the last quarter as the body leaves it.
                    float spread = Math.Min(1f, _stateTimer / (float)SummonPoolTicks);
                    float shrinkStart = SummonTicks * 0.75f;
                    float shrink = 1f - Math.Clamp((_stateTimer - shrinkStart) / (SummonTicks - shrinkStart), 0f, 1f);
                    poolRadius = SummonPoolRadius * spread * shrink;
                }
                else
                {
                    // Dissolve: the pool widens as the body runs down into it.
                    poolRadius = SummonPoolRadius * Math.Min(1f, _stateTimer / (DissolveTicks * 0.5f));
                }

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

                // Drips only while some of the body is above the surface (after the pool phase of the rise; all
                // through the dissolve).
                bool bodyAboveSurface = dissolving || _stateTimer >= SummonPoolTicks;

                if (bodyAboveSurface && Main.rand.NextBool(2))
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

            // Dismissed: the owner cancelled (or lost) the MimicTearBuff. Player buffs reach the server through
            // vanilla's own sync, so wait out the spawn round-trip before trusting "no buff".
            _serverAgeTicks++;
            int summonBuffType = ModContent.BuffType<Buffs.Weapons.Summon.MimicTearBuff>();
            bool dismissed = _serverAgeTicks > SummonBuffGraceTicks && !owner.HasBuff(summonBuffType);

            // Its time is up (LifetimeSeconds from spawn), or it was dismissed: melt back into the pool, then go.
            bool expired = _serverAgeTicks >= LifetimeSeconds * 60;

            if ((dismissed || expired) && !dissolving)
            {
                State = MimicState.Dissolving;
                NPC.netUpdate = true;
                return;
            }

            if (dissolving)
            {
                NPC.velocity.X = 0f;

                if (_stateTimer >= DissolveTicks)
                {
                    NPC.active = false;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
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

            // ── Dodge roll in progress: the player's profile — full speed for the first half, then coast and decay ──
            if (_rollCooldown > 0)
            {
                _rollCooldown--;
            }

            if (_evadeLockout > 0)
            {
                _evadeLockout--;
            }

            if (_strafeTicksLeft > 0)
            {
                _strafeTicksLeft--;
            }

            if (rolling)
            {
                if (_stateTimer < RollTicks / 2)
                {
                    NPC.velocity.X = _rollDirection * RollSpeed;
                }
                else
                {
                    NPC.velocity.X *= RollTailRetention;
                }

                if (_stateTimer >= RollTicks)
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

            // ── Estus: never mid-attack, and not with an enemy on top of it ──
            // Wanting a drink puts it in PreparingEstus, which (being not-Fight) makes the owner client stop starting
            // attacks. It drinks once the swing in progress has finished (OwnerFlagAttacking clear) AND no enemy is
            // within 4 tiles; while crowded it backs off toward 6 tiles (movement block below). If it can't get clear
            // within EstusSpaceTimeoutTicks it drinks anyway rather than die waiting.
            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool wantsEstus = lowLife && _estusCharges > 0 && !IsDrinkingCerulean && _estusHealTicksLeft == 0;
            bool canPrepare = state == MimicState.Fight || state == MimicState.PreparingEstus;
            NPC crowdingEnemy = null;

            if (canPrepare && wantsEstus)
            {
                float nearestEnemyDistance = EstusSpaceNearDistance;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC enemy = Main.npc[i];

                    if (!UsefulFunctions.IsHostileThreat(enemy))
                    {
                        continue;
                    }

                    float enemyDistance = Vector2.Distance(enemy.Center, NPC.Center);

                    if (enemyDistance < nearestEnemyDistance)
                    {
                        nearestEnemyDistance = enemyDistance;
                        crowdingEnemy = enemy;
                    }
                }

                bool attacking = (OwnerFlags & OwnerFlagAttacking) != 0;
                bool gaveUpOnSpace = state == MimicState.PreparingEstus && _stateTimer >= EstusSpaceTimeoutTicks;
                bool hasSpace = crowdingEnemy == null || gaveUpOnSpace;

                if (hasSpace && !attacking)
                {
                    State = MimicState.DrinkingEstus;
                    NPC.netUpdate = true;
                    return;
                }

                if (state == MimicState.Fight)
                {
                    State = MimicState.PreparingEstus;
                    NPC.netUpdate = true;
                }
            }
            else if (state == MimicState.PreparingEstus)
            {
                // Healed some other way, or out of charges: back to fighting.
                State = MimicState.Fight;
                NPC.netUpdate = true;
            }

            // ── Evasion: find the most urgent threat, then roll / jump / sidestep ──
            // Threat 1: a hostile projectile whose closest approach in the next EvadeLookaheadTicks passes through us.
            // Solved in the mimic's frame: relative position + relative velocity, closest approach at
            // t = -(r·v)/(v·v).
            bool threatFound = FindIncomingProjectile(false, out _, out Vector2 threatVelocity, out Vector2 threatOrigin);
            bool threatIsMelee = false;

            // Threat 2: an enemy within reach that is winding up or swinging (FighterAI's AttackCommitted, or a
            // puppet enemy's melee telegraph/attack phase). Only looked for if no projectile is about to land.
            if (!threatFound)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC enemy = Main.npc[i];

                    if (!UsefulFunctions.IsHostileThreat(enemy) || Vector2.Distance(enemy.Center, NPC.Center) > MeleeThreatRange)
                    {
                        continue;
                    }

                    bool fighterSwinging = enemy.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackCommitted;
                    bool puppetSwinging = enemy.ModNPC is Puppets.PuppetNPC puppetEnemy && puppetEnemy.IsMeleeSwingThreatening;

                    if (!fighterSwinging && !puppetSwinging)
                    {
                        continue;
                    }

                    threatFound = true;
                    threatOrigin = enemy.Center;
                    threatIsMelee = true;
                    break;
                }
            }

            if (threatFound && _evadeLockout == 0)
            {
                _evadeLockout = EvadeDecisionLockoutTicks;
                bool reacts = Main.rand.NextFloat() < EvadeChance;

                if (reacts)
                {
                    bool grounded = NPC.velocity.Y == 0f;
                    bool canRoll = (OwnerFlags & OwnerFlagCanRoll) != 0 && _rollCooldown == 0 && grounded;
                    bool mostlyHorizontal = Math.Abs(threatVelocity.X) > Math.Abs(threatVelocity.Y);

                    // Away from where the threat is: for melee, away from the attacker; for a shot from above or
                    // below, out from under it.
                    int awayDirection = Math.Sign(NPC.Center.X - threatOrigin.X);

                    if (awayDirection == 0)
                    {
                        awayDirection = -NPC.direction;
                    }

                    if (canRoll)
                    {
                        // A level shot is rolled THROUGH (toward where it came from — the i-frames carry it past);
                        // anything else is rolled away from.
                        int rollDirection = awayDirection;

                        if (!threatIsMelee && mostlyHorizontal)
                        {
                            rollDirection = -Math.Sign(threatVelocity.X);
                        }

                        // Look before rolling: no ledges, no wall in the first two tiles. If the preferred way is
                        // bad, the other way still gets the i-frames; if both are bad, fall through to jump/sidestep.
                        bool rollPathSafe = IsRollPathSafe(rollDirection);

                        if (!rollPathSafe)
                        {
                            rollDirection = -rollDirection;
                            rollPathSafe = IsRollPathSafe(rollDirection);
                        }

                        if (rollPathSafe)
                        {
                            _rollDirection = rollDirection;
                            _rollCooldown = RollTicks + RollCooldownTicks;
                            NPC.direction = rollDirection;
                            State = MimicState.Rolling;
                            NPC.netUpdate = true;
                            return;
                        }
                    }

                    if (grounded && !threatIsMelee && mostlyHorizontal)
                    {
                        // No roll available: hop over a level shot.
                        NPC.velocity.Y = -JumpEvadeSpeed;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        // Sidestep out of the way for a moment.
                        _strafeDirection = awayDirection;
                        _strafeTicksLeft = StrafeTicks;
                    }
                }
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

            // Getting ready to drink Estus: if an enemy is crowding it, open up 6 tiles of space first; otherwise
            // just stop and wait for the swing in progress to finish (the state machine above starts the drink).
            if (State == MimicState.PreparingEstus)
            {
                if (crowdingEnemy == null)
                {
                    NPC.velocity.X *= 0.8f;
                    return;
                }

                int sideOfCrowder = Math.Sign(NPC.Center.X - crowdingEnemy.Center.X);

                if (sideOfCrowder == 0)
                {
                    sideOfCrowder = -NPC.direction;
                }

                Vector2 spaceWaypoint = crowdingEnemy.Center + new Vector2(sideOfCrowder * EstusSpaceSettleDistance, 0f);
                bool spaceWaypointDrifted = !_hasHeldWaypoint || Vector2.Distance(spaceWaypoint, _heldWaypoint) > WaypointRefreshDistance;

                if (spaceWaypointDrifted)
                {
                    _heldWaypoint = spaceWaypoint;
                    _hasHeldWaypoint = true;
                }

                SmartFighter4AI.Run(NPC, topSpeed, acceleration, doorBreakingDamage: DoorOpenValue, movementWaypoint: _heldWaypoint);
                return;
            }

            // Sidestep (from the evasion block): for StrafeTicks the only goal is to get out from under the threat.
            if (_strafeTicksLeft > 0)
            {
                Vector2 strafeWaypoint = NPC.Center + new Vector2(_strafeDirection * StrafeDistance, 0f);
                bool strafeWaypointDrifted = !_hasHeldWaypoint || Vector2.Distance(strafeWaypoint, _heldWaypoint) > WaypointRefreshDistance * 3f;

                if (strafeWaypointDrifted)
                {
                    _heldWaypoint = strafeWaypoint;
                    _hasHeldWaypoint = true;
                }

                SmartFighter4AI.Run(NPC, topSpeed, acceleration, doorBreakingDamage: DoorOpenValue, movementWaypoint: _heldWaypoint);
                return;
            }

            Vector2 waypoint;
            float stopDistance;

            // Winded (owner-side stamina below the threshold, OwnerFlags): a short breather near the target while the
            // pool refills, like a player who's run dry. The owner client also refuses attacks while winded (ChooseAttack).
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
                float maximumDistance = RangedMaximum;

                if (activeType > 0)
                {
                    GetEngagementRange(ContentSamples.ItemsByType[activeType], out preferredDistance, out maximumDistance);
                }

                // Jump attack: a melee-range weapon and a target overhead that it can't reach from the ground (a
                // flyer, or something on a ledge). Launch speed is solved for the height: v = sqrt(2·g·h), clamped —
                // the owner client swings when the target comes into reach mid-air (ChooseAttack only needs range).
                if (_jumpAttackCooldown > 0)
                {
                    _jumpAttackCooldown--;
                }

                float heightAboveMimic = NPC.Center.Y - target.Center.Y;
                bool meleeRangeWeapon = maximumDistance <= SpearLikeMaximum;
                bool targetOverhead = Math.Abs(NPC.Center.X - target.Center.X) <= maximumDistance
                    && heightAboveMimic > maximumDistance * 0.6f
                    && heightAboveMimic <= JumpAttackMaxHeight;
                bool canJumpAttack = meleeRangeWeapon && targetOverhead && !winded && NPC.velocity.Y == 0f && _jumpAttackCooldown == 0 && State == MimicState.Fight;

                if (canJumpAttack)
                {
                    float launchSpeed = (float)Math.Sqrt(2f * NPC.gravity * heightAboveMimic);
                    NPC.velocity.Y = -Math.Clamp(launchSpeed, JumpAttackMinSpeed, JumpAttackMaxSpeed);
                    _jumpAttackCooldown = JumpAttackCooldownTicks;
                    NPC.netUpdate = true;
                }

                int sideOfTarget = Math.Sign(NPC.Center.X - target.Center.X);

                if (sideOfTarget == 0)
                {
                    sideOfTarget = -owner.direction;
                }

                if (winded)
                {
                    // Catching its breath: only give ground if the target is right on top of it (< 3 tiles), and
                    // then just to 4.5 tiles. Otherwise hold where it is, facing the target.
                    float gapToTarget = Math.Abs(NPC.Center.X - target.Center.X);

                    if (gapToTarget >= WindedNearDistance)
                    {
                        NPC.velocity.X *= 0.8f;
                        NPC.direction = -sideOfTarget;
                        return;
                    }

                    preferredDistance = WindedSettleDistance;
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

            SmartFighter4AI.Run(NPC, topSpeed, acceleration, doorBreakingDamage: DoorOpenValue, movementWaypoint: _heldWaypoint);
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

            // ── Dodge roll: the server starts it (State → Rolling); the stamina it costs is charged here, to the
            // mimic's own pool, at exactly the owner's roll price. The pool may be overdrawn to zero, never below.
            // Then tell the server whether another roll is affordable (the player's rule: any stamina above zero).
            MimicState currentState = State;
            bool rollJustStarted = currentState == MimicState.Rolling && _ownerLastState != MimicState.Rolling;

            if (rollJustStarted)
            {
                float rollCost = owner.GetModPlayer<tsorcRevampPlayer>().RollStaminaCost;
                StaminaCurrent = Math.Max(0f, StaminaCurrent - rollCost);
            }

            _ownerLastState = currentState;

            if (StaminaCurrent > 0f)
            {
                OwnerFlags = (byte)(OwnerFlags | OwnerFlagCanRoll);
            }
            else
            {
                OwnerFlags = (byte)(OwnerFlags & ~OwnerFlagCanRoll);
            }

            // A use in progress (as of last tick's ItemCheck): the server holds off on Estus until it's over.
            if (UseState.ItemAnimation > 0)
            {
                OwnerFlags = (byte)(OwnerFlags | OwnerFlagAttacking);
            }
            else
            {
                OwnerFlags = (byte)(OwnerFlags & ~OwnerFlagAttacking);
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

        /// <summary>All the owner's minion slots, up to <see cref="MinionSlotCost"/> (design §2). A non-summoner with
        /// one slot pays one; a summoner pays three and keeps the rest.</summary>
        public override float GetMinionSlotsUsed(Player owner)
        {
            return Math.Min(MinionSlotCost, owner.maxMinions);
        }

        public override bool DrawsAggro => true;

        public override bool CanDrawAggroNow => State != MimicState.Summoning && State != MimicState.Dissolving;

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
                // Dissolve: sinks back down, slow at first then faster (ease-in), the reverse of the rise.
                if (State == MimicState.Dissolving)
                {
                    float sinkProgress = Math.Clamp(_stateTimer / (float)DissolveTicks, 0f, 1f);
                    return new Vector2(0f, sinkProgress * sinkProgress * NPC.height);
                }

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

            // Dodge roll: one full tumble over the roll's length, tucked legs — the player's own roll visual
            // (tsorcRevampPlayer.UpdateDodging: rotation lerps to ±2π, forcedLegFrame = Jump).
            clone.fullRotation = 0f;

            if (State == MimicState.Rolling)
            {
                float rollProgress = Math.Clamp(_stateTimer / (float)RollTicks, 0f, 1f);
                int rollFacing = NPC.direction == 0 ? 1 : NPC.direction;
                clone.fullRotation = rollFacing * rollProgress * MathHelper.TwoPi;
                clone.fullRotationOrigin = RollRotationOrigin;
                clone.legFrame = PlayerFrames.Jump.ToRectangle();
            }
        }

        // ── Damage taken: the owner's rules, not an NPC's (design §11.6) ─────────────────────────────

        /// <summary>The owner's post-hit immunity: 40 ticks, 80 with a Cross Necklace-type longInvince (vanilla Player.Hurt).</summary>
        internal override int PostHitImmunityTicks
        {
            get
            {
                bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

                if (ownerIndexValid && Main.player[OwnerIndex].longInvince)
                {
                    return 80;
                }

                return base.PostHitImmunityTicks;
            }
        }

        /// <summary>
        /// Every hit is taken the way the owner would take it. NPC strikes count defense at 50%; a player counts it at
        /// their own DefenseEffectiveness (0.75 Expert, 1.0 Master, plus gear), so the ratio is applied on top. Then the
        /// owner's damage reduction (endurance) and the mod's enemy-hit progression scaling (ProgressionScaling, x1.0-1.34),
        /// as ProgressionScalingPlayer applies to players. Owner stats are read live (the server updates every player).
        /// </summary>
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

            if (!ownerIndexValid)
            {
                return;
            }

            Player owner = Main.player[OwnerIndex];
            float defenseRatio = owner.DefenseEffectiveness.Value / NpcDefenseEffectiveness;
            modifiers.DefenseEffectiveness *= defenseRatio;

            float damageReduction = MathHelper.Clamp(owner.endurance, 0f, 1f);
            modifiers.FinalDamage *= 1f - damageReduction;
            modifiers.FinalDamage *= ProgressionScaling.DamageTakenScale;
        }

        /// <summary>NPC.HitModifiers' default defense effectiveness (vanilla: NPC defense counts half).</summary>
        private const float NpcDefenseEffectiveness = 0.5f;

        /// <summary>Hostile projectiles hurt players for twice their damage (vanilla Projectile.Damage: DamageVar(damage) x 2)
        /// but friendly NPCs for once — so the mimic took half of what its owner would.</summary>
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.hostile)
            {
                modifiers.SourceDamage *= 2f;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // Starts the shared post-hit immunity (FriendlyPuppetNPC).
            base.HitEffect(hit);

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

            // Debug path: skips the Mimic Rune cooldown and slot checks, but still needs the buff — the server treats a
            // mimic whose owner has no MimicTearBuff as dismissed.
            caller.Player.AddBuff(ModContent.BuffType<Buffs.Weapons.Summon.MimicTearBuff>(), MimicTear.SummonBuffGraceTicks);
            MimicTear.RequestSpawn(caller.Player);
            caller.Reply("Mimic Tear summoned (debug: no cooldown).", Color.Lime);
        }
    }
}
