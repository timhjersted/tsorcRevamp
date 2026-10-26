using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;
using tsorcRevamp.Systems.EstusFlask;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// The hostile mirror invader (design §12, build step 5): a copy of the triggering player — their look and every
    /// usable hotbar weapon — that hunts the whole party, fights through the step-4 use wrapper (tells, aim lock,
    /// bursts, recovery), dodge rolls with the player's numbers, drinks Estus and drops the Mimic Rune.
    ///
    /// Authority (CLAUDE.md multiplayer rule):
    ///   server — target, host, state, active weapon slot (shuffled bag), movement (SmartFighter4 waypoint mode), HP,
    ///            Estus, roll decisions.
    ///   host   — weapon use through the swap, stamina/mana pools; reports wrapper state back in OwnerFlags.
    ///   clients — display clone (the triggering player's look), tell visuals, flask/roll poses, sounds.
    ///
    /// ai[0] = host (MirrorPuppetNPC.OwnerIndex), ai[1] = triggering player (look + loadout source), ai[2] = target
    /// player (-1 none), ai[3] = <see cref="InvaderState"/>. Loadout, active slot and Estus charges ride in SendExtraAI.
    /// Summoners (§12.9, step 6): each minion staff in the hotbar lets it raise the staff and call a ghost add (at most 2
    /// alive); fewer than 2 usable weapons → Night's Edge + Flintlock Pistol are added. Not yet: the event ring (step 7).
    /// </summary>
    public class MirrorInvader : HostileMirrorPuppetNPC
    {
        public enum InvaderState
        {
            Fight = 0,
            /// <summary>Lowering one weapon and raising the next: no attacks (the switch doubles as a tell, §12.10).</summary>
            SwitchingWeapon = 1,
            /// <summary>Wants Estus: stops attacking, backs off a crowding player, then drinks.</summary>
            PreparingEstus = 2,
            DrinkingEstus = 3,
            /// <summary>The player's roll: same length, speed and i-frames, paid from the invader's stamina.</summary>
            Rolling = 4,
            /// <summary>Nobody left to fight: sinks away and is removed. Invulnerable.</summary>
            Dissolving = 5,
            /// <summary>Raising a minion staff: a ghost add forms at the summon point, then arrives (§12.9).</summary>
            SummoningAdd = 6
        }

        /// <summary>Hotbar slots copied (all 10, §12.11 #2).</summary>
        private const int HotbarSlots = 10;

        // ── Tuning (ESTIMATE — first pass) ────────────────────────────────────────────────────────
        /// <summary>Authored life, used only where the EnemyBalance registry has no value (pre-Hardmode debug spawns).
        /// HM/SHM health lives in Systems/EnemyBalance.cs.</summary>
        private const int UnregisteredLifeMax = 20000;
        private const float KnockbackTaken = 0.4f;

        private const int WeaponSwitchTicks = 20;
        private const int RetargetIntervalTicks = 30;
        /// <summary>Players further than this are not hunted (90 tiles).</summary>
        private const float TargetLeash = 90f * 16f;
        /// <summary>A player who hit it within this long is preferred as the target ("last hit by").</summary>
        private const int LastHitMemoryTicks = 180;
        /// <summary>A different player at under this fraction of the current target's distance takes over as target.</summary>
        private const float RetargetCloserFraction = 0.5f;
        /// <summary>The host must stay within this distance (100 tiles) — its client simulates the invader's projectiles
        /// and terrain; past it the server hands the swap to the target or the nearest living player.</summary>
        private const float HostMaxDistance = 100f * 16f;
        /// <summary>No valid target for this long (10 s): the fight is over and it dissolves.</summary>
        private const int LostTargetDissolveTicks = 600;
        private const int DissolveTicks = 60;

        /// <summary>Run speed is the triggering player's, clamped to this range (px/tick).</summary>
        private const float MinTopSpeed = 3f;
        private const float MaxTopSpeed = 6f;
        private const float Acceleration = 0.15f;
        /// <summary>During a wrapper recovery it walks at this fraction of its speed (§12.6 "slow walk").</summary>
        private const float RecoveryWalkSpeedFraction = 0.4f;
        /// <summary>Planted while attacking: per-tick horizontal velocity kept during a tell or burst.</summary>
        private const float AttackingVelocityRetention = 0.85f;
        /// <summary>Stand this much further than a shot's minimum firing distance, so it isn't refused at the edge.</summary>
        private const float ShotDistanceMargin = 48f;
        private const float EngagementSlack = 12f;
        /// <summary>The waypoint is re-sent to SF4 only once the desired spot moves 2 tiles (SF4 replans on any move).</summary>
        private const float WaypointRefreshDistance = 32f;
        /// <summary>SmartFighter4 door value: opens doors, doesn't destroy them (SF4's own default).</summary>
        private const int DoorOpenValue = 4;
        /// <summary>Too close for its shot this long, or out of reach with its weapon this long: switch weapon.</summary>
        private const int TooCloseSwitchTicks = 30;
        private const int OutOfReachSwitchTicks = 120;

        /// <summary>Winded below 15% of max stamina, fights again at 60% (same hysteresis as the Soul Mimic). While winded
        /// it backs off to 4.5 tiles if the target is within 3, else holds.</summary>
        private const float WindedEnterFraction = 0.15f;
        private const float WindedExitFraction = 0.6f;
        private const float WindedNearDistance = 3f * 16f;
        private const float WindedSettleDistance = 4.5f * 16f;

        /// <summary>Evasion: reacts to a given threat with this chance, one decision per lockout. Lower than the Soul
        /// Mimic's 0.75 — an enemy that rolls everything is a wall, not a duel.</summary>
        private const float EvadeChance = 0.5f;
        private const int EvadeDecisionLockoutTicks = 20;
        /// <summary>A player within 5 tiles, facing it, mid-swing with a melee weapon, is a melee threat.</summary>
        private const float MeleeThreatRange = 5f * 16f;
        private const float JumpEvadeSpeed = 7.5f;
        private const float StrafeDistance = 5f * 16f;
        private const int StrafeTicks = 24;

        /// <summary>Estus (§12.4): at most 3 charges; each drink heals this fraction of max life over the player's
        /// default heal time. The server can't read a player's Estus stats, so these are fixed, not copied.</summary>
        private const int MaxEstusCharges = 3;
        private const float EstusHealLifeFraction = 0.12f;
        private const float EstusDrinkLifeFraction = 0.4f;
        private static readonly int EstusDrinkTicks = (int)(EstusFlaskPlayer.EstusDrinkTimerMaxBase * 60f);
        private static readonly int EstusHealTicks = (int)EstusFlaskPlayer.DefaultEstusHealingTimerMax;
        private const float EstusSpaceNearDistance = 4f * 16f;
        private const float EstusSpaceSettleDistance = 6f * 16f;
        private const int EstusSpaceTimeoutTicks = 150;
        private const float DrinkingVelocityRetention = 0.9f;
        private const float DrinkPoseStartFraction = 0.05f;

        /// <summary>Summoner adds (§12.9, ESTIMATE): at most this many alive, and never more than the number of staffs. A
        /// cast is the staff held up for SummonHoldTicks with ghost dust gathering at the summon point (3 tiles toward the
        /// target, dropped to the ground), then swept down over SummonSweepTicks as the add arrives. One cast per
        /// SummonCooldownTicks (the first may come SummonFirstDelayTicks after spawning). Never mid-attack.</summary>
        private const int MaxAliveAdds = 2;
        private const int SummonHoldTicks = 38;
        private const int SummonSweepTicks = 12;
        private const int SummonCooldownTicks = 900;
        private const int SummonFirstDelayTicks = 180;
        private const float SummonPointDistance = 3f * 16f;
        /// <summary>The adds' health (Expert value, mode-scaled like the regular-enemy registry), set by the user
        /// 2026-10-21: the Studded Leather Warrior and the Black Ninja are far tougher as mirror adds than their own boss
        /// values (4,000 / 6,000).</summary>
        private const int MirrorAddLifeHardmode = 25000;
        private const int MirrorAddLifeSuperHardmode = 100000;
        private const int SummonGroundSearchTiles = 12;

        /// <summary>Fallback weapons when fewer than 2 usable ones were copied (§12.11 #5): always a melee and a ranged
        /// option. The pistol fires basic ammo (UsesBasicAmmo), so it works whatever the host carries.</summary>
        private static readonly int[] FallbackWeaponTypes = { ItemID.NightsEdge, ItemID.FlintlockPistol };
        private const int MinUsableWeapons = 2;

        /// <summary>OwnerFlags bits (host → server). Attacking = a tell or burst in progress (the wrapper's Tell/Using).</summary>
        private const byte OwnerFlagWinded = 1;
        private const byte OwnerFlagCanRoll = 2;
        private const byte OwnerFlagAttacking = 4;
        private const byte OwnerFlagTooClose = 8;
        private const byte OwnerFlagRecovering = 16;
        private const byte OwnerFlagUseFailed = 32;

        // Loadout (synced): item type + prefix per usable hotbar weapon. Items are rebuilt from them on every machine.
        private readonly int[] _loadoutTypes = new int[HotbarSlots];
        private readonly int[] _loadoutPrefixes = new int[HotbarSlots];
        private int _loadoutCount;
        private int _activeSlot;
        private Item[] _loadoutItems;
        private bool _loadoutItemsDirty = true;
        private int _estusCharges = MaxEstusCharges;

        // Minion staffs from the hotbar (synced): each one calls adds instead of being used. Which one the current cast
        // shows, and where its add arrives.
        private readonly int[] _summonStaffTypes = new int[HotbarSlots];
        private int _summonStaffCount;
        private int _summonStaffShown;
        private Vector2 _summonPoint;
        private Item _shownStaffItem;

        // Server only.
        private readonly List<int> _weaponBag = new List<int>();
        private int _retargetTimer;
        private int _lostTargetTicks;
        private int _lastLife;
        private int _lastHitPlayer = -1;
        private int _lastHitTicksAgo = int.MaxValue / 2;
        private bool _wasRecovering;
        private bool _wasUseFailed;
        private int _tooCloseTicks;
        private int _outOfReachTicks;
        private int _rollDirection;
        private int _rollCooldown;
        private int _evadeLockout;
        private int _strafeTicksLeft;
        private int _strafeDirection;
        private Vector2 _heldWaypoint;
        private bool _hasHeldWaypoint;
        private int _estusHealTicksLeft;
        private float _estusHealPerTick;
        private float _estusHealAccumulator;
        private int _summonCooldown = SummonFirstDelayTicks;
        private readonly List<int> _addIndices = new List<int>();
        private readonly List<int> _addTypes = new List<int>();

        // All machines: a local timer for the current state (reset when ai[3] changes).
        private InvaderState _lastSeenState = InvaderState.Fight;
        private int _stateTimer;

        // Host only.
        private InvaderState _ownerLastState = InvaderState.Fight;

        public InvaderState State
        {
            get
            {
                return (InvaderState)(int)NPC.ai[3];
            }
            set
            {
                NPC.ai[3] = (int)value;
            }
        }

        private int TriggerIndex => (int)NPC.ai[1];

        private int TargetIndex => (int)NPC.ai[2];

        protected override int LookSourceIndex => TriggerIndex;

        protected override Item ActiveWeapon
        {
            get
            {
                if (_loadoutItemsDirty || _loadoutItems == null)
                {
                    // Rebuilt only when the synced loadout changes, so the wrapper's per-item state (its "weapon changed"
                    // check, the muted sound) keeps pointing at the same Item objects.
                    _loadoutItems = new Item[HotbarSlots];

                    for (int slot = 0; slot < _loadoutCount; slot++)
                    {
                        Item copy = new Item();
                        copy.SetDefaults(_loadoutTypes[slot]);
                        copy.Prefix(_loadoutPrefixes[slot]);
                        _loadoutItems[slot] = copy;
                    }

                    _loadoutItemsDirty = false;
                }

                bool slotValid = _activeSlot >= 0 && _activeSlot < _loadoutCount;

                if (!slotValid)
                {
                    return null;
                }

                return _loadoutItems[_activeSlot];
            }
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.lifeMax = UnregisteredLifeMax;
            NPC.dontTakeDamage = false;
            NPC.immortal = false;
            NPC.HitSound = SoundID.PlayerHit;
            NPC.DeathSound = SoundID.PlayerKilled;
            NPC.knockBackResist = KnockbackTaken;
            NPC.value = 0f;
            NPC.ai[2] = -1f;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanUseRopes = true;
        }

        // ── Spawning (server / single player) ─────────────────────────────────────────────────────

        /// <summary>Spawns an invader copying <paramref name="trigger"/> at <paramref name="feetPosition"/> (bottom centre).
        /// Server or single player only. The step-7 event ring will call this; for now /mimictest invader does.</summary>
        public static int SpawnFor(Player trigger, Vector2 feetPosition)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int invaderType = ModContent.NPCType<MirrorInvader>();
            int npcIndex = NPC.NewNPC(trigger.GetSource_FromThis(), (int)feetPosition.X, (int)feetPosition.Y, invaderType, 0, trigger.whoAmI, trigger.whoAmI, trigger.whoAmI, (int)InvaderState.Fight);

            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                return -1;
            }

            NPC npc = Main.npc[npcIndex];
            MirrorInvader invader = (MirrorInvader)npc.ModNPC;

            // Every usable hotbar weapon (§12.8 filter), as type + prefix. Ammo weapons fire the basic ammo of their type
            // (UsesBasicAmmo), so they only need one to exist. Minion staffs are kept apart: they call adds (§12.9).
            for (int slot = 0; slot < HotbarSlots; slot++)
            {
                Item item = trigger.inventory[slot];
                WeaponCategory category = Categorize(item);

                if (category == WeaponCategory.SummonStaff)
                {
                    invader._summonStaffTypes[invader._summonStaffCount] = item.type;
                    invader._summonStaffCount++;
                    continue;
                }

                if (category == WeaponCategory.Unusable)
                {
                    continue;
                }

                if (item.useAmmo > 0 && MirrorPuppetSwap.BasicAmmoFor(item.useAmmo) == null)
                {
                    continue;
                }

                invader._loadoutTypes[invader._loadoutCount] = item.type;
                invader._loadoutPrefixes[invader._loadoutCount] = item.prefix;
                invader._loadoutCount++;
            }

            // Fewer than 2 usable weapons (a pure summoner, an empty hotbar): add the fallbacks it doesn't already have.
            if (invader._loadoutCount < MinUsableWeapons)
            {
                for (int i = 0; i < FallbackWeaponTypes.Length; i++)
                {
                    int fallbackType = FallbackWeaponTypes[i];
                    bool alreadyHas = Array.IndexOf(invader._loadoutTypes, fallbackType, 0, invader._loadoutCount) >= 0;

                    if (alreadyHas || invader._loadoutCount >= HotbarSlots)
                    {
                        continue;
                    }

                    invader._loadoutTypes[invader._loadoutCount] = fallbackType;
                    invader._loadoutPrefixes[invader._loadoutCount] = 0;
                    invader._loadoutCount++;
                }
            }

            invader._loadoutItemsDirty = true;
            invader._activeSlot = 0;
            invader._estusCharges = MaxEstusCharges;

            // The copied player's defense; health comes from the EnemyBalance registry (SetDefaults).
            npc.defense = trigger.statDefense;
            npc.defDefense = trigger.statDefense;
            npc.direction = trigger.Center.X < npc.Center.X ? -1 : 1;
            invader._lastLife = npc.life;

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcIndex);
            }

            return npcIndex;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_loadoutCount);

            for (int slot = 0; slot < _loadoutCount; slot++)
            {
                writer.Write(_loadoutTypes[slot]);
                writer.Write((byte)_loadoutPrefixes[slot]);
            }

            writer.Write((byte)_activeSlot);
            writer.Write((byte)_estusCharges);

            writer.Write((byte)_summonStaffCount);

            for (int i = 0; i < _summonStaffCount; i++)
            {
                writer.Write(_summonStaffTypes[i]);
            }

            writer.Write((byte)_summonStaffShown);
            writer.Write((short)_summonPoint.X);
            writer.Write((short)(_summonPoint.Y / 16f));
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int count = reader.ReadByte();
            bool loadoutChanged = count != _loadoutCount;

            for (int slot = 0; slot < count; slot++)
            {
                int type = reader.ReadInt32();
                int prefix = reader.ReadByte();
                bool slotChanged = slot >= HotbarSlots || type != _loadoutTypes[slot] || prefix != _loadoutPrefixes[slot];

                if (slot < HotbarSlots)
                {
                    _loadoutTypes[slot] = type;
                    _loadoutPrefixes[slot] = prefix;
                }

                loadoutChanged = loadoutChanged || slotChanged;
            }

            _loadoutCount = Math.Min(count, HotbarSlots);
            _activeSlot = reader.ReadByte();
            _estusCharges = reader.ReadByte();

            int staffCount = reader.ReadByte();

            for (int i = 0; i < staffCount; i++)
            {
                int staffType = reader.ReadInt32();

                if (i < HotbarSlots)
                {
                    _summonStaffTypes[i] = staffType;
                }
            }

            _summonStaffCount = Math.Min(staffCount, HotbarSlots);
            _summonStaffShown = reader.ReadByte();

            // X in px; Y in tiles (world height in px overflows a short).
            float summonX = reader.ReadInt16();
            float summonTileY = reader.ReadInt16();
            _summonPoint = new Vector2(summonX, summonTileY * 16f);

            if (loadoutChanged)
            {
                _loadoutItemsDirty = true;
            }
        }

        public override void OnKill()
        {
            RemoveAdds();
        }

        /// <summary>Server: its ghost adds go with it (dead or dissolved). Removed, not killed, so they drop nothing.
        /// Callers: OnKill and the end of Dissolving.</summary>
        private void RemoveAdds()
        {
            for (int i = 0; i < _addIndices.Count; i++)
            {
                NPC add = Main.npc[_addIndices[i]];

                if (!add.active || add.type != _addTypes[i])
                {
                    continue;
                }

                add.active = false;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, _addIndices[i]);
                }
            }

            _addIndices.Clear();
            _addTypes.Clear();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Guaranteed (§12.11 #4).
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MimicRune>()));
        }

        // ── AI (server decides; every machine runs the local state timer and visuals) ──────────────

        public override void AI()
        {
            base.AI();

            InvaderState state = State;

            if (state != _lastSeenState)
            {
                bool startedRoll = state == InvaderState.Rolling && !Main.dedServ;
                bool finishedEstus = _lastSeenState == InvaderState.DrinkingEstus && !Main.dedServ;
                bool startedDissolve = state == InvaderState.Dissolving && !Main.dedServ;

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

                if (finishedEstus)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
                    SoundEngine.PlaySound(SoundID.Item3, NPC.Center);
                }

                if (startedDissolve)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.5f }, NPC.Center);
                }

                bool startedSummon = state == InvaderState.SummoningAdd && !Main.dedServ;
                bool finishedSummon = _lastSeenState == InvaderState.SummoningAdd && !Main.dedServ;

                if (startedSummon)
                {
                    SoundEngine.PlaySound(SoundID.Item44 with { Pitch = -0.3f }, NPC.Center);
                }

                // The add arrives: a burst of ghost dust where it formed (the add's own spawn is a plain NewNPC).
                if (finishedSummon)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = 0.4f, Volume = 0.7f }, _summonPoint);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 burstVelocity = Main.rand.NextVector2Circular(4f, 4f);
                        Dust burst = Dust.NewDustPerfect(_summonPoint + new Vector2(0f, -21f), DustID.DungeonSpirit, burstVelocity, 0, default, 1.5f);
                        burst.noGravity = true;
                    }
                }

                _lastSeenState = state;
                _stateTimer = 0;
            }
            else
            {
                _stateTimer++;
            }

            bool rolling = state == InvaderState.Rolling;
            bool dissolving = state == InvaderState.Dissolving;

            // A roll is immune for its whole length, like the player's; dissolving can't be hit either.
            NPC.dontTakeDamage = rolling || dissolving;
            NPC.behindTiles = dissolving;

            if (dissolving && !Main.dedServ)
            {
                // Sinks into a dark pool, with silver glints: the reflection going out.
                Vector2 poolPosition = NPC.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), -2f);
                Dust poolDust = Dust.NewDustPerfect(poolPosition, DustID.Asphalt, new Vector2(0f, -0.2f), 0, default, 1.3f);
                poolDust.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    Dust glint = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.SilverCoin, 0f, -1f, 0, default, 0.8f);
                    glint.noGravity = true;
                }
            }

            // Summon telegraph (every machine): ghost dust spiralling in on the summon point, denser as the cast goes on, so
            // the arrival spot is readable before anything is there (attack-quality-pass §7).
            if (state == InvaderState.SummoningAdd && !Main.dedServ)
            {
                float castProgress = Math.Clamp(_stateTimer / (float)(SummonHoldTicks + SummonSweepTicks), 0f, 1f);
                int gatherCount = 1 + (int)(castProgress * 3f);
                Vector2 formCenter = _summonPoint + new Vector2(0f, -21f);

                for (int i = 0; i < gatherCount; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(40f, 40f) * (1f - castProgress * 0.5f);
                    Vector2 inward = -offset * 0.08f;
                    Dust gather = Dust.NewDustPerfect(formCenter + offset, DustID.DungeonSpirit, inward, 0, default, 1.1f);
                    gather.noGravity = true;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (dissolving)
            {
                NPC.velocity.X = 0f;

                if (_stateTimer >= DissolveTicks)
                {
                    RemoveAdds();
                    NPC.active = false;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
                    }
                }

                return;
            }

            // ── Who hit it last (NPC.lastInteraction reaches the server with every player strike) ──
            _lastHitTicksAgo++;

            if (NPC.life < _lastLife)
            {
                int hitter = NPC.lastInteraction;
                bool hitterValid = hitter >= 0 && hitter < Main.maxPlayers;

                if (hitterValid)
                {
                    _lastHitPlayer = hitter;
                    _lastHitTicksAgo = 0;
                }
            }

            _lastLife = NPC.life;

            // ── Target: keep the current one; on the retarget timer prefer whoever just hit it, else a much nearer player ──
            Player target = null;
            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxPlayers;

            if (targetIndexValid)
            {
                target = Main.player[TargetIndex];
            }

            bool targetValid = target != null && target.active && !target.dead && Vector2.Distance(target.Center, NPC.Center) <= TargetLeash;

            if (!targetValid)
            {
                target = null;
            }

            _retargetTimer--;

            if (target == null || _retargetTimer <= 0)
            {
                _retargetTimer = RetargetIntervalTicks;
                Player nearest = null;
                float nearestDistance = TargetLeash;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];

                    if (!candidate.active || candidate.dead)
                    {
                        continue;
                    }

                    float candidateDistance = Vector2.Distance(candidate.Center, NPC.Center);

                    if (candidateDistance < nearestDistance)
                    {
                        nearestDistance = candidateDistance;
                        nearest = candidate;
                    }
                }

                Player recentHitter = null;
                bool hitterRemembered = _lastHitPlayer >= 0 && _lastHitTicksAgo <= LastHitMemoryTicks;

                if (hitterRemembered)
                {
                    Player hitterPlayer = Main.player[_lastHitPlayer];
                    bool hitterAvailable = hitterPlayer.active && !hitterPlayer.dead && Vector2.Distance(hitterPlayer.Center, NPC.Center) <= TargetLeash;

                    if (hitterAvailable)
                    {
                        recentHitter = hitterPlayer;
                    }
                }

                if (target == null)
                {
                    target = recentHitter ?? nearest;
                }
                else if (recentHitter != null)
                {
                    target = recentHitter;
                }
                else if (nearest != null && nearest != target)
                {
                    float currentDistance = Vector2.Distance(target.Center, NPC.Center);

                    if (nearestDistance < currentDistance * RetargetCloserFraction)
                    {
                        target = nearest;
                    }
                }
            }

            int newTargetIndex = -1;

            if (target != null)
            {
                newTargetIndex = target.whoAmI;
            }

            if (newTargetIndex != TargetIndex)
            {
                NPC.ai[2] = newTargetIndex;
                NPC.netUpdate = true;
            }

            // Nobody left to fight (everyone dead, gone or out of range) for long enough: the fight is over.
            if (target == null)
            {
                _lostTargetTicks++;
                NPC.velocity.X *= 0.8f;

                if (_lostTargetTicks >= LostTargetDissolveTicks)
                {
                    State = InvaderState.Dissolving;
                    NPC.netUpdate = true;
                }

                return;
            }

            _lostTargetTicks = 0;

            // ── Host: the client that runs the swap. Re-assigned if it died, left or is too far to simulate it ──
            Player host = Main.player[OwnerIndex];
            bool hostValid = host.active && !host.dead && Vector2.Distance(host.Center, NPC.Center) <= HostMaxDistance;

            if (!hostValid)
            {
                NPC.ai[0] = target.whoAmI;
                NPC.netUpdate = true;
            }

            // ── Timers ──
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

            // ── Dodge roll in progress: full speed for the first half, then coast and decay (the player's profile) ──
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
                    State = InvaderState.Fight;
                    NPC.netUpdate = true;
                }

                return;
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
                    _lastLife = NPC.life;
                }

                if (_estusHealTicksLeft % 10 == 0)
                {
                    NPC.netUpdate = true;
                }
            }

            if (state == InvaderState.DrinkingEstus)
            {
                NPC.velocity.X *= DrinkingVelocityRetention;

                if (_stateTimer >= EstusDrinkTicks)
                {
                    int healAmount = (int)(NPC.lifeMax * EstusHealLifeFraction);
                    _estusCharges--;
                    _estusHealTicksLeft = Math.Max(1, EstusHealTicks);
                    _estusHealPerTick = healAmount / (float)_estusHealTicksLeft;
                    _estusHealAccumulator = 0f;
                    NPC.HealEffect(healAmount);
                    State = InvaderState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            if (state == InvaderState.SwitchingWeapon && _stateTimer >= WeaponSwitchTicks)
            {
                State = InvaderState.Fight;
                state = InvaderState.Fight;
                NPC.netUpdate = true;
            }

            bool attacking = (OwnerFlags & OwnerFlagAttacking) != 0;
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;
            float targetDistance = Vector2.Distance(NPC.Center, target.Center);

            // ── Summoner adds: forget dead/replaced adds, cast when below the cap, spawn at the end of the cast ──
            for (int i = _addIndices.Count - 1; i >= 0; i--)
            {
                NPC add = Main.npc[_addIndices[i]];
                bool addAlive = add.active && add.type == _addTypes[i];

                if (!addAlive)
                {
                    _addIndices.RemoveAt(i);
                    _addTypes.RemoveAt(i);
                }
            }

            if (_summonCooldown > 0)
            {
                _summonCooldown--;
            }

            if (state == InvaderState.SummoningAdd)
            {
                NPC.velocity.X *= 0.8f;
                FaceTarget(target);

                if (_stateTimer >= SummonHoldTicks + SummonSweepTicks)
                {
                    // Tier-appropriate add (§12.9 "an existing enemy" with its real AI), chosen by the user: the Studded
                    // Leather Warrior up to Hardmode, the Black Ninja in SHM — both puppets, drawn in the Soul Mimic's
                    // obsidian shell (PuppetNPC.MirrorShell), which also makes them non-bosses with no banner and no loot.
                    int addType = ModContent.NPCType<Puppets.StuddedLeatherWarrior>();
                    int addExpertLife = MirrorAddLifeHardmode;

                    if (tsorcRevampWorld.SuperHardMode)
                    {
                        addType = ModContent.NPCType<Puppets.BlackNinja>();
                        addExpertLife = MirrorAddLifeSuperHardmode;
                    }

                    int addIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)_summonPoint.X, (int)_summonPoint.Y, addType, 0, 0f, 0f, 0f, 0f, target.whoAmI);
                    bool addSpawned = addIndex >= 0 && addIndex < Main.maxNPCs;

                    if (addSpawned)
                    {
                        NPC add = Main.npc[addIndex];
                        add.SpawnedFromStatue = true;
                        add.value = 0f;
                        add.boss = false;

                        if (add.ModNPC is Puppets.PuppetNPC addPuppet)
                        {
                            addPuppet.MirrorShell = true;
                        }

                        // Normal 0.5x / Expert 1x / Master 1.275x of the Expert value (EnemyBalance.ModeScaledLife); synced
                        // to clients in PuppetNPC's extra AI with the MirrorShell flag.
                        add.lifeMax = Systems.EnemyBalance.ModeScaledLife(add, addExpertLife);
                        add.life = add.lifeMax;

                        add.direction = Math.Sign(target.Center.X - add.Center.X);
                        _addIndices.Add(addIndex);
                        _addTypes.Add(addType);

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, addIndex);
                        }
                    }

                    _summonCooldown = SummonCooldownTicks;
                    State = InvaderState.Fight;
                    NPC.netUpdate = true;
                }

                return;
            }

            int addCap = Math.Min(MaxAliveAdds, _summonStaffCount);
            bool canSummon = state == InvaderState.Fight && !attacking && !winded && _summonCooldown == 0 && _addIndices.Count < addCap;

            if (canSummon)
            {
                // Summon point: 3 tiles toward the target, on the first solid ground from one tile above its feet down to
                // 12 tiles below, with 3 clear tiles of headroom over it. No such spot (a wall, a pit) → its own feet.
                // Feet position, as NewNPC takes the bottom centre.
                int towardTarget = Math.Sign(target.Center.X - NPC.Center.X);

                if (towardTarget == 0)
                {
                    towardTarget = NPC.direction;
                }

                float pointX = NPC.Center.X + towardTarget * SummonPointDistance;
                int pointTileX = (int)(pointX / 16f);
                int feetTileY = (int)((NPC.Bottom.Y - 1f) / 16f);
                Vector2 summonFeet = NPC.Bottom;

                for (int drop = 0; drop <= SummonGroundSearchTiles; drop++)
                {
                    int groundTileY = feetTileY + drop;

                    if (!UsefulFunctions.IsTileReallySolid(pointTileX, groundTileY))
                    {
                        continue;
                    }

                    bool headroomClear = !UsefulFunctions.IsTileReallySolid(pointTileX, groundTileY - 1)
                        && !UsefulFunctions.IsTileReallySolid(pointTileX, groundTileY - 2)
                        && !UsefulFunctions.IsTileReallySolid(pointTileX, groundTileY - 3);

                    if (headroomClear)
                    {
                        summonFeet = new Vector2(pointX, groundTileY * 16f);
                    }

                    break;
                }

                _summonPoint = summonFeet;
                _summonStaffShown = (_summonStaffShown + 1) % Math.Max(1, _summonStaffCount);
                State = InvaderState.SummoningAdd;
                NPC.netUpdate = true;
                return;
            }

            // ── Estus: never mid-attack; back off a player within 4 tiles first (gives up after 2.5 s and drinks) ──
            bool lowLife = NPC.life < NPC.lifeMax * EstusDrinkLifeFraction;
            bool wantsEstus = lowLife && _estusCharges > 0 && _estusHealTicksLeft == 0;
            bool canPrepare = state == InvaderState.Fight || state == InvaderState.PreparingEstus;
            Player crowdingPlayer = null;

            if (canPrepare && wantsEstus)
            {
                float nearestCrowdDistance = EstusSpaceNearDistance;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];

                    if (!candidate.active || candidate.dead)
                    {
                        continue;
                    }

                    float candidateDistance = Vector2.Distance(candidate.Center, NPC.Center);

                    if (candidateDistance < nearestCrowdDistance)
                    {
                        nearestCrowdDistance = candidateDistance;
                        crowdingPlayer = candidate;
                    }
                }

                bool gaveUpOnSpace = state == InvaderState.PreparingEstus && _stateTimer >= EstusSpaceTimeoutTicks;
                bool hasSpace = crowdingPlayer == null || gaveUpOnSpace;

                if (hasSpace && !attacking)
                {
                    State = InvaderState.DrinkingEstus;
                    NPC.netUpdate = true;
                    return;
                }

                if (state == InvaderState.Fight)
                {
                    State = InvaderState.PreparingEstus;
                    state = InvaderState.PreparingEstus;
                    NPC.netUpdate = true;
                }
            }
            else if (state == InvaderState.PreparingEstus)
            {
                State = InvaderState.Fight;
                state = InvaderState.Fight;
                NPC.netUpdate = true;
            }

            // ── Evasion: a player shot about to hit, or a player swinging at it up close → roll / hop / sidestep ──
            // Never mid-attack: a committed tell or burst is the player's punish window (§12.10).
            bool canEvade = state == InvaderState.Fight && !attacking;
            bool threatFound = false;
            bool threatIsMelee = false;
            Vector2 threatVelocity = Vector2.Zero;
            Vector2 threatOrigin = NPC.Center;

            if (canEvade)
            {
                threatFound = FindIncomingProjectile(true, out _, out threatVelocity, out threatOrigin);
            }

            if (canEvade && !threatFound)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player swinger = Main.player[i];

                    if (!swinger.active || swinger.dead || swinger.itemAnimation <= 0)
                    {
                        continue;
                    }

                    Item swingerWeapon = swinger.HeldItem;
                    bool meleeSwing = swingerWeapon != null && swingerWeapon.damage > 0 && !swingerWeapon.noMelee;
                    bool facingUs = Math.Sign(NPC.Center.X - swinger.Center.X) == swinger.direction;
                    bool inReach = Vector2.Distance(swinger.Center, NPC.Center) <= MeleeThreatRange;

                    if (meleeSwing && facingUs && inReach)
                    {
                        threatFound = true;
                        threatIsMelee = true;
                        threatOrigin = swinger.Center;
                        break;
                    }
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
                    int awayDirection = Math.Sign(NPC.Center.X - threatOrigin.X);

                    if (awayDirection == 0)
                    {
                        awayDirection = -NPC.direction;
                    }

                    if (canRoll)
                    {
                        // A level shot is rolled THROUGH (the i-frames carry it past); a swing or a lob is rolled away from.
                        int rollDirection = awayDirection;

                        if (!threatIsMelee && mostlyHorizontal)
                        {
                            rollDirection = -Math.Sign(threatVelocity.X);
                        }

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
                            State = InvaderState.Rolling;
                            NPC.netUpdate = true;
                            return;
                        }
                    }

                    if (grounded && !threatIsMelee && mostlyHorizontal)
                    {
                        NPC.velocity.Y = -JumpEvadeSpeed;
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        _strafeDirection = awayDirection;
                        _strafeTicksLeft = StrafeTicks;
                    }
                }
            }

            // ── Weapon choice: a shuffled bag of loadout slots (attack-quality-pass §6), filtered by range ──
            // A new weapon after every burst (the recovery edge), when its shot keeps being refused for distance, when
            // the current one can't reach for a while, or when a use failed to start (no ammo/mana on the host).
            Item activeItem = ActiveWeapon;
            float preferredDistance = RangedPreferred;
            float maximumDistance = RangedMaximum;
            float minimumShotDistance = 0f;

            if (activeItem != null)
            {
                GetEngagementRange(activeItem, out preferredDistance, out maximumDistance);
                minimumShotDistance = MinimumFiringDistance(activeItem);
            }

            bool recovering = (OwnerFlags & OwnerFlagRecovering) != 0;
            bool useFailed = (OwnerFlags & OwnerFlagUseFailed) != 0;
            bool tooClose = (OwnerFlags & OwnerFlagTooClose) != 0;
            bool justEnteredRecovery = recovering && !_wasRecovering;
            bool justFailed = useFailed && !_wasUseFailed;
            _wasRecovering = recovering;
            _wasUseFailed = useFailed;

            if (tooClose)
            {
                _tooCloseTicks++;
            }
            else
            {
                _tooCloseTicks = 0;
            }

            if (targetDistance > maximumDistance)
            {
                _outOfReachTicks++;
            }
            else
            {
                _outOfReachTicks = 0;
            }

            bool wantsNewWeapon = justEnteredRecovery || justFailed || _tooCloseTicks >= TooCloseSwitchTicks || _outOfReachTicks >= OutOfReachSwitchTicks;
            bool canSwitch = state == InvaderState.Fight && !attacking && _loadoutCount > 1;

            if (wantsNewWeapon && canSwitch)
            {
                _tooCloseTicks = 0;
                _outOfReachTicks = 0;
                int pickedSlot = -1;

                // Refill: every slot but the current one, shuffled (never an immediate repeat).
                if (_weaponBag.Count == 0)
                {
                    for (int slot = 0; slot < _loadoutCount; slot++)
                    {
                        if (slot != _activeSlot)
                        {
                            _weaponBag.Add(slot);
                        }
                    }

                    for (int i = _weaponBag.Count - 1; i > 0; i--)
                    {
                        int swapIndex = Main.rand.Next(i + 1);
                        int held = _weaponBag[i];
                        _weaponBag[i] = _weaponBag[swapIndex];
                        _weaponBag[swapIndex] = held;
                    }
                }

                // First slot in bag order that fits the current distance; none fits → the bag's head anyway (it walks
                // to that weapon's distance).
                for (int i = 0; i < _weaponBag.Count; i++)
                {
                    Item candidate = _loadoutItems[_weaponBag[i]];
                    GetEngagementRange(candidate, out _, out float candidateMaximum);
                    float candidateMinimum = MinimumFiringDistance(candidate);
                    bool fits = targetDistance <= candidateMaximum && targetDistance >= candidateMinimum;

                    if (fits)
                    {
                        pickedSlot = _weaponBag[i];
                        break;
                    }
                }

                if (pickedSlot < 0 && _weaponBag.Count > 0)
                {
                    pickedSlot = _weaponBag[0];
                }

                if (pickedSlot >= 0 && pickedSlot != _activeSlot)
                {
                    _weaponBag.Remove(pickedSlot);
                    _activeSlot = pickedSlot;
                    State = InvaderState.SwitchingWeapon;
                    state = InvaderState.SwitchingWeapon;
                    NPC.netUpdate = true;

                    activeItem = ActiveWeapon;
                    GetEngagementRange(activeItem, out preferredDistance, out maximumDistance);
                    minimumShotDistance = MinimumFiringDistance(activeItem);
                }
            }

            // ── Movement (SmartFighter4 waypoint mode toward a spot at the weapon's distance, on its own side) ──
            NPC.target = target.whoAmI;
            Player trigger = Main.player[Math.Clamp(TriggerIndex, 0, Main.maxPlayers - 1)];
            float topSpeed = Math.Clamp(Math.Max(trigger.maxRunSpeed, trigger.accRunSpeed), MinTopSpeed, MaxTopSpeed);

            // Planted while a tell or burst plays (the player's punish read), and drinking Estus slows it like the player.
            if (attacking)
            {
                NPC.velocity.X *= AttackingVelocityRetention;
                FaceTarget(target);
                return;
            }

            if (recovering)
            {
                topSpeed *= RecoveryWalkSpeedFraction;
            }

            int sideOfTarget = Math.Sign(NPC.Center.X - target.Center.X);

            if (sideOfTarget == 0)
            {
                sideOfTarget = -NPC.direction;
            }

            Vector2 waypoint;

            if (state == InvaderState.PreparingEstus)
            {
                if (crowdingPlayer == null)
                {
                    NPC.velocity.X *= 0.8f;
                    return;
                }

                int sideOfCrowder = Math.Sign(NPC.Center.X - crowdingPlayer.Center.X);

                if (sideOfCrowder == 0)
                {
                    sideOfCrowder = -NPC.direction;
                }

                waypoint = crowdingPlayer.Center + new Vector2(sideOfCrowder * EstusSpaceSettleDistance, 0f);
            }
            else if (_strafeTicksLeft > 0)
            {
                waypoint = NPC.Center + new Vector2(_strafeDirection * StrafeDistance, 0f);
            }
            else if (winded)
            {
                // Catching its breath: only gives ground if the target is right on top of it.
                float gapToTarget = Math.Abs(NPC.Center.X - target.Center.X);

                if (gapToTarget >= WindedNearDistance)
                {
                    NPC.velocity.X *= 0.8f;
                    FaceTarget(target);
                    return;
                }

                waypoint = target.Center + new Vector2(sideOfTarget * WindedSettleDistance, 0f);
            }
            else
            {
                // A shot stands outside its minimum firing distance; anything else at its preferred distance.
                float standDistance = preferredDistance;

                if (minimumShotDistance > 0f)
                {
                    standDistance = Math.Max(standDistance, minimumShotDistance + ShotDistanceMargin);
                }

                waypoint = target.Center + new Vector2(sideOfTarget * standDistance, 0f);
            }

            float horizontalGap = Math.Abs(waypoint.X - NPC.Center.X);
            bool closeEnough = horizontalGap <= EngagementSlack && Math.Abs(waypoint.Y - NPC.Center.Y) < 64f;

            if (closeEnough)
            {
                NPC.velocity.X *= 0.8f;
                FaceTarget(target);
                return;
            }

            bool waypointDrifted = !_hasHeldWaypoint || Vector2.Distance(waypoint, _heldWaypoint) > WaypointRefreshDistance;

            if (waypointDrifted)
            {
                _heldWaypoint = waypoint;
                _hasHeldWaypoint = true;
            }

            SmartFighter4AI.Run(NPC, topSpeed, Acceleration, doorBreakingDamage: DoorOpenValue, movementWaypoint: _heldWaypoint);
        }

        /// <summary>Server: face the target while standing (moving, SF4 faces the way it walks). Callers: the planted,
        /// winded and in-position branches of the movement block.</summary>
        private void FaceTarget(Player target)
        {
            int facing = Math.Sign(target.Center.X - NPC.Center.X);

            if (facing != 0 && facing != NPC.direction)
            {
                NPC.direction = facing;
                NPC.netUpdate = true;
            }
        }

        /// <summary>Shots (not near-instant ones) refuse to fire inside effective speed x 20 ticks (the wrapper's rule,
        /// HostileMirrorPuppetNPC.TooCloseToFire). 0 for everything else. Callers: weapon choice and movement.</summary>
        private static float MinimumFiringDistance(Item item)
        {
            WeaponCategory category = Categorize(item);
            bool shotRules = category == WeaponCategory.Shot || category == WeaponCategory.Channelled;

            if (!shotRules)
            {
                return 0f;
            }

            return EffectiveShotSpeed(item) * ShotReactionTicks;
        }

        // ── Host side: stamina flags, roll cost, the target for the wrapper ─────────────────────────

        protected override void UpdateOwnerSide(Player owner)
        {
            // Winded: its own stamina pool ran low → no new attacks, and the server's movement gives it a breather.
            // Only for classes that pay weapon stamina (as for the player). Hysteresis 15% / 60%.
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

            // The server starts a roll; its stamina is charged here, at the host's roll price, overdrawing to zero at most.
            InvaderState currentState = State;
            bool rollJustStarted = currentState == InvaderState.Rolling && _ownerLastState != InvaderState.Rolling;

            if (rollJustStarted)
            {
                float rollCost = owner.GetModPlayer<tsorcRevampPlayer>().RollStaminaCost;
                StaminaCurrent = Math.Max(0f, StaminaCurrent - rollCost);
            }

            _ownerLastState = currentState;

            // Wrapper state for the server (as of last tick's decision; one tick of lag is fine for movement).
            bool attacking = CurrentUsePhase == UsePhase.Tell || CurrentUsePhase == UsePhase.Using || UseState.ItemAnimation > 0;
            byte flags = 0;

            if (winded)
            {
                flags |= OwnerFlagWinded;
            }

            if (StaminaCurrent > 0f)
            {
                flags |= OwnerFlagCanRoll;
            }

            if (attacking)
            {
                flags |= OwnerFlagAttacking;
            }

            if (TooCloseToFire)
            {
                flags |= OwnerFlagTooClose;
            }

            if (CurrentUsePhase == UsePhase.Recovery)
            {
                flags |= OwnerFlagRecovering;
            }

            if (LastUseFailed)
            {
                flags |= OwnerFlagUseFailed;
            }

            OwnerFlags = flags;
        }

        protected override bool ChooseTarget(Player owner, out Vector2 targetCenter)
        {
            targetCenter = NPC.Center;

            // Only while fighting; winded = no new attacks until the pool recovers.
            bool winded = (OwnerFlags & OwnerFlagWinded) != 0;

            if (State != InvaderState.Fight || winded)
            {
                return false;
            }

            bool targetIndexValid = TargetIndex >= 0 && TargetIndex < Main.maxPlayers;

            if (!targetIndexValid)
            {
                return false;
            }

            Player target = Main.player[TargetIndex];

            if (!target.active || target.dead)
            {
                return false;
            }

            Item weapon = ActiveWeapon;

            if (weapon == null || weapon.IsAir)
            {
                return false;
            }

            // Anti-whiff: only attack inside the weapon's reach (the server walks it in); shots also need a clear line.
            GetEngagementRange(weapon, out _, out float maximumDistance);
            float distance = Vector2.Distance(NPC.Center, target.Center);

            if (distance > maximumDistance)
            {
                return false;
            }

            bool rangedWeapon = maximumDistance > SpearLikeMaximum;

            if (rangedWeapon && !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            targetCenter = target.Center;
            return true;
        }

        // ── Display ──────────────────────────────────────────────────────────────────────────────

        protected override void DecorateDisplayClone(Player clone)
        {
            // The flask draw layer reads the drawn player's own Estus state, so setting the CLONE's copy puts the flask
            // in the invader's hand; the raised drinking arm starts 5% into the drink, as for the player.
            EstusFlaskPlayer cloneEstus = clone.GetModPlayer<EstusFlaskPlayer>();
            bool drinkingEstus = State == InvaderState.DrinkingEstus;
            cloneEstus.IsDrinking = drinkingEstus;
            cloneEstus.EstusDrinkTimer = drinkingEstus ? _stateTimer / 60f : 0f;
            cloneEstus.EstusDrinkTimerMax = EstusDrinkTicks / 60f;
            cloneEstus.EstusChargesCurrent = _estusCharges;
            cloneEstus.EstusChargesMax = MaxEstusCharges;

            bool estusPose = drinkingEstus && _stateTimer >= EstusDrinkTicks * DrinkPoseStartFraction;

            if (estusPose)
            {
                clone.bodyFrame = PlayerFrames.Use2.ToRectangle();
            }

            // Summon cast: the staff in hand, held at the top of its swing, then swept down as the add arrives. Vanilla's
            // own pose maths (ItemCheck_ApplyUseStyle) from a hand-set use timer; body frame by the same thirds PlayerFrame
            // uses for a swing (1 = raised, 2 = level, 3 = low).
            bool castingSummon = State == InvaderState.SummoningAdd && _summonStaffCount > 0;

            if (castingSummon)
            {
                int staffType = _summonStaffTypes[Math.Clamp(_summonStaffShown, 0, _summonStaffCount - 1)];

                if (_shownStaffItem == null || _shownStaffItem.type != staffType)
                {
                    _shownStaffItem = new Item();
                    _shownStaffItem.SetDefaults(staffType);
                }

                const int poseTimerMax = 30;
                int sweepTicks = Math.Max(0, _stateTimer - SummonHoldTicks);
                float sweepProgress = Math.Clamp(sweepTicks / (float)SummonSweepTicks, 0f, 1f);
                int poseTimer = Math.Max(1, (int)Math.Round(poseTimerMax * (1f - sweepProgress)));

                clone.inventory[0] = _shownStaffItem;
                clone.selectedItem = 0;
                clone.lastVisualizedSelectedItem = _shownStaffItem;
                clone.itemAnimationMax = poseTimerMax;
                clone.itemAnimation = poseTimer;
                clone.ItemCheck_ApplyUseStyle(clone.HeightOffsetHitboxCenter, _shownStaffItem, Item.GetDrawHitbox(staffType, clone));

                float timerFraction = poseTimer / (float)poseTimerMax;
                int bodyFrameIndex = 3;

                if (timerFraction >= 0.666f)
                {
                    bodyFrameIndex = 1;
                }
                else if (timerFraction >= 0.333f)
                {
                    bodyFrameIndex = 2;
                }

                clone.bodyFrame.Y = clone.bodyFrame.Height * bodyFrameIndex;
            }

            // Dodge roll: one full tumble over the roll's length with tucked legs (tsorcRevampPlayer.UpdateDodging).
            clone.fullRotation = 0f;

            if (State == InvaderState.Rolling)
            {
                float rollProgress = Math.Clamp(_stateTimer / (float)RollTicks, 0f, 1f);
                int rollFacing = NPC.direction == 0 ? 1 : NPC.direction;
                clone.fullRotation = rollFacing * rollProgress * MathHelper.TwoPi;
                clone.fullRotationOrigin = RollRotationOrigin;
                clone.legFrame = PlayerFrames.Jump.ToRectangle();
            }
        }

        /// <summary>Dissolving: sinks into the ground over its length (drawn behind tiles), slow then faster.</summary>
        protected override Vector2 DisplayDrawOffset
        {
            get
            {
                if (State != InvaderState.Dissolving)
                {
                    return Vector2.Zero;
                }

                float sinkProgress = Math.Clamp(_stateTimer / (float)DissolveTicks, 0f, 1f);
                return new Vector2(0f, sinkProgress * sinkProgress * NPC.height);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A hit chips the reflection (a few silver flecks); death shatters it.
            int fleckCount = 3;

            if (NPC.life <= 0)
            {
                fleckCount = 50;
            }

            for (int i = 0; i < fleckCount; i++)
            {
                Dust fleck = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.SilverCoin, hit.HitDirection * 2f, -2f, 0, default, 1.1f);
                fleck.noGravity = NPC.life <= 0;
            }

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Asphalt, hit.HitDirection, -1.5f, 0, default, 1.3f);
                }
            }
        }
    }
}
