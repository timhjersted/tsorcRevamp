using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Systems;
using tsorcRevamp.Systems.ArcaneSorcery;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    // This class stores necessary player info for our custom resource (stamina) that governs the usage of special abilities such as dodge rolls, spins, dashes etc.
    public class tsorcRevampStaminaPlayer : ModPlayer
    {
        public static tsorcRevampStaminaPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampStaminaPlayer>();
        }


        // Here we include a custom resource, similar to mana or health.
        // Creating some variables to define the current value of our Stamina resource as well as the current maximum value. We also include a temporary max value, as well as some variables to handle the natural regeneration of this resource.
        public float staminaResourceCurrent;
        public const float DefaultStaminaResourceMax = 125;
        public float staminaResourceMax;
        public float staminaResourceMax2;
        public float staminaResourceRegenRate;
        /// <summary>
        /// Total stamina regen multiplier from gear, buffs and food. Starts at 1.
        ///
        /// **CONVENTION: BONUSES ARE ADDITIVE — always `+= X / 100f`, never `*= 1f + X / 100f`.**
        ///
        /// Mixing the two made the total order-dependent (a `*=` piece multiplies whatever accumulated before
        /// it, so ring-then-armor and armor-then-ring gave different answers) and made tooltips lie — a "+10%"
        /// armor piece delivered +21% to a player already sitting at 2.15x. Additive keeps every listed number
        /// literally true regardless of what else is worn, and gives diminishing returns for free: +15% is a 15%
        /// improvement at 1.0 but only a 5% improvement at 3.0, with no hidden curve to explain to the player.
        ///
        /// PENALTIES are the exception and may stay multiplicative (see ShieldGuardBreak's `*= 0.2f`): a debuff
        /// meant to cripple regen has to bite proportionally, or a geared player shrugs it off.
        /// </summary>
        public float staminaResourceGainMult = 1;
        public float staminaResourceGain;
        internal float staminaResourceRegenTimer = 0f;
        public static readonly Color RestoreStaminaResource = new Color(20, 100, 20); // We can use this for CombatText, if you create an item that replenishes exampleResourceCurrent.

        public int minionStaminaCap;
        private const float OutputStaminaPerSecond = 39f;
        private const float WeaponOutputEmaWeight = 0.2f;
        private const float PlayerOutputEmaWeight = 0.02f;
        private const float MinimumRelativePower = 0.5f;
        private const float MaximumRelativePower = 3f;
        // Whips pay the same rate as any other weapon. Both archetype multipliers are neutral; the seam is
        // kept for future per-archetype trims.
        private const float WhipStaminaMultiplier = 1f;
        // 1.0, was 0.15. Summon staves no longer get a large discount on their own cast cost — a summon is a
        // deliberate, committed action and should be priced like one. (Their minions still pay separately and
        // continuously via SummonDamageStaminaRate; that drain is NOT what the item tooltip's number shows.)
        // Both archetype multipliers are neutral now; the seam is kept for future per-archetype trims.
        private const float SummonStaffStaminaMultiplier = 1f;
        public const float SummonDamageStaminaRate = 0.02f;

        /// <summary>
        /// Stamina multiplier for pick/axe/hammer swings — deliberately its own knob, separate from
        /// WeaponStaminaMult, so wood-chopping/mining throughput can be tuned without touching
        /// sword/spear combat costs.
        ///
        /// These tools were previously excluded from BOTH the real-time output-based system (see
        /// UsesOutputBasedWeaponCost below) and the per-swing legacy path in PreItemCheck, and instead
        /// paid stamina only in tsorcGlobalItem.OnHitNPC — i.e. only when a swing actually connected
        /// with an enemy. That meant whiffing (or chopping wood/mining, which never fires OnHitNPC at
        /// all) was completely free, and a single connecting swing paid the FULL per-swing cost by
        /// itself, which read as disconnected from the swing rhythm and unusually punishing per hit.
        /// This constant is the base cost now paid on every swing instead (hit, miss, tile, or air),
        /// matching how every other weapon already works. See ToolCombatHitSurcharge for the small
        /// extra now added specifically when the swing lands on an NPC.
        ///
        /// 0.3, down from the original 0.6 — the first pass made pure mining/chopping (no combat
        /// surcharge, ever, since trees/tiles never reach OnHitNPC) drain a full stamina pool in as
        /// little as ~5-7 seconds of continuous swinging on a fast tool like Diamond Pickaxe. Mining
        /// should barely register against the bar; combat is what's supposed to cost real stamina, and
        /// real WEAPON-classified axes/hammers (tsorcRevamp.WeaponClassifiedTools) don't use this
        /// constant at all any more — only genuine utility tools do.
        /// </summary>
        public const float ToolSwingStaminaMult = 0.3f;

        /// <summary>Extra flat stamina charged (before WeaponStaminaMult) on top of the swing cost when a
        /// pick/axe/hammer actually lands on an NPC — so fighting with one of these costs a bit more than
        /// idly chopping wood or mining, without reintroducing the old "only pay when you connect"
        /// behaviour that let the swing cost itself be skipped entirely by whiffing.</summary>
        public const float ToolCombatHitSurcharge = 8f;

        private sealed class WeaponOutputState
        {
            public float DamagePerUseEma;
            /// <summary>Attack-speed-scaled use animation of the activation currently accumulating PendingDamage.
            /// Stored because the damage is banked now but folded into the per-second player baseline later, by
            /// which point the held weapon (and its speed) may have changed.</summary>
            public int PendingUseAnimation = 1;
            public float PendingDamage;
            public bool HasPendingActivation;
        }

        private readonly Dictionary<int, WeaponOutputState> weaponOutputStates = new();
        private float playerDamagePerSecondEma;

        /// <summary>Slow EMA of this player's measured damage per SECOND across ALL weapons — i.e. "how hard you
        /// hit right now", tracking progression automatically. 0 until the player has swung something. Exposed so
        /// abilities that want to scale with the player's own power (Dragon Crest's fire breath) can share the
        /// same yardstick the stamina system uses, instead of maintaining a parallel progression table.
        ///
        /// Per-SECOND, not per-use: comparing per-use figures across weapons of different speeds counts swing
        /// speed twice (see CalculateOutputBasedWeaponCost). Anything consuming this must not divide by a use
        /// animation — it is already a rate.</summary>
        public float PlayerDamagePerSecondEma => playerDamagePerSecondEma;

        /// <summary>This weapon's learned damage-per-use, or 0 if it hasn't been used yet. For telemetry.</summary>
        public float GetWeaponDamagePerUseEma(int itemType)
            => weaponOutputStates.TryGetValue(itemType, out WeaponOutputState state) ? state.DamagePerUseEma : 0f;

        /// <summary>The legacy swing-speed-only stamina cost, kept for the DebugMode side-by-side comparison
        /// in the item tooltip. Not used for gameplay any more — output-based cost is the only live path.</summary>
        public static float LegacyWeaponStaminaCost(int scaledUseAnimation)
            => tsorcRevampPlayer.ReduceStamina(scaledUseAnimation);

        /// <summary>
        /// Whether this item prices its stamina cost off the live, DPS-relative output system rather than
        /// the flat swing-speed formula (ReduceStamina).
        ///
        /// DPS-aware pricing was reverted as the general-roster default 
        /// (in short: cost fluctuating with live measured damage output, including ammo swaps and piercing hits summing
        /// multiple targets into one activation, was reported as inconsistent and punishing for high
        /// damage/DPS builds, and independent verification confirmed the mechanism). CoinGun is the one
        /// deliberate carve-out: it deals 0 base damage but still deals real damage via its own mechanic,
        /// so ReduceStamina has nothing sane to scale against.
        ///
        /// The output-based machinery itself (BeginOutputBasedWeaponUse, GetExpectedOutputBasedWeaponCost,
        /// the EMA tracking) is NOT dead code — GaeBolg/Longinus/DarkTrident call
        /// GetExpectedOutputBasedWeaponCost directly for their own charge-up affordability checks
        /// (never gated behind this switch), Dragon Crest Shield reads PlayerDamagePerSecondEma to scale
        /// its fire breath, and the balance-log telemetry records both EMAs. Keep it running; this
        /// function just stops being what routes the general weapon roster into it.
        /// </summary>
        public static bool UsesOutputBasedWeaponCost(Item item)
        {
            if (item == null || item.IsAir || item.ammo != AmmoID.None || item.consumable)
            {
                return false;
            }

            return item.type == ItemID.CoinGun;
        }

        public float BeginOutputBasedWeaponUse(Item item, int scaledUseAnimation)
        {
            WeaponOutputState state = GetOrCreateWeaponOutputState(item, scaledUseAnimation);
            FinalizePendingWeaponOutput(state); // folds the PREVIOUS activation, using its own stored speed
            state.PendingDamage = 0f;
            state.PendingUseAnimation = Math.Max(1, scaledUseAnimation);
            state.HasPendingActivation = true;
            return CalculateOutputBasedWeaponCost(item, scaledUseAnimation, state.DamagePerUseEma);
        }

        public float GetExpectedOutputBasedWeaponCost(Item item, int scaledUseAnimation)
        {
            float weaponOutput = weaponOutputStates.TryGetValue(item.type, out WeaponOutputState state)
                ? state.DamagePerUseEma
                : GetBootstrapDamage(item);
            return CalculateOutputBasedWeaponCost(item, scaledUseAnimation, weaponOutput);
        }

        public void RecordWeaponOutputDamage(int itemType, int damageDone)
        {
            if (damageDone <= 0
                || !weaponOutputStates.TryGetValue(itemType, out WeaponOutputState state)
                || !state.HasPendingActivation)
            {
                return;
            }

            state.PendingDamage += damageDone;
        }

        public void SpendStaminaForSummonDamage(int damageDone)
        {
            if (damageDone <= 0 || !Player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina)
            {
                return;
            }

            float cost = damageDone * SummonDamageStaminaRate * Player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult;
            staminaResourceCurrent = Math.Max(0f, staminaResourceCurrent - cost);
        }

        private WeaponOutputState GetOrCreateWeaponOutputState(Item item, int scaledUseAnimation)
        {
            if (!weaponOutputStates.TryGetValue(item.type, out WeaponOutputState state))
            {
                state = new WeaponOutputState { DamagePerUseEma = GetBootstrapDamage(item) };
                weaponOutputStates[item.type] = state;
            }

            if (playerDamagePerSecondEma <= 0f)
            {
                playerDamagePerSecondEma = ToDps(state.DamagePerUseEma, scaledUseAnimation);
            }

            return state;
        }

        /// <summary>Convert a damage-per-USE figure into damage per second at a given swing speed.</summary>
        private static float ToDps(float damagePerUse, int scaledUseAnimation)
            => damagePerUse * 60f / Math.Max(1, scaledUseAnimation);

        private void FinalizePendingWeaponOutput(WeaponOutputState state)
        {
            if (!state.HasPendingActivation || state.PendingDamage <= 0f)
            {
                return;
            }

            // Per-weapon EMA stays per-USE: that's the honest measurement of one activation, and it's what the
            // per-swing cost is ultimately derived from. The PLAYER baseline is per-SECOND — see the comment on
            // CalculateOutputBasedWeaponCost for why comparing per-use figures against each other was wrong.
            state.DamagePerUseEma = MathHelper.Lerp(state.DamagePerUseEma, state.PendingDamage, WeaponOutputEmaWeight);
            playerDamagePerSecondEma = MathHelper.Lerp(
                playerDamagePerSecondEma,
                ToDps(state.PendingDamage, state.PendingUseAnimation),
                PlayerOutputEmaWeight);
        }

        /// <summary>
        /// Cost for one activation.
        ///
        /// relativePower compares DAMAGE PER SECOND, not damage per use. Comparing per-use figures counted swing
        /// speed TWICE — once because a slow weapon packs a whole second of damage into a single swing (inflating
        /// its per-use output against a mixed baseline), and again in useTimeFactor. The result was that
        /// damage-per-stamina reduced to 60*baseline/(TARGET*classMult*useAnimation): it depended ONLY on swing
        /// speed, so two weapons with identical DPS were priced completely differently. Measured on the real
        /// roster that made a fast weapon ~2.25x more stamina-efficient than a slow one of comparable DPS —
        /// the mirror image of the old swing-speed-only curve this system replaced, and worse.
        ///
        /// Comparing per-second makes the two speed factors cancel exactly: cost/swing reduces to
        /// TARGET * damagePerUse / baselineDps * classMult, so damage-per-stamina is CONSTANT across the roster
        /// regardless of swing speed, while cost-per-second correctly tracks how much DPS a weapon produces.
        /// A weapon sitting exactly at the player's average DPS still pays TARGET per second at any speed, so
        /// OutputStaminaPerSecond keeps its meaning and needs no recalibration.
        /// </summary>
        private float CalculateOutputBasedWeaponCost(Item item, int scaledUseAnimation, float weaponOutput)
        {
            float weaponDps = ToDps(weaponOutput, scaledUseAnimation);
            float baselineDps = playerDamagePerSecondEma > 0f
                ? playerDamagePerSecondEma
                : ToDps(GetBootstrapDamage(item), scaledUseAnimation);
            baselineDps = Math.Max(1f, baselineDps);

            float relativePower = MathHelper.Clamp(weaponDps / baselineDps, MinimumRelativePower, MaximumRelativePower);
            float archetypeMultiplier = IsWhip(item)
                ? WhipStaminaMultiplier
                : (item.DamageType == DamageClass.Summon ? SummonStaffStaminaMultiplier : 1f);
            float useTimeFactor = Math.Max(1, scaledUseAnimation) / 60f;

            return OutputStaminaPerSecond * relativePower * archetypeMultiplier
                * Player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult * useTimeFactor;
        }

        private static float GetBootstrapDamage(Item item) => Math.Max(1, item.damage);

        private static bool IsWhip(Item item) => item.shoot > ProjectileID.None
            && item.shoot < ProjectileID.Sets.IsAWhip.Length
            && ProjectileID.Sets.IsAWhip[item.shoot];

        /*
		In order to make the Example Resource example straightforward, several things have been left out that would be needed for a fully functional resource similar to mana and health. 
		Here are additional things you might need to implement if you intend to make a custom resource:
		- Multiplayer Syncing: The current example doesn't require MP code, but pretty much any additional functionality will require this. ModPlayer.SendClientChanges and clientClone will be necessary, as well as SyncPlayer if you allow the user to increase exampleResourceMax.
		- Save/Load and increased max resource: You'll need to implement Save/Load to remember increases to your exampleResourceMax cap.
		- Resouce replenishment item: Use GlobalNPC.NPCLoot to drop the item. ModItem.OnPickup and ModItem.ItemSpace will allow it to behave like Mana Star or Heart. Use code similar to Player.HealEffect to spawn (and sync) a colored number suitable to your resource.
		*/

        public override void SaveData(TagCompound tag)
        {
            tag.Add("staminaResourceMax", staminaResourceMax);

            // Persist the measured-output learning. Without this, every world load wiped both EMAs: the player
            // baseline reseeded from the item.damage of whatever weapon you happened to swing first, and since
            // per-weapon EMAs converge ~10x faster than the baseline (0.2 vs 0.02), multi-hit weapons spiked
            // toward the relativePower clamp for the first few minutes of every session before settling back.
            // That made stamina costs both inconsistent between sessions and dependent on swing order.
            tag["playerDamagePerSecondEma"] = playerDamagePerSecondEma;

            List<string> keys = new();
            List<float> values = new();
            foreach (KeyValuePair<int, WeaponOutputState> pair in weaponOutputStates)
            {
                if (pair.Value.DamagePerUseEma <= 0f)
                {
                    continue;
                }
                string key = PersistentItemKey(pair.Key);
                if (key == null)
                {
                    continue;
                }
                keys.Add(key);
                values.Add(pair.Value.DamagePerUseEma);
            }
            tag["weaponOutputKeys"] = keys;
            tag["weaponOutputEmas"] = values;
        }

        public override void LoadData(TagCompound tag)
        {

            staminaResourceMax = tag.GetFloat("staminaResourceMax");

            playerDamagePerSecondEma = tag.GetFloat("playerDamagePerSecondEma");

            weaponOutputStates.Clear();
            if (tag.ContainsKey("weaponOutputKeys") && tag.ContainsKey("weaponOutputEmas"))
            {
                List<string> keys = tag.Get<List<string>>("weaponOutputKeys");
                List<float> values = tag.Get<List<float>>("weaponOutputEmas");
                int count = Math.Min(keys?.Count ?? 0, values?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    int type = ResolvePersistentItemKey(keys[i]);
                    if (type > 0 && values[i] > 0f)
                    {
                        weaponOutputStates[type] = new WeaponOutputState { DamagePerUseEma = values[i] };
                    }
                }
            }
        }

        /// <summary>Save-safe identity for an item type. Modded item ids are assigned at load and shift when
        /// content is added or removed, so a raw int would silently reattribute a weapon's learned output to a
        /// different weapon after any content change. Vanilla ids are stable and stay numeric.</summary>
        private static string PersistentItemKey(int type)
        {
            if (type <= 0)
            {
                return null;
            }
            if (type < ItemID.Count)
            {
                return "v:" + type;
            }
            ModItem modItem = ModContent.GetModItem(type);
            return modItem == null ? null : "m:" + modItem.FullName;
        }

        /// <summary>Inverse of PersistentItemKey. Returns 0 for anything that no longer resolves (content
        /// removed, mod disabled) so the entry is simply dropped and that weapon relearns on next use.</summary>
        private static int ResolvePersistentItemKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 3)
            {
                return 0;
            }
            string body = key.Substring(2);
            if (key[0] == 'v')
            {
                return int.TryParse(body, out int vanillaType) && vanillaType < ItemID.Count ? vanillaType : 0;
            }
            return ModContent.TryFind(body, out ModItem modItem) ? modItem.Type : 0;
        }

        public override void Initialize()
        {
            staminaResourceMax = DefaultStaminaResourceMax;
            staminaResourceCurrent = staminaResourceMax;
            staminaDebt = 0f;
            weaponOutputStates.Clear();
            playerDamagePerSecondEma = 0f;
        }

        public override void OnRespawn()
        {
            staminaResourceCurrent = staminaResourceMax; //
            staminaDebt = 0f; // dying clears the slate — respawning already full but still in debt would be absurd
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            // Current stamina is normally converted into the positive debt field at the end of UpdateResource.
            // Check both representations so a hit on the exact overdraw frame cannot slip through the seam.
            if (info.Damage > 0 && (staminaResourceCurrent < 0f || staminaDebt > 0f))
            {
                Buffs.Debuffs.Stagger.Apply(Player);
            }
        }

        public override void ResetEffects()
        {
            ResetVariables();
        }

        public override void UpdateDead()
        {
            ResetVariables();
        }

        private void ResetVariables()
        {
            tsorcRevampPlayer soulsPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            bool experimentalStaminaValues = ModContent.GetInstance<tsorcRevampConfig>().NewSoulsModeStaminaSystem;

            // Regen rates pair with the cost multipliers in tsorcRevampPlayer.WeaponStaminaMult — change the two
            // together or the class balance drifts. Effective regen is ~15/sec per 1.0 here (the gain applies
            // every 4th tick), before gear gainMult and the stationary bonus.
            if (soulsPlayer.BearerOfTheCurse)
            {
                // Base 2.0 against its 1.15 cost. Experimental 2.8 against 1.5 cost — a much bigger swing on both
                // axes, leaning hard into "spends fast, recovers fast", but 2.8 specifically because that is the
                // net-parity point with Unkindled's experimental 1.0/1.5 (both land at -16.5/sec while attacking).
                // Solve for it with regenRate = (39 * costMult - 16.5) / 15 if either cost changes.
                staminaResourceRegenRate = experimentalStaminaValues ? 2.8f : 2f;
            }
            else if (soulsPlayer.Unkindled)
            {
                // 1.25 base gives Unkindled a real regen edge of its own rather than leaving it on the Classic
                // baseline with nothing but smaller penalties.
                staminaResourceRegenRate = experimentalStaminaValues ? 1.5f : 1.25f;
            }
            else
            {
                staminaResourceRegenRate = 1f;
            }

            staminaResourceGainMult = 1f;

            staminaResourceMax2 = staminaResourceMax;
        }

        public override void PostUpdateMiscEffects()
        {
            UpdateResource();
        }

        //const float BoomerangDrainPerFrame = 0.6f;
        const float HeldProjectileDrainPerFrame = 1f;
        /// <summary>
        /// Ticks regeneration stays paused after any stamina expenditure (Souls classes only).
        ///
        /// Bearer of the Curse gets half the pause. The delay is a flat cost, so it eats a larger share
        /// of the recovery for whichever class refills fastest — a shared 30 ticks quietly ate most of
        /// BotC's 2x regen advantage. Halving it for BotC keeps its burst identity intact.
        /// </summary>
        /// <summary>
        /// 60 / 40, up from 30 / 15 — half a second was barely perceptible and did almost nothing to price the
        /// swing-pause-swing rhythm it exists to tax.
        ///
        /// The 2:3 split is what keeps the classes level, and two independent measures agree on it:
        ///   • Equal stamina forgone (delay x regen): 60 x (18.75 / 30) = 37.5 ticks.
        ///   • Equal tempo advantage — the felt quantity is time until you can act again, `delay + cost/regen`.
        ///     Regen rates alone give BotC a 1.6x edge; at 60/40 it keeps 1.56x, at 60/45 1.49x, but at an equal
        ///     60/60 it collapses to 1.30x. A flat delay quietly erodes the very thing that makes BotC distinct,
        ///     because dead time is worth more to whoever recovers fastest.
        /// </summary>
        internal const int StaminaRegenDelayTicks = 60;
        internal const int StaminaRegenDelayTicksBotC = 40;

        /// <summary>
        /// Regen pause applied when a hit is absorbed by a raised shield — 1.5x the ordinary spend delay.
        ///
        /// Blocking a hit is a heavier commitment than a swing: the shield-down window between an enemy's
        /// spaced shots was long enough to refill everything the block cost, which is what made the
        /// block-shoot loop free. A longer pause makes a sustained block loop bleed the pool instead.
        /// A perfect parry deliberately keeps the ordinary delay (see FreeDodge) — that's the timing reward.
        ///
        /// THESE MUST STAY ABOVE StaminaRegenDelayTicks. They are separate constants rather than a multiple of
        /// the base, so raising the base silently inverts the relationship: at base 60/40 the old 60/30 values
        /// left BotC's *block* penalty (30) SHORTER than an ordinary swing (40), and since PauseStaminaRegen
        /// takes the max, the block penalty would have disappeared entirely.
        ///
        /// 1.5x rather than the original 2x because the base doubled. Straight doubling would put an ordinary
        /// block at 2 full seconds, and a greatshield (up to 1.75x on top) at 3.5 — long enough that a single
        /// block, with no regen while the guard is up, would decide a fight on its own.
        /// </summary>
        internal const int BlockStaminaRegenDelayTicks = 90;
        internal const int BlockStaminaRegenDelayTicksBotC = 60;

        internal int staminaRegenDelayTimer;
        private float _staminaLastFrame;

        /// <summary>Ordinary post-spend regen pause for this player's class.</summary>
        internal int SpendRegenDelay => Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse
            ? StaminaRegenDelayTicksBotC
            : StaminaRegenDelayTicks;

        /// <summary>Post-block regen pause for this player's class (double the ordinary one).</summary>
        internal int BlockRegenDelay => Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse
            ? BlockStaminaRegenDelayTicksBotC
            : BlockStaminaRegenDelayTicks;

        /// <summary>
        /// Pause stamina regeneration for at least <paramref name="ticks"/> frames.
        ///
        /// Always extends, never shortens: a block sets the long pause, and the generic "you spent stamina"
        /// detector in UpdateResource fires on the same spend with the short one. Whichever runs second must
        /// not undo the other, and Player.Hurt (where blocks resolve) is not ordered against
        /// PostUpdateMiscEffects (where UpdateResource runs), so taking the max is the only order-safe rule.
        /// </summary>
        /// <summary>
        /// Stamina owed from overdrawing. Kept as a SEPARATE positive value rather than letting
        /// staminaResourceCurrent go negative, because a negative current would have to be special-cased at
        /// every one of the many places that compare against it (and would break the bar's current/max fill
        /// maths). By construction debt > 0 only ever coincides with current == 0, so the existing
        /// "current > 0" checks already gate actions correctly and the UI needs no changes — which also
        /// matches the Souls games, where the bar simply reads empty and never shows a negative.
        /// </summary>
        public float staminaDebt;

        /// <summary>Hard ceiling on debt, expressed in SECONDS of recovery rather than as a flat number or a
        /// share of the pool. Bounding the felt punishment directly means it stays ~2 seconds whatever your
        /// pool size, class or regen gear — a big pool or heavy regen investment can't turn one overdrawn
        /// swing into a five-second lockout.</summary>
        private const float MaxStaminaDebtSeconds = 2f;

        /// <summary>Regen lands every 4th tick, so 15 applications per second. Uses the pre-modifier rate so
        /// the ceiling doesn't lurch about with the stationary bonus or a raised shield.</summary>
        private float MaxStaminaDebt =>
            MaxStaminaDebtSeconds * 15f * Math.Max(0.05f, staminaResourceGainMult * staminaResourceRegenRate);

        /// <summary>Regeneration pays off debt before it refills the bar, carrying any remainder over so a
        /// tick is never wasted at the moment the debt clears.</summary>
        private void ApplyRegenTick(float gain)
        {
            if (gain <= 0f)
            {
                return;
            }
            if (staminaDebt > 0f)
            {
                staminaDebt -= gain;
                if (staminaDebt < 0f)
                {
                    staminaResourceCurrent += -staminaDebt;
                    staminaDebt = 0f;
                }
                return;
            }
            staminaResourceCurrent += gain;
        }

        internal void PauseStaminaRegen(int ticks)
        {
            // Chloranthy shortens the pause itself. Applied here rather than at the call sites so it covers
            // every source uniformly — weapon spends, blocks, and the greatshield BlockRegenDelayMult alike.
            float reduction = ChloranthyRegenDelayReduction;
            if (reduction > 0f)
            {
                ticks = (int)Math.Round(ticks * (1f - reduction));
            }
            if (ticks > staminaRegenDelayTimer)
            {
                staminaRegenDelayTimer = ticks;
            }
        }

        /// <summary>Fraction cut from the post-spend regen delay by an equipped Chloranthy Ring.
        /// The rings are mutually exclusive, so II replaces I rather than stacking with it.</summary>
        private float ChloranthyRegenDelayReduction
        {
            get
            {
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                if (modPlayer.ChloranthyRing2)
                {
                    return Items.Accessories.Mobility.ChloranthyRing2.RegenDelayReduction / 100f;
                }
                if (modPlayer.ChloranthyRing1)
                {
                    return Items.Accessories.Mobility.ChloranthyRing.RegenDelayReduction / 100f;
                }
                return 0f;
            }
        }

        /// <summary>Fraction of normal regen kept while a shield is raised. Starts at the shared shield baseline;
        /// Chloranthy adds to that baseline, with Ring II replacing Ring I rather than stacking.</summary>
        private float BlockRegenFraction
        {
            get
            {
                float fraction = tsorcRevampActiveShieldPlayer.BlockStaminaRegenMult;
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                if (modPlayer.ChloranthyRing2)
                {
                    fraction += Items.Accessories.Mobility.ChloranthyRing2.BlockStaminaRegenBonus / 100f;
                }
                else if (modPlayer.ChloranthyRing1)
                {
                    fraction += Items.Accessories.Mobility.ChloranthyRing.BlockStaminaRegenBonus / 100f;
                }
                return Math.Min(1f, fraction);
            }
        }

        const float SpecialHeldProjectileDrainPerFrame = 0.6f;
        const float FlailDrainPerFrame = 0.2f;
        const float YoyoDrainPerFrame = 0.3f;
        const float ChargedWhipDrainPerFrame = 0.2f;

        // Lets do all our logic for the custom resource here, such as limiting it, increasing it and so on.
        private void UpdateResource()
        {
            var staminaPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
            var arcanePlayer = Player.GetModPlayer<ArcaneSorceryPlayer>();
            //Main.NewText("Stamina: " + staminaResourceCurrent + "/" + staminaResourceMax2);
            //Main.NewText("Stamina regen rate: " + staminaResourceRegenRate);
            //Main.NewText("Stamina regen gain mult: " + staminaResourceGainMult);
            //Main.NewText("Timer: " + staminaResourceRegenTimer);

            for (int p = 0; p < 1000; p++) //To-do add a check before this making this loop only run if there are actually any projectiles in the array
            {
                var proj = Main.projectile[p];
                if (Player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina)
                {
                    // Apply the active Souls Mode class cost (legacy or experimental config) to held-weapon drains.
                    float mult = Player.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult;
                    //if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == ProjAIStyleID.Boomerang) //find boomerangs, if so, cut regen by 2/3
                    //{
                    //    Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= .333333f;
                    //    break; //break to prevent it nuking the regen rate when multiple boomerangs are present
                    //}

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.ChainGuillotine
                        || Main.projectile[p].type == ProjectileID.MechanicalPiranha))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= SpecialHeldProjectileDrainPerFrame * mult;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                        break;
                    }

                    //why did shortswords have a special per-frame drain in addition to their normal drain?

                    /*if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == ProjAIStyleID.Boomerang) //Can't have boomerangs just not use any stamina, especially weapons like Bananarangs and Possessed Hatchet(their individual throws just do not use stamina currently)
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0.34f;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= BoomerangDrainPerFrame;
                        break;
                    }*/
                    if (Player.HasBuff(ModContent.BuffType<ArcaneSorcery>()) 
                        && staminaPlayer.staminaResourceCurrent < staminaPlayer.staminaResourceMax2 * arcanePlayer.ManaBurnStaminaThreshold / 100f)
                    {
                        Player.AddBuff(ModContent.BuffType<ManaBurn>(), ManaBurn.Duration * 60);
                    }

                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.VortexBeater
                        || Main.projectile[p].type == ProjectileID.Celeb2Weapon || Main.projectile[p].type == ProjectileID.FlyingKnife))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= HeldProjectileDrainPerFrame * mult;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }
                    if (!Player.HasBuff(ModContent.BuffType<ManaBurn>())
                        && Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].type == ProjectileID.Terragrim || Main.projectile[p].type == ProjectileID.Arkhalis
                            || Main.projectile[p].type == ProjectileID.ChargedBlasterCannon || Main.projectile[p].type == ProjectileID.MedusaHead
                            || Main.projectile[p].type == ProjectileID.ChainKnife || Main.projectile[p].type == ProjectileID.LastPrism
                            || Main.projectile[p].type == ProjectileID.LaserMachinegun || proj.type == ProjectileID.ChargedBlasterCannon
                            || Main.projectile[p].type == ProjectileID.DD2PhoenixBow || Main.projectile[p].type == ProjectileID.DD2PhoenixBowShot
                            || Main.projectile[p].type == ProjectileID.Phantasm))
                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= HeldProjectileDrainPerFrame * mult;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (Main.projectile[p].active && Main.projectile[p].type > 0 && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].aiStyle == ProjAIStyleID.Flail
                        || Main.projectile[p].type == ProjectileID.Anchor || Main.projectile[p].GetGlobalProjectile<tsorcGlobalProjectile>().ModdedFlail))

                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= FlailDrainPerFrame * mult;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }


                    if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && (Main.projectile[p].aiStyle == ProjAIStyleID.Yoyo))

                    {
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= YoyoDrainPerFrame * mult;
                        Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    if (Main.projectile[p].active && Main.projectile[p].type > 0 && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].GetGlobalProjectile<tsorcGlobalProjectile>().ChargedWhip && !Player.GetModPlayer<tsorcRevampPlayer>().FinishedChargingWhip)
                    {
                        staminaResourceCurrent -= ChargedWhipDrainPerFrame * mult;
                        staminaResourceRegenRate *= 0f;
                        if (staminaResourceCurrent < 1)
                        {
                            Main.projectile[p].Kill();
                        }
                    }
                }
            }

            //Stamina capping for summoners - First minion costs 18, second one 16, third one 14, etc. Once the cost hits 2, at 9 minions, it keeps costing 2 for subsequent minions
            //this doesn't exist anymore
            if (Player.numMinions > 0)
            {
                minionStaminaCap = (int)(staminaResourceMax2 * 1f);
            }
            else { minionStaminaCap = (int)staminaResourceMax2; }


            staminaResourceGain = staminaResourceGainMult * staminaResourceRegenRate; //Apply our multiplier to our base regen rate
            if (Player.velocity == Vector2.Zero)
            {
                staminaResourceGain *= 1.5f;
            }

            // A raised shield retains only a fraction of normal stamina recovery. The existing post-block delay
            // still has to expire first; this multiplier changes recovery rate, not when recovery begins.
            // Applied here, at the point regen is computed, because setting staminaResourceGainMult from the shield
            // ModPlayer's hooks raced this calc and never landed. Reads isBlocking, which is set earlier in the frame.
            if (Player.GetModPlayer<tsorcRevampActiveShieldPlayer>().isBlocking)
            {
                staminaResourceGain *= BlockRegenFraction;
            }

            // Dark-Souls-style regen delay: spending stamina pauses regeneration for half a second.
            //
            // This is what makes the "you may swing with any stamina at all" rule cost something. Without
            // it the old ~4-tick gap meant you could overdraw to zero, wait a few ticks for a sliver of
            // regen, and swing again — sustained DPS would be completely unaffected by stamina and the
            // only real loss would be dodge access.
            //
            // Compared against the previous frame here, after the per-frame drains above have already
            // been applied, so held-projectile and flail drains refresh the delay too.
            if (Player.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina
                && staminaResourceCurrent < _staminaLastFrame)
            {
                // Extends only — a block that already set the longer pause this frame must survive this.
                PauseStaminaRegen(SpendRegenDelay);
            }
            if (staminaRegenDelayTimer > 0)
            {
                staminaRegenDelayTimer--;
            }

            // For our resource lets make it regen slowly over time to keep it simple, let's use exampleResourceRegenTimer to count up to whatever value we want, then increase currentResource.
            staminaResourceRegenTimer++; //Increase it by 60 per second, or 1 per tick.

            // A simple timer that goes up to 3, increases the exampleResourceCurrent by 1 and then resets back to 0.
            if (Player.whoAmI == Main.myPlayer && staminaRegenDelayTimer <= 0)
            {
                //no stamina regen during a roll/swordspin/using an item, for balance? Yes
                if (!Player.GetModPlayer<tsorcRevampPlayer>().isDodging && !Player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping)
                {
                    if (!Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        // Unkindled no longer regenerates while swinging either, matching Bearer of the
                        // Curse. Classic keeps regenerating unconditionally — it pays no weapon stamina,
                        // so gating it would only punish dodge rolls.
                        bool blockedByAttacking = Player.GetModPlayer<tsorcRevampPlayer>().Unkindled
                            && Player.itemAnimation != 0;

                        if (!blockedByAttacking && staminaResourceRegenTimer > 3)
                        {
                            ApplyRegenTick(staminaResourceGain);
                            staminaResourceRegenTimer = 0;
                        }
                    }
                    else if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Player.itemAnimation == 0 && staminaResourceCurrent <= minionStaminaCap) //Bearer of the Curse doesn't regen stamina while using items
                    {
                        if (staminaResourceRegenTimer > 3)
                        {
                            ApplyRegenTick(staminaResourceGain);

                            if (staminaResourceCurrent > minionStaminaCap) { staminaResourceCurrent = minionStaminaCap; }

                            staminaResourceRegenTimer = 0;
                        }
                    }
                }
            }

            // Overdraw becomes DEBT rather than being forgiven. Previously an overdraw was simply clamped away,
            // which meant overshooting by 1 and overshooting by 130 cost exactly the same — so a heavy weapon was
            // cheapest when you were nearly empty, and the optimal play was to swing your biggest weapon at 1
            // stamina. Now the shortfall is banked and has to be repaid before you can act again, so an action
            // costs the same whenever you take it.
            if (staminaResourceCurrent < 0f)
            {
                staminaDebt += -staminaResourceCurrent;
                staminaResourceCurrent = 0f;
            }
            staminaDebt = MathHelper.Clamp(staminaDebt, 0f, MaxStaminaDebt);

            // Limit exampleResourceCurrent from going over the limit imposed by exampleResourceMax.
            staminaResourceCurrent = Utils.Clamp(staminaResourceCurrent, 0, staminaResourceMax2);

            // Baseline for next frame's "did we spend?" check. Taken after the clamp so an overdraw that
            // was clamped up to 0 doesn't read as a spend again on the following frame.
            _staminaLastFrame = staminaResourceCurrent;
        }

        static readonly List<int> HeldProjectileWeapons = new()
        {
            ItemID.Terragrim,
            ItemID.Arkhalis,
            ItemID.ChargedBlasterCannon,
            ItemID.MedusaHead,
            ItemID.ChainKnife,
            ItemID.LastPrism,
            ItemID.LaserMachinegun,
            ItemID.DD2PhoenixBow,
            ItemID.Phantasm,
            ItemID.VortexBeater,
            ItemID.Celeb2,
            ItemID.FlyingKnife
        };

        static readonly List<int> SpecialHeldProjectileWeapons = new()
        {
            ItemID.ChainGuillotines,
            ItemID.PiranhaGun
        };
        private class tsrStaminaGlobalItem : GlobalItem
        {
            public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
            {
                if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().UsesWeaponStamina) return;
                float staminaMult = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().WeaponStaminaMult;
                if (!ModContent.GetInstance<tsorcRevampConfig>().ShowStaminaTooltip) return;
                if (item.DamageType == DamageClass.Summon && !UsesOutputBasedWeaponCost(item)) return;
                if (item.damage <= 0 && item.type != ItemID.CoinGun) return;
                if (item.ammo != AmmoID.None) return; //ammo does not consume stamina
                if (item.type == ItemID.EoCShield) return;
                StringBuilder tipToAdd = new();
                // Checked against WeaponClassifiedTools directly, NOT UsesOutputBasedWeaponCost — that
                // function now only distinguishes CoinGun from everything else, so it can't be used to
                // tell a real weapon-classified axe (tsorcRevamp.WeaponClassifiedTools) apart from a
                // genuine utility tool any more. A weapon-classified axe should show the plain "Stamina
                // Use: " legacy number here, same as any sword — not the tool label/surcharge.
                bool isToolWeapon = (item.pick != 0 || item.axe != 0 || item.hammer != 0)
                    && (tsorcRevamp.WeaponClassifiedTools == null || !tsorcRevamp.WeaponClassifiedTools.Contains(item.type));
                if (isToolWeapon)
                {
                    // No longer "on-hit only" — pick/axe/hammer now pay this per swing, same as every
                    // other weapon, plus a small extra shown separately for landing on an NPC.
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaUseToolSwing"));
                }
                else
                {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaUse"));
                }

                int preModificationLength = tipToAdd.Length;

                #region drain per frame AIStyle-based cases
                Projectile shot = new();
                shot.SetDefaults(item.shoot);

                float drainPerFrame = 0f;
                bool preventsRegen = false;
                bool inhibitsRegen = false;
                switch (shot.aiStyle)
                {
                    /*case ProjAIStyleID.Boomerang:
                        {
                            drainPerFrame = BoomerangDrainPerFrame;
                            inhibitsRegen = true;
                            break;
                        }*/
                    case ProjAIStyleID.Flail:
                        {
                            drainPerFrame = FlailDrainPerFrame;
                            preventsRegen = true;
                            break;
                        }
                    case ProjAIStyleID.Yoyo:
                        {
                            drainPerFrame = YoyoDrainPerFrame;
                            preventsRegen = true;
                            break;
                        }
                    default: break;

                }
                if (item.shoot != 0)
                {
                    if (shot.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedFlail)
                    {
                        drainPerFrame = FlailDrainPerFrame;
                        preventsRegen = true;
                    }
                    if (shot.GetGlobalProjectile<tsorcGlobalProjectile>().ChargedWhip)
                    {
                        drainPerFrame = ChargedWhipDrainPerFrame;
                        preventsRegen = true;
                    }
                }
                #endregion

                #region unique cases
                //it's just harpoon. seriously, what IS this weapon? i dont understand.
                if (item.type == ItemID.Harpoon)
                {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.14"));
                }
                #endregion

                if (tipToAdd.Length == preModificationLength)
                {
                    int scaledUseAnimation = (int)(item.useAnimation / Main.LocalPlayer.GetAttackSpeed(item.DamageType));
                    bool outputBased = UsesOutputBasedWeaponCost(item);
                    int staminaUse = outputBased
                        ? (int)Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>()
                            .GetExpectedOutputBasedWeaponCost(item, scaledUseAnimation)
                        : (int)(LegacyWeaponStaminaCost(scaledUseAnimation) * staminaMult * (isToolWeapon ? ToolSwingStaminaMult : 1f));
                    tipToAdd.Append($"{staminaUse}");

                    if (isToolWeapon)
                    {
                        int surcharge = (int)(ToolCombatHitSurcharge * staminaMult);
                        tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaUseToolHitSurcharge", surcharge));
                    }

                    // DebugMode side-by-side. Comparing the old swing-speed-only cost against the live
                    // output-based one is a DISPLAY concern, not a gameplay one — doing it this way means
                    // there's no gameplay toggle that can ship in the wrong state, and no second cost path
                    // to keep working in combat code.
                    if (outputBased && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                    {
                        tipToAdd.Append($" (old: {(int)(LegacyWeaponStaminaCost(scaledUseAnimation) * staminaMult)})");
                    }
                }

                #region special drain per frame
                if (HeldProjectileWeapons.Contains(item.type))
                {
                    drainPerFrame = HeldProjectileDrainPerFrame;
                    preventsRegen = true;
                }

                else if (SpecialHeldProjectileWeapons.Contains(item.type))
                {
                    drainPerFrame = SpecialHeldProjectileDrainPerFrame;
                    //these dont prevent regen
                }
                #endregion

                #region drain per frame tooltips
                if (drainPerFrame != 0f)
                {
                    tipToAdd.Append($" + {drainPerFrame * 60 * staminaMult} " + LangUtils.GetTextValue("UI.PerSecond"));
                }

                if (inhibitsRegen)
                {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaRegenReduction"));
                }
                if (preventsRegen)
                {
                    tipToAdd.Append(LangUtils.GetTextValue("UI.StaminaRegenNullification"));
                }
                #endregion
                int ttindex = tooltips.FindLastIndex(t => t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {// if we find one
                 //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "",
                    tipToAdd.ToString()));
                }
            }
        }
    }
}
