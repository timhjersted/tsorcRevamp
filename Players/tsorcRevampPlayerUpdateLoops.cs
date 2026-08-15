using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using TerraUI.Objects;
using tsorcRevamp;
using tsorcRevamp.Achievements;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Accessories;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;
using tsorcRevamp.NPCs;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;
using tsorcRevamp.Projectiles.Summon.YoungHunter;
using tsorcRevamp.Projectiles.Pets;
using tsorcRevamp.Systems;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    //update loops that run every frame
    public partial class tsorcRevampPlayer
    {
        public Vector2 CursorPosition = new Vector2(0, 0); //just so it doesn't null

        public const float MeleeBonusMultiplier = 0.5f;

        public bool NoDamageSpread = false;

        public Vector2 greatMirrorWarpPoint;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public int RuneMageChatTimer = 0;

        public bool BeastMode1 = false;
        public bool SilverSerpentRing = false;
        public bool SoulSerpentRing = false;
        public bool DragonStoneImmunity = false;
        public static bool DragonStonePotency = false;
        public int SoulReaper = 5;
        public bool TornWings = false;
        public bool Crippled = false;
        public bool WitchkingsGrasp = false;
        public int PullStrength = 0;
        public int PullFrame = 0;

        // Set (and continually refreshed) by whatever is impaling the player - e.g. Artorias's
        // StabbingPiercingDash - to freeze movement/attacks and anchor the player to the impaling
        // weapon's tip. Decremented in PreUpdateMovement, so it self-releases a few ticks after
        // the attacker stops refreshing it (safety net if the attack ends abnormally).
        public int ImpaleFreezeTimer = 0;
        public Vector2 ImpaleWorldPosition;


        public bool Celestriad = false;
        public bool UndeadTalisman = false;
        public bool WolfRing = false;
        public bool BarrierRing;
        public bool Trinity = false;

        public bool DragoonBoots = false;
        public bool DragoonBootsEnable = false;
        public bool DragoonHorn = false;

        public bool ConditionOverload = true;

        public bool HerculesBeetle = false;
        public bool NecromanticScroll = false;
        public bool PapyrusScarab = false;

        public float WhipTipHitBonusDamage = 35f;
        public bool FinishedChargingWhip = false;

        public int CurseLevel = 1;
        public int PowerfulCurseLevel = 1;
        public int curseDecayTimer = 0;
        public int powerfulCurseDecayTimer = 0;

        public int DestinedDeathStacks = 0;
        public float DestinedDeathCurrentHPLoss = 0f;
        public bool DestinedDeathActive = false;

        public bool BrokenSpirit;

        public bool HasSporePowder;
        public bool HasVenomPowder;
        public bool HasYoungHunterAccessory;
        public bool HasLoveRing;
        private int loveHealCooldown = 0;

        public int MaxMinionTurretMultiplier;

        public float LethalTempoMeleeBaseAttackSpeedMult = 0.83f;
        public int LethalTempoDuration = 3;
        public float LethalTempoStacks = 0;
        public int LethalTempoMaxStacks = 6;
        public float LethalTempoBonusAttackSpeedPerStack = 0.07f;

        public float ConquerorSummonBaseDamageMult = 0.4f;
        public int ConquerorDuration = 3;
        public float ConquerorStacks = 0;
        public int ConquerorMaxStacks = 10;
        public float ConquerorBonusDmgPerStack = 0.06f;
        public float FullConquerorBonusTagDuration = 0.1f;

        public bool SteraksGage = false;
        public bool InfinityEdge = false;
        public bool LudensTempest = false;
        public bool Goredrinker = false;
        public bool GoredrinkerReady = false;
        public bool GoredrinkerSwung = false;
        public int GoredrinkerHits = 0;

        public int WorldEnderSwing = 1;

        public bool MaskOfTheFather = false;
        public bool BoneRing;
        public bool CelestialCloak;

        public bool CanUseItemsWhileDodging;
        public bool ArtoriasAbysswalker;
        public float ArtoriasAbysswalkerDodgeStaminaCostReduction;
        public int ArtoriasAbysswalkerCounterType;
        public bool Witch;

        public bool Kraken;
        public bool NecromanticSerpent = false;

        public int SeveringDuskDashTime = 0;

        public int SoulVessel = 0;
        public float MaxManaAmplifier;

        public bool ZirconRing = false;

        public int CritColorTier = 0;

        public int MagicPlatingStacks = 0;

        public bool ChloranthyRing1 = false;
        public bool ChloranthyRing2 = false;

        public bool DarkInferno = false;
        public bool AbyssInferno = false;
        public bool MorgulPoisoning = false;
        public bool CrimsonDrain = false;
        public bool PhazonCorruption = false;
        public int count = 0;

        public bool Shockwave = false;
        public bool Falling;
        public int StopFalling;
        public float FallDist;
        public float fallStartY;
        public int fallStart_old = -1;
        private float lastWingFallGravityDirection = 1f;

        public int JaggedFlatCritDmgBonus = 0;
        public int RashBadLifeRegenPerSec = 0;
        public float EncouragingSummonTagDmg = 0f;

        public bool MeleeArmorVamp10 = false;

        public bool MagmaArmor;
        public bool PortlyPlateArmor;

        public bool OldWeapon = false;

        public bool PhoenixSkull = false;
        public bool BossBlockedPhoenixRevive = false;

        public bool MythrilOrichalcumCritDamage = false;
        public bool PilgrimSpontoonBuff = false;
        public bool Shunpo = false;
        public float ShunpoTimer = 0;
        public Vector2 ShunpoVelocity;
        public float WhipCritHitboxSize = 1f;

        public int SteelTempestStacks = 0;
        public int SweepingBladeTimer = 0;
        public Vector2 SweepingBladeVelocity;
        public Vector2 MouseHitboxSize = new Vector2(125, 125);

        public int EssenceThief = 0;
        public int SpiritRushCharges = 3;
        public float SpiritRushTimer = 0f;
        public int SpiritRushSoundStyle = 0;
        public float SpiritRushCooldown = 0f;
        public Vector2 SpiritRushVelocity;

        public int RuneterraMinionHitSoundCooldown = 0;
        public bool InterstellarBoost = false;
        public int InterstellarBoostCooldown = 0;
        public int MinimumMinionCircleRadius = 150;
        public int MaximumMinionCircleRadius = 500;
        public float MinionCircleRadius = 150;
        public int CenterOfTheUniverseStardustCount = 0;

        public float CrystalNunchakuDefenseDamage;

        public float DragoonLashFireBreathTimer = 0f;
        public float SupremeDragoonLashFireBreathTimer = 0f;

        public float SummonTagStrength;
        public float SummonTagDuration;
        public bool CrystallineShard;
        public float CrystallinePower;

        public bool MythrilBulwark = false;
        public bool IceboundMythrilAegis = false;

        public bool WaspPower = false;
        public bool DemonPower = false;
        public bool HollowSoldierAgility = false;
        public bool SmoughShieldSkills = false;
        public bool BurdenOfSmough = false;
        public bool SmoughAttackSpeedReduction = false;

        public bool Miakoda = false;
        public bool RTQ2 = false;

        public bool BoneRevenge = false;
        public bool SoulSiphon = false;
        public float SoulSiphonScaling = 1;
        public int ConsSoulChanceMult;
        public bool SoulSickle = false;

        public bool BurningAura = false;
        public bool BurningStone = false;

        public bool Sharpened = false;
        public bool AmmoBox = false;
        public bool AmmoReservationPotion = false;
        public bool TitanPotion = false;
        public float AmmoReservationDamageScaling = 1f;
        public float TitanSizeScaling = 1f;

        public bool SOADrain = false;
        public bool VOEGDrain = false;

        public int supersonicLevel = 0;

        public int darkSoulQuantity;
        private int lastDarkSoulQuantityForGainText;
        private bool initializedDarkSoulQuantityForGainText;
        public int magicDefense = 0;

        //An int because it'll probably be necessary to split it into multiple levels
        public int manaShield = 0;

        public int staminaShield = 0;
        public bool DragonCrestShieldEquipped = false;

        //How many more frames the Mana Shield is disabled after using a mana potion
        public int manaShieldCooldown = 0;
        //What frame of the shield's animation it's on
        public int shieldFrame = 0;

        public int staminaShieldFrame = 0;
        //Did they have the shield up last frame?
        public bool shieldUp = false;

        public bool staminaShieldUp = false;

        public bool chestBankOpen;
        public int chestBank = -1;

        public bool chestPiggyOpen;
        public int chestPiggy = -1;

        public int FracturingArmor = 1;
        public bool HasFracturingArmor = false;

        public int dragonMorphDamage = 45;

        public int MiakodaEffectsTimer;

        public bool MiakodaFull; //Miakoda - Full Moon Form
        public bool MiakodaFullHeal1;
        public bool MiakodaFullHeal2;

        public bool MiakodaCrescent; //Miakoda - Crescent Moon Form
        public bool MiakodaCrescentBoost;
        public int MiakodaCrescentBoostTimer;
        public bool MiakodaCrescentDust1;
        public bool MiakodaCrescentDust2;

        public bool MiakodaNew; //Miakoda - New Moon Form
        public bool MiakodaNewBoost;
        public int MiakodaNewBoostTimer;
        public bool MiakodaNewDust1;
        public bool MiakodaNewDust2;

        internal bool gotDarksign;
        public bool FirstEncounter;
        public bool ReceivedGift;
        public bool ReceivedHuntingTome;
        public int heraldChatState;
        /// <summary>Times this player has crafted a Bonfire Placeable. Drives its doubling Dark Soul cost -
        /// see Items.Placeable.BonfirePlaceable.GetDarkSoulCost.</summary>
        public int BonfiresCrafted;

        public bool[] PermanentBuffToggles;
        public static Dictionary<int, float> DamageDir;

        public bool GiveBossZen;
        private bool BossZenInternal = false;
        public bool BossZenBuff
        {
            get { return BossZenInternal; }
            // Boss Zen internal is only set to true if the config setting is enabled.
            // This overrides all the instances where the player is directly acessed to apply boss zen
            set { BossZenInternal = ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig && value; }
        }

        public bool MagicWeapon;
        public bool GreatMagicWeapon;
        public bool CrystalMagicWeapon;
        public bool ReboundProjectile;
        public bool ShadowmoonCloak;

        //increased grab range immediately after killing a boss
        public int bossMagnetTimer;
        public bool bossMagnet;

        public bool ShadowWeight;

        public bool ReflectionShiftEnabled; //Does the player have it equipped?
        public int ReflectionShiftKeypressTime = 0; //If they just pressed an arrow key, this is set to 15. It counts down to 0. If another arrow key is pressed when it is not zero, a dash initiates.
        public Vector2 ReflectionShiftState = Vector2.Zero;
        int[] keyPrimed = new int[4] { 0, 0, 0, 0 }; //Holds the state of each key
        public int FastFallTimer = 0;

        public static readonly int DashDown = 0;
        public static readonly int DashUp = 1;
        public static readonly int DashRight = 2;
        public static readonly int DashLeft = 3;

        public bool BearerOfTheCurse;
        public bool Unkindled;
        public int unkindledManaDelayTimer;
        // True for either Unkindled or Bearer of the Curse — gate Souls-system features (Estus/Cerulean UI,
        // bonfire refills, drop bonuses, recipe conditions) on this. BotC-only features keep checking BearerOfTheCurse.
        public bool SoulsMode => Unkindled || BearerOfTheCurse;

        // Weapon stamina usage. Classic pays none either way.
        //
        // BASE (0.85 Unkindled / 1.15 BotC, paired with 1.25 and 2.0 regen in tsorcRevampPlayerStamina):
        // the two classes drain at nearly the same net rate (~-14/sec each while attacking). The old base
        // (0.75 / 1.0 with 1.0 and 2.0 regen) left BotC at only ~-9/sec versus Unkindled's ~-14/sec, so its
        // 2x regen very nearly cancelled its higher cost and the higher-power class was the one that felt
        // the resource LEAST. Converging them means the classes differ in tempo — BotC spends and recovers
        // faster — rather than in how much stamina constrains them.
        //
        // EXPERIMENTAL (config, default off): pushes both costs up and widens the regen gap instead.
        /// <summary>
        /// Convert a "percent of max life per second" drain into Terraria's lifeRegen units.
        ///
        /// lifeRegen is half-HP per second (the engine adds it to lifeRegenCount each frame and grants 1 HP per
        /// 120), hence the x2. Floored at 2 (-1 HP/sec) so a low-life character still feels the curse.
        /// Percent rather than flat because a flat drain shrinks against a growing pool: the old flat 8 cost 80%
        /// of an early 100 HP bar over 20s but only 20% of a late 400 HP one.
        /// </summary>
        private int PercentLifeDrain(float percentPerSecond)
            => Math.Max(2, (int)Math.Round(Player.statLifeMax2 * percentPerSecond * 2f / 100f));

        public bool UsesWeaponStamina => BearerOfTheCurse || Unkindled;
        public float WeaponStaminaMult
        {
            get
            {
                bool experimentalStaminaValues = ModContent.GetInstance<tsorcRevampConfig>().NewSoulsModeStaminaSystem;
                float classStaminaMult = BearerOfTheCurse
                    ? (experimentalStaminaValues ? 1.5f : 1.15f)
                    : (Unkindled ? (experimentalStaminaValues ? 1f : 0.85f) : 0f);

                return classStaminaMult * TiredStaminaMult;
            }
        }

        // Tired debuff: +25% stamina cost on everything that spends it. Applied here so every existing
        // WeaponStaminaMult read picks it up automatically; the dodge roll's flat stamina cost (which doesn't
        // go through WeaponStaminaMult) multiplies by this directly instead - see DodgeRoll().
        public float TiredStaminaMult => Tired ? 1.25f : 1f;

        // Tier-aware healing scalar applied to instant-heal items (food, potions, Tome of Health, etc.).
        // Classic: full heal. Unkindled: half heal. Bearer of the Curse: zero (caller should skip heal entirely).
        // Use this in custom UseItem implementations; for vanilla items the reduction is applied in
        // tsorcGlobalItem.OnConsumeItem via the same multiplier.
        public const float UnkindledHealMultiplier = 0.5f;
        public int ApplyHealing(int baseAmount)
        {
            if (BearerOfTheCurse) return 0;
            if (Unkindled) return (int)(baseAmount * UnkindledHealMultiplier);
            return baseAmount;
        }
        public bool LifegemHealing;
        public bool RadiantLifegemHealing;
        public bool StarlightShardRestoration;
        int healingTimer = 0;
        float restorationTimer = 0;

        public Item[] PotionBagItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
        public int potionBagCountdown = 0; //You can't move items around if an item is still 'in use'. This lets us delay opening the bag until that finishes.

        public UIItemSlot SoulSlot;

        //Active Shields Revamp: the SoulsMode-only "Right-Click" (2nd) slot. Holds a usable item or shield that activates on right click.
        public UIItemSlot RightClickSlot;

        //Dark Souls Storage: a decorative button-slot below the 2nd slot that opens the Storage pop-up. Holds no item.
        public UI.StorageOpenerSlot StorageOpenerSlot;

        public int PiercingGazeCharge;

        public bool PowerWithin;
        public int StaminaReaper = 0;

        public bool AuraOfIlluminance;

        public List<int> ActivePermanentPotions;

        public Vector2[] oldPos = new Vector2[60];

        public bool CowardsAffliction;
        public bool GravityField;
        public float TextCooldown; //Used for if we want text to display when a Thing happens, but not to spam the player
        public float FieldTimer;

        public bool startedQuest;
        public bool finishedQuest;
        public bool touchedSurface;

        public tsorcAuraState CurrentAuraState;

        public float rotation3d;
        public static Vector2 RealMouseWorld;

        public string DeathText;
        public string DeathTextOverride = "";
        public int currentDeathTextIndex;
        bool setDeathText = false;
        public static List<string> DeathTextList;

        public int spawnRate;// For adventure card spwanRate display
        static FieldInfo spawnRateFieldInfo;

        public bool gilled;

        public bool MorgulWhipEffect;
        public bool DragonSoulEffect;
        public bool MidasGreedEffect;
        public bool CurseActive;
        public float CurseMaxLifeMultiplier;
        public float CurseDefenseBonus;
        public float CurseResistanceBonus;
        public float CurseDamageBonus;
        public float CurseAttackSpeedBonus;
        public float CurseMovementSpeedBonus;
        public float CurseLifeRegenerationBonus;

        public bool powerfulCurseActive;
        public float powerfulCurseMaxLifeMultiplier;
        public float powerfulCurseDefenseBonus;
        public float powerfulCurseResistanceBonus;
        public float powerfulCurseDamageBonus;
        public float powerfulCurseAttackSpeedBonus;
        public float powerfulCurseMovementSpeedBonus;
        public float powerfulCurseLifeRegenerationBonus;

        public float CursePositiveStatsMultiplier;

        // Right click structure buffs
        public bool HadBuffBewitched;
        public bool HadBuffClairvoyance;
        public bool HadBuffSharpened;
        public bool HadBuffAmmoBox;
        public bool HadBuffStrategist;

        public bool EnterTheAbyss;
        // Set by the Vessel of Souls while it has swallowed you (hides the whole player). Reset each tick.
        public bool SwallowHidden;
        // Not reset in ResetEffects - persists across ticks so AbyssTransitionEffects() (PostUpdateMiscEffects)
        // can detect the enter/exit edge instead of re-triggering every tick EnterTheAbyss happens to be true.
        public bool WasInAbyss;
        public bool CovenantOfArtoriasEquipped;
        public bool SlowfallWingActive;
        public bool Suppressed;
        public bool Tired;
        public int timeSinceLastAttacked = 0;

        // 3 seconds until sinking
        public const int LavaWalkTimer = 60 * 3;
        public int LavaWalkCount = 0;
        public int TimeAwayFromLava = 60 * 3;
        public bool WaterWalkingBoots;
        
        public override void ResetEffects()
        {
            // Re-evaluate if any boss is alive once per tick
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                tsorcRevampWorld.ResetBossAlive();
            }

            EnterTheAbyss = false;
            SwallowHidden = false;
            CovenantOfArtoriasEquipped = false;
            SlowfallWingActive = false;
            Suppressed = false;
            Tired = false;
            DestinedDeathActive = false;
            if (Player.dead)
            {
                DestinedDeathStacks = 0;
                DestinedDeathCurrentHPLoss = 0f;
            }
            BeastMode1 = false;
            SilverSerpentRing = false;
            SoulSerpentRing = false;
            DragonStoneImmunity = false;
            if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer)
            {
                DragonStonePotency = false;
            }
            SoulReaper = 5;

            DragoonBoots = false;
            //player.eocDash = 0;
            Player.armorEffectDrawShadowEOCShield = false;
            UndeadTalisman = false;

            SteraksGage = false;
            InfinityEdge = false;
            LudensTempest = false;
            Goredrinker = false;

            JaggedFlatCritDmgBonus = 0;
            RashBadLifeRegenPerSec = 0;
            EncouragingSummonTagDmg = 0f;

            MaskOfTheFather = false;
            BoneRing = false;
            CursePositiveStatsMultiplier = 1f;

            ChloranthyRing1 = false;
            ChloranthyRing2 = false;
            WolfRing = false;
            BarrierRing = false;

            HerculesBeetle = false;
            NecromanticScroll = false;
            PapyrusScarab = false;

            BrokenSpirit = false;

            MythrilOrichalcumCritDamage = false;
            PilgrimSpontoonBuff = false;
            Shunpo = false;
            WhipCritHitboxSize = 1;

            PhoenixSkull = false;
            Trinity = false;

            SummonTagStrength = 1f;
            SummonTagDuration = 1f;
            CrystallineShard = false;

            HasSporePowder = false;
            HasVenomPowder = false;
            HasYoungHunterAccessory = false;
            HasLoveRing = false;
            MaxMinionTurretMultiplier = 1;

            MythrilBulwark = false;
            IceboundMythrilAegis = false;

            Celestriad = false;
            MaxManaAmplifier = 0f;
            CelestialCloak = false;

            CanUseItemsWhileDodging = false;
            ArtoriasAbysswalker = false;
            ArtoriasAbysswalkerDodgeStaminaCostReduction = 0f;

            Witch = false;

            DragoonBoots = false;
            OldWeapon = false;
            Miakoda = false;
            MagmaArmor = false;
            PortlyPlateArmor = false;

            WaspPower = false;
            DemonPower = false;

            HollowSoldierAgility = false;
            SmoughShieldSkills = false;
            BurdenOfSmough = false;
            SmoughAttackSpeedReduction = false;

            RTQ2 = false;
            DarkInferno = false;
            AbyssInferno = false;
            MorgulPoisoning = false;
            BoneRevenge = false;
            SoulSiphon = false;
            SoulSiphonScaling = 1f;

            ZirconRing = false;
            NecromanticSerpent = false;

            CrimsonDrain = false;
            Shockwave = false;
            SOADrain = false;
            VOEGDrain = false;
            MiakodaFull = false;

            MiakodaFullHeal1 = false;
            MiakodaCrescent = false;
            MiakodaCrescentDust1 = false;

            MiakodaNew = false;
            MiakodaNewDust1 = false;
            GiveBossZen = false;

            BossZenBuff = false;
            MagicWeapon = false;
            GreatMagicWeapon = false;
            CrystalMagicWeapon = false;
            ReboundProjectile = false;

            ShadowmoonCloak = false;
            manaShield = 0;
            staminaShield = 0;

            ConditionOverload = false;
            supersonicLevel = 0;
            ConsSoulChanceMult = 0;
            SoulSickle = false;
            TornWings = false;
            WitchkingsGrasp = false;
            MorgulWhipEffect = false;
            DragonSoulEffect = false;
            MidasGreedEffect = false;

            Crippled = false;
            ShadowWeight = false;
            ReflectionShiftEnabled = false;

            Sharpened = false;
            AmmoBox = false;
            normansRingAmmoSave = false;
            AmmoReservationPotion = false;
            AmmoReservationDamageScaling = 1f;
            TitanPotion = false;
            TitanSizeScaling = 1f;

            PhazonCorruption = false;
            LifegemHealing = false;
            RadiantLifegemHealing = false;
            StarlightShardRestoration = false;

            PowerWithin = false;
            BurningAura = false;
            BurningStone = false;
            StaminaReaper = 0;

            ActivePermanentPotions = new List<int>();
            CowardsAffliction = false;
            DragonCrestShieldEquipped = false;

            if (!HasFracturingArmor)
            {
                FracturingArmor = 1;
            }
            HasFracturingArmor = false;
            GravityField = false;
            DragoonHorn = false;
            if (!Player.channel) rotation3d = 0;
            MeleeArmorVamp10 = false;
            AuraOfIlluminance = false;
            CurrentAuraState = tsorcAuraState.None;

            gilled = false;
            WaterWalkingBoots = false;

        }
        public override void PreUpdate()
        {
            if (unkindledManaDelayTimer > 0)
            {
                unkindledManaDelayTimer--;
            }

            int playerX = (int)(Main.LocalPlayer.Center.X / 16f);
            int playerY = (int)(Main.LocalPlayer.Center.Y / 16f);

            timeSinceLastAttacked++;
            // Reset the last attacked NPC every 10 seconds
            if (timeSinceLastAttacked > 10 * 60)
            {
                LastAttackedNPCIndex = -1;
                timeSinceLastAttacked = 0;
            }

            RealMouseWorld = Main.MouseWorld;

            // Suppress weapon/item use while the cursor is over a soapstone's interaction zone.
            // tsorcRevampSystems.PostDrawInterface also sets Player.mouseInterface = true on click,
            // but that runs in the DRAW phase, which happens *after* this frame's Update() (and
            // therefore after Player's own item-use check) already ran. That made it a no-op: every
            // click on the "Show" button also fired whatever weapon was equipped, and if it had any
            // forward lunge/knockback the character would visibly drift for a second or two afterward
            // while the message was open — looking exactly like the sticky bubble closing itself with
            // no input. Setting mouseInterface here, in PreUpdate (before item-use runs), actually
            // suppresses it. Uses last frame's NearbySoapstone selection, which is accurate enough
            // since position/mouse barely change frame-to-frame.
            if (Player.whoAmI == Main.myPlayer && tsorcRevamp.NearbySoapstone != null)
            {
                Tiles.SoapstoneTileEntity soapstone = tsorcRevamp.NearbySoapstone;
                Vector2 soapstoneCenter = new(soapstone.Position.X * 16 + 8, soapstone.Position.Y * 16 + 8);
                if (Vector2.Distance(RealMouseWorld, soapstoneCenter) <= 48f)
                {
                    Player.mouseInterface = true;
                }
            }

            tsorcRevampPlayerAuraDrawLayers.HandleAura(this);
            //Fixes bug where switching from a better to a worse pair of wings keeps the previous wing time cap
            if (Player.wingTime > Player.wingTimeMax)
            {
                Player.wingTime = Player.wingTimeMax;
            }

            magicDefense = 0;

            if (loveHealCooldown > 0)
            {
                loveHealCooldown--;
            }
            //No more Distorted debuff
            Player.buffImmune[BuffID.VortexDebuff] = true;

            //Hacky but necessary. Mount items seem to bypass the CanUseItem check for some stupid reason
            if (!NPC.downedQueenBee)
            {
                Player.ClearBuff(BuffID.SlimeMount);
            }
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
            {
                Player.ClearBuff(BuffID.QueenSlimeMount);
            }

            if (tsorcRevampWorld.BossAlive && ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig)
            {
                Player.AddBuff(ModContent.BuffType<Buffs.BossZenBuff>(), 5);
            }
            if (TextCooldown > 0)
            {
                TextCooldown--;
            }

            Point point = Player.Center.ToTileCoordinates();
            Player.ZoneBeach = Player.ZoneOverworldHeight && (point.X < 1000 || point.X > Main.maxTilesX - 8398); //default 380 and 380

            Player.fullRotationOrigin = new Vector2(11, 22);
            SetDirection(true);

            darkSoulQuantity = Player.CountItem(ModContent.ItemType<DarkSoul>(), 999999);

            //the item in the soul slot will only ever be souls, so we dont need to check type
            if (SoulSlot.Item.stack > 0) { darkSoulQuantity += SoulSlot.Item.stack; }

            if (!initializedDarkSoulQuantityForGainText)
            {
                lastDarkSoulQuantityForGainText = darkSoulQuantity;
                initializedDarkSoulQuantityForGainText = true;
            }
            else if (Player.whoAmI == Main.myPlayer && darkSoulQuantity > lastDarkSoulQuantityForGainText)
            {
                CombatText.NewText(Player.getRect(), Color.MediumPurple, $"+{darkSoulQuantity - lastDarkSoulQuantityForGainText}");
            }
            lastDarkSoulQuantityForGainText = darkSoulQuantity;

            if (!Player.HasBuff(ModContent.BuffType<Bonfire>()))
            { //this ensures that BonfireUIState is only visible when within Bonfire range
                if (Player.whoAmI == Main.LocalPlayer.whoAmI)
                {
                    BonfireUIState.Visible = false;
                }
            }

            if (potionBagCountdown > 0)
            {
                potionBagCountdown--;
            }
            if (potionBagCountdown == 1)
            {
                if (Player.whoAmI == Main.myPlayer)
                {
                    if (!PotionBagUIState.Visible)
                    {
                        Player.chest = -1;
                        Main.playerInventory = true;
                        PotionBagUIState.Visible = true;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                    else
                    {
                        PotionBagUIState.Visible = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
                    }
                }
            }


            #region Miakoda

            MiakodaEffectsTimer++;

            if (MiakodaFullHeal1)
            { //dust loop on player the instant they get healed
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 107, 0f, 0f, 30, default, .75f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (MiakodaCrescentDust1)
            { //dust loop on player the instant they get imbue
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 164, 0f, 0f, 30, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 5f);
                    Main.dust[dust].noGravity = false;
                }
            }
            if (MiakodaCrescentBoost)
            {
                MiakodaCrescentBoostTimer++;
            }
            if (MiakodaCrescentBoostTimer > Items.Pets.MiakodaCrescent.BoostDuration * 60)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentBoost = false;
                MiakodaCrescentBoostTimer = 0;
            }

            if (MiakodaNewDust1)
            { //dust loop on player the instant they get boost
                for (int d = 0; d < 100; d++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 57, 0f, 0f, 50, default, 1.2f);
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 7.5f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (MiakodaNewBoost)
            {
                MiakodaNewBoostTimer++;
                Player.armorEffectDrawShadow = true;

            }
            if (MiakodaNewBoostTimer > Items.Pets.MiakodaNew.BoostDuration * 60)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewBoost = false;
                MiakodaNewBoostTimer = 0;
            }

            // Miakoda help checks
            // ALSO for Achievements Explore condition

            if (!tsorcRevampWorld.ExploreVillage && UsefulFunctions.PlayerInZone(Player, 4000, 4400, 620, 745))
            {
                tsorcRevampWorld.ExploreVillage = true;
                ModContent.GetInstance<ExploreVillage>().ExploreVillageCondition.Complete();
            }
            if (!tsorcRevampWorld.EnteredHell && Player.ZoneUnderworldHeight)
            {
                tsorcRevampWorld.EnteredHell = true;
            }

            if (!tsorcRevampWorld.TalkedToAraz && UsefulFunctions.PlayerInZone(Player, 7290, 7340, 590, 610))
            {
                tsorcRevampWorld.TalkedToAraz = true;
            }

            if (!tsorcRevampWorld.EnteredRuinsOfElengad && (UsefulFunctions.PlayerInZone(Player, 1630, 1660, 950, 980) || UsefulFunctions.PlayerInZone(Player, 920, 950, 1030, 1060)))
            {
                tsorcRevampWorld.EnteredRuinsOfElengad = true;
            }

            if (!tsorcRevampWorld.EnteredRuinsOfSerris && UsefulFunctions.PlayerInZone(Player, 1420, 1460, 920, 940))
            {
                tsorcRevampWorld.EnteredRuinsOfSerris = true;
            }

            if (!tsorcRevampWorld.EnteredShadowTemple && UsefulFunctions.PlayerInZone(Player, 1890, 1930, 1080, 1120))
            {
                tsorcRevampWorld.EnteredShadowTemple = true;
            }

            if (!tsorcRevampWorld.EnteredFloodedMachineTemple && UsefulFunctions.PlayerInZone(Player, 4600, 4620, 1080, 1110))
            {
                tsorcRevampWorld.EnteredFloodedMachineTemple = true;
            }

            if (!tsorcRevampWorld.EnteredPyramid && UsefulFunctions.PlayerInZone(Player, 5740, 5760, 1750, 1780))
            {
                tsorcRevampWorld.EnteredPyramid = true;
            }

            #endregion

            #region manashield
            if (manaShield > 0)
            {
                shieldFrame++;
                if (shieldFrame > 23)
                {
                    shieldFrame = 0;
                }

                //Disable Mana Regen Potions
                Player.manaRegenBuff = false;
                Player.buffImmune[BuffID.ManaRegeneration] = true;
            }
            #endregion manashield

            #region Abyss Shader
            if (Player.whoAmI == Main.myPlayer)
            {
                if (Filters.Scene["tsorcRevamp:TheAbyss"].Active)
                {
                    Filters.Scene["tsorcRevamp:TheAbyss"].Deactivate();
                }
            }

            #endregion

            #region Reflection Shift
            if (ReflectionShiftEnabled)
            {

                int dashCooldown = 30;
                if (ReflectionShiftKeypressTime > 0)
                {
                    ReflectionShiftKeypressTime--;
                }
                else
                {
                    //This would have looked so much nicer if controlUp, controlLeft, etc were all in an array like doubleTapCardinalTimer, but...
                    if (Player.controlUp && keyPrimed[DashUp] == 0)
                    {
                        keyPrimed[DashUp] = 1;
                    }
                    if (Player.releaseUp && keyPrimed[DashUp] == 1)
                    {
                        keyPrimed[DashUp] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashUp] == 0)
                    {
                        keyPrimed[DashUp] = 0;
                    }
                    if (Player.controlUp && Player.doubleTapCardinalTimer[DashUp] < 15 && keyPrimed[DashUp] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = -1;
                    }

                    if (Player.controlLeft && keyPrimed[DashLeft] == 0)
                    {
                        keyPrimed[DashLeft] = 1;
                    }
                    if (Player.releaseLeft && keyPrimed[DashLeft] == 1)
                    {
                        keyPrimed[DashLeft] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashLeft] == 0)
                    {
                        keyPrimed[DashLeft] = 0;
                    }
                    if (Player.controlLeft && Player.doubleTapCardinalTimer[DashLeft] < 15 && keyPrimed[DashLeft] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = -1;
                    }

                    if (Player.controlRight && keyPrimed[DashRight] == 0)
                    {
                        keyPrimed[DashRight] = 1;
                    }
                    if (Player.releaseRight && keyPrimed[DashRight] == 1)
                    {
                        keyPrimed[DashRight] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashRight] == 0)
                    {
                        keyPrimed[DashRight] = 0;
                    }
                    if (Player.controlRight && Player.doubleTapCardinalTimer[DashRight] < 15 && keyPrimed[DashRight] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.X = 1;
                    }

                    if (Player.controlDown && keyPrimed[DashDown] == 0)
                    {
                        keyPrimed[DashDown] = 1;
                    }
                    if (Player.releaseDown && keyPrimed[DashDown] == 1)
                    {
                        keyPrimed[DashDown] = 2;
                    }
                    if (Player.doubleTapCardinalTimer[DashDown] == 0)
                    {
                        keyPrimed[DashDown] = 0;
                    }
                    if (Player.controlDown && Player.doubleTapCardinalTimer[DashDown] < 15 && keyPrimed[DashDown] == 2)
                    {
                        ReflectionShiftKeypressTime = dashCooldown;
                        ReflectionShiftState.Y = 1;
                    }
                }
            }
            #endregion


            if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 10 && (Player.HeldItem.channel == true))
            {
                Player.channel = false;
            }

            if (Player.itemAnimation != 0 && (Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineSpark>() || Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.DivineBoomCannon>()))
            {
                Player.statMana -= 1;
                if (Player.statMana < 1) { Player.channel = false; }
                if (Player.statMana < 0) { Player.statMana = 0; }

            }

            if (Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.StarlitHeavenWallpaper)
            {
                Player.AddBuff(BuffID.Darkness, 60);
            }


            if ((playerX > 6083 && playerX < 6847 && playerY > 1664 && playerY < 1999) &&
            tsorcRevampWorld.RemixMap && !(Main.LocalPlayer.ZoneOverworldHeight || Main.LocalPlayer.ZoneDirtLayerHeight || Main.LocalPlayer.ZoneSkyHeight) && Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.ObsidianBrickUnsafe)
            {     
                bool DarkCloudIsAlive = false;

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && npc.type == ModContent.NPCType<DarkCloud>()) 
                    {
                        DarkCloudIsAlive = true;
                        break;
                    }
                }

                if (!DarkCloudIsAlive)
                {
                    Player.AddBuff(BuffID.Blackout, 60);
                    Player.AddBuff(BuffID.Darkness, 60);
                    Player.AddBuff(BuffID.Battle, 60);
                }
            }

            if (Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.PinkDungeonSlab)
            {
                Player.AddBuff(BuffID.Darkness, 5 * 60);
                Player.AddBuff(BuffID.WitheredArmor, 5 * 60);
            }

            if (tsorcRevampWorld.SuperHardMode && Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.HellstoneBrickUnsafe)
            {
                Player.AddBuff(BuffID.OnFire, 5 * 60);
                Player.AddBuff(BuffID.WitheredArmor, 5 * 60);
            }

            if (Framing.GetTileSafely(new Point((int)Player.position.X / 16, (int)Player.position.Y / 16)).WallType == WallID.LavaMossBlockWall)
            {
                Player.AddBuff(BuffID.OnFire, 5 * 60);
                Player.AddBuff(BuffID.WitheredArmor, 5 * 60);
            }

            if (Player.HasBuff(ModContent.BuffType<NondescriptOwlBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>()] == 0)
            {
                Item staff = new();
                staff.SetDefaults(ModContent.ItemType<Items.Weapons.Summon.PeculiarSphere>());
                int damage = staff.damage;
                if (Main.myPlayer == Player.whoAmI)
                {
                    int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>(), damage, 0, Player.whoAmI);
                    Main.projectile[p].originalDamage = damage;
                }
            }

            if (Player.HasBuff(ModContent.BuffType<SunsetQuasarBuff>()) && Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarMinion>()] == 0)
            {
                Item staff = new();
                staff.SetDefaults(ModContent.ItemType<Items.Weapons.Summon.SunsetQuasar>());
                int damage = staff.damage;
                if (Main.myPlayer == Player.whoAmI)
                {
                    int p = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarMinion>(), damage, 0, Player.whoAmI);
                    Main.projectile[p].originalDamage = damage;
                }
            }
            if (Player.HasBuff(BuffID.ShadowFlame))
            {
                int dust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.ShadowbeamStaff, 0f, 0f, 30, default, Main.rand.NextFloat(1f, 2f));
                Main.dust[dust].noGravity = true;
            }
        
            //--------------------

            //TODO REMOVE WHEN FINALIZED

            //--------------------
            return;




            if (finishedQuest)
                return;
            //{72, 492} is the house on the very left edge, where the shaman elder lives
            //64x32 rectangle to start the quest
            if (Collision.CheckAABBvAABBCollision(Player.position, new Vector2(Player.width, Player.height), new Vector2(72, 492) * 16, new Vector2(64, 32) * 16) && !startedQuest)
            {
                startedQuest = true;
                Main.NewText(LangUtils.GetTextValue("Quest.Start"));
            }
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (!startedQuest)
                return;

            //stay underground!
            if (!Player.ShoppingZone_BelowSurface)
            {
                touchedSurface = true;
            }

            //if the player is on the left edge of the world and hasnt gone past the angler's house, they're in the grace zone
            if (Player.position.ToTileCoordinates16().X < 350)
            {
                touchedSurface = false;
            }

            if (touchedSurface)
            {
                startedQuest = false;
                Main.NewText(LangUtils.GetTextValue("Quest.Surface"));
            }

            //teleporting through a one block thick wall horizontally with a
            //rod of discord gives just over 36.1f distance if done perfectly.
            //with max movespeed gear the most i could get was around 27f falling
            //flying can get you as high as 38ish, but that's only possible outdoors
            //because there are no ceilings, and at that point youre already disqualified.
            //its possible to get over 36f by flying up the side of the pyramid though...
            //just dont do that, i guess? its kinda hard anyway
            if (Vector2.Distance(Player.OldPos(1), Player.position) > 36f)
            {
                startedQuest = false;
                Main.NewText(LangUtils.GetTextValue("Quest.Teleport"));
            }

            //{7909, 1081} is the underwater observatory's top left corner (legacy 2000-space; mapped on the expanded
            //world), and {320, 119} is its rectangular SIZE (not a coordinate — never transformed)
            if (Collision.CheckAABBvAABBCollision(Player.position, new Vector2(Player.width, Player.height), ExpandedWorldTransform.MapWorld(new Vector2(7909, 1081) * 16), new Vector2(320, 119) * 16))
            {
                Main.NewText(LangUtils.GetTextValue("Quest.Finish"));
                Player.QuickSpawnItem(Player.GetSource_GiftOrReward(), ItemID.RodofDiscord);
                finishedQuest = true;
            }
        }

        public override void PreUpdateBuffs()
        {
            if (BearerOfTheCurse)
            {
                Player.GetModPlayer<ArcaneSorceryPlayer>().Enabled = true;
            }
            
            //Gives the DPS meter effect for free
            //Do not ask why it is called "accDreamCatcher"
            //This has to go here for it to work. Don't ask me why either. I value my sanity too much to go check.
            Player.accDreamCatcher = true;

            if (chestBank >= 0)
            {
                DoPortableChest<SafeProjectile>(ref chestBank, ref chestBankOpen);
            }
            if (chestPiggy >= 0)
            {
                DoPortableChest<PiggyBankProjectile>(ref chestPiggy, ref chestPiggyOpen);
            }

            if (!Main.playerInventory)
            {
                chestPiggy = -1;
                chestPiggyOpen = false;
                chestBank = -1;
                chestBankOpen = false;
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.GetModPlayer<tsorcRevampPlayer>().SoulVessel > 0)
                {
                    Player.statManaMax2 += Player.GetModPlayer<tsorcRevampPlayer>().SoulVessel * Items.SoulVessel.MaxManaIncrease;
                }
            }

            if (!NPC.downedGolemBoss && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !NPC.downedEmpressOfLight)
            {
                //Legacy 2000-space TILE coord (compared against Player.Center / 16); mapped on the expanded world.
                Vector2 arena = ExpandedWorldTransform.MapTile(new Vector2(4468, 365));
                float distance = Vector2.DistanceSquared(Player.Center / 16, arena);

                //If EoL isn't alive, do the big forcefield
                if (!NPC.AnyNPCs(NPCID.HallowBoss))
                {
                    if (distance < 42500)
                    {
                        float proximity = distance - 22500;
                        proximity /= 20000f;
                        proximity = 1 - proximity;
                        for (int i = 0; i < 10f * proximity * proximity; i++)
                        {
                            Vector2 diff = Player.Center - arena * 16;
                            diff.Normalize();
                            diff *= 2400;

                            diff = diff.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi / 30, MathHelper.Pi / 30));

                            Vector2 vel = diff;
                            vel.Normalize();
                            vel = vel.RotatedBy(Main.rand.NextBool() ? MathHelper.PiOver2 : -MathHelper.PiOver2);
                            Dust.NewDustPerfect(arena * 16 + diff, DustID.ShadowbeamStaff, vel, default, default, 1.5f * proximity).noGravity = true;
                        }

                        if (distance < 22500)
                        {
                            for (int p = 0; p < 1000; p++)
                            {
                                if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == 7)
                                {
                                    FieldTimer++;
                                    if (FieldTimer == 1)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Grapple"), Color.Orange);
                                        TextCooldown = 350;
                                    }
                                    if (FieldTimer >= 340)
                                    {
                                        Player.velocity += new Vector2(0, -15);
                                        Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30);
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Snap"), Color.Red);
                                    }
                                }
                            }

                            Player.velocity += UsefulFunctions.Aim(ExpandedWorldTransform.MapWorld(new Vector2(4484, 355) * 16), Player.Center, 20);
                            if (TextCooldown <= 0)
                            {
                                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("EoLForcefield.Expelled"), Color.Pink);
                                TextCooldown = 240;
                            }
                        }
                        else
                        {
                            FieldTimer = 0;
                        }
                    }
                }
            }
            if (tsorcRevampWorld.RemixMap && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && (!NPC.downedTowerSolar || !NPC.downedTowerVortex || !NPC.downedTowerNebula || !NPC.downedTowerStardust))
            {
                Vector2 arenaLC = new Vector2(171, 202);
                float distanceLC = Vector2.DistanceSquared(Player.Center / 16, arenaLC);

                if (!NPC.AnyNPCs(NPCID.CultistBoss))
                {
                    if (distanceLC < 42500)
                    {
                        float proximity = distanceLC - 22500;
                        proximity /= 20000f;
                        proximity = 1 - proximity;

                        for (int i = 0; i < 10f * proximity * proximity; i++)
                        {
                            Vector2 diff = Player.Center - arenaLC * 16;
                            diff.Normalize();
                            diff *= 2400;

                            diff = diff.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi / 30, MathHelper.Pi / 30));

                            Vector2 vel = diff;
                            vel.Normalize();
                            vel = vel.RotatedBy(Main.rand.NextBool() ? MathHelper.PiOver2 : -MathHelper.PiOver2);
                            Dust.NewDustPerfect(arenaLC * 16 + diff, DustID.AncientLight, vel, default, default, 1.5f * proximity).noGravity = true;
                        }

                        if (distanceLC < 22500)
                        {
                            for (int p = 0; p < 1000; p++)
                            {
                                if (Main.projectile[p].active && Main.projectile[p].owner == Player.whoAmI && Main.projectile[p].aiStyle == 7)
                                {
                                    FieldTimer++;
                                    if (FieldTimer == 1)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("LunaticForcefield.Grapple"), Color.Cyan);
                                        TextCooldown = 350;
                                    }
                                    if (FieldTimer >= 340)
                                    {
                                        Player.velocity += new Vector2(0, -15);
                                        Player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30);
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("LunaticForcefield.Snap"), Color.Green);
                                    }
                                }
                            }

                            Player.velocity += UsefulFunctions.Aim(arenaLC * 16, Player.Center, 20);
                            if (TextCooldown <= 0)
                            {
                                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("LunaticForcefield.Expelled"), Color.Pink);
                                TextCooldown = 240;
                            }
                        }
                    }
                    else
                    {
                        FieldTimer = 0;
                    }
                }
            }
        }

        /*public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            health = StatModifier.Default;
            mana = StatModifier.Default;
            health.Base = -cursePoints;
        }*/

        public override void PostUpdateBuffs()
        {
            foreach (Item thisItem in PotionBagItems)
            {
                if (thisItem != null && !thisItem.IsAir)
                {
                    thisItem.ModItem?.UpdateInventory(Player);
                }
            }

            if (MiakodaCrescentBoost)
            {
                Player.GetDamage(DamageClass.Generic) += Items.Pets.MiakodaCrescent.Dmg2 / 100f;
            }

            if (MiakodaNewBoost)
            {
                Player.moveSpeed += Items.Pets.MiakodaNew.MoveSpeed2 / 100f;
                Player.endurance += Items.Pets.MiakodaNew.DamageReduction / 100f;
                Player.noKnockback = true;
            }

            if (Player.HasBuff(BuffID.WellFed))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCWellFedStaminaRegen / 100f;
            }
            if (Player.HasBuff(BuffID.WellFed2))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCPlentySatisfiedStaminaRegen / 100f;
            }
            if (Player.HasBuff(BuffID.WellFed3))
            {
                Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += MinorEdits.BotCExquisitelyStuffedStaminaRegen / 100f;
            }


            if (Player.HasBuff(BuffID.TheTongue))
            {
                for (int i = 0; i < 9; i++)
                {
                    CombatText.NewText(Player.Hitbox, CombatText.DamagedFriendly, 999999999, true);
                }
                Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(Player.name + LangUtils.GetTextValue("DeathText.WallDeathReason")), 999999999, 1);
            }

            if (!Player.HasBuff(ModContent.BuffType<CurseBuildup>()))
            {
                CurseLevel = 1; //Not sure why 1 is the default
            }

            if (!Player.HasBuff(ModContent.BuffType<PowerfulCurseBuildup>()))
            {
                PowerfulCurseLevel = 1; //Not sure why 1 is the default
            }
        }
        public override void PostUpdateEquips()
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                Player.GetAttackSpeed(DamageClass.Melee) *= LethalTempoMeleeBaseAttackSpeedMult + (LethalTempoStacks * LethalTempoBonusAttackSpeedPerStack);
                Player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) /= LethalTempoMeleeBaseAttackSpeedMult + (LethalTempoStacks * LethalTempoBonusAttackSpeedPerStack); //neutralizing Lethal Tempo attack speed changes for whips
                
                Player.GetDamage(DamageClass.Summon) *= ConquerorSummonBaseDamageMult + (ConquerorStacks * ConquerorBonusDmgPerStack);
                Player.GetDamage(DamageClass.MagicSummonHybrid) /= ConquerorSummonBaseDamageMult + (ConquerorStacks * ConquerorBonusDmgPerStack); //neutralizing Conqueror damage changes
                Player.GetDamage(DamageClass.MagicSummonHybrid) *= 1f + ConquerorStacks * ConquerorBonusDmgPerStack / 4.5f; //adding small benefit for usage of Conqueror
                if (ConquerorStacks == ConquerorMaxStacks)
                {
                    SummonTagDuration += FullConquerorBonusTagDuration;
                }
            }

            // Tier-aware mana regen penalty.
            // Both Unkindled and BotC magic players are expected to lean on the Cerulean Flask. The pin/penalty
            // only fires when "in combat or away from a bonfire" — sitting at a bonfire with no boss alive lets
            // mana regen normally for either tier.
            // BotC: hard pin to manaRegenDelay = 100 each frame, so the countdown never reaches 0 and Stage-2
            //       regen never starts. Mana comes exclusively from drinking Cerulean charges.
            // Unkindled: first cut the positive manaRegenBonus to 40% to dampen late-game item/armor scaling
            //       (vanilla multiplies regen by (1 + manaRegenBonus/100), which gear inflates past any flat
            //       subtraction), then subtract a fraction of statManaMax2 from manaRegenBonus. Vanilla's rate
            //       formula includes stationaryBonus = M/3 when standing still, so to hit consistent percent
            //       targets across all pool sizes we use different subtractions per velocity state:
            //          standing  → subtract M * 2/5
            //          moving    → subtract M * 3/10
            //       Cerulean Flask remains the active recovery option; passive regen is intentionally weak.
            if (Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200]) || !Player.HasBuff(ModContent.BuffType<Bonfire>()))
            {
                if (Player.GetModPlayer<tsorcRevampPlayer>().Unkindled)
                {
                    // Dampen gear scaling first. Vanilla's regen rate is multiplied by (1 + manaRegenBonus/100),
                    // and late-game items/armor inflate manaRegenBonus enough that a flat subtraction alone can't
                    // keep up. Cut the positive bonus to 40% so item/armor contributions lose most of their
                    // effect, THEN apply the flat per-velocity subtraction below. Only dampen when positive so
                    // we never weaken an already-negative (nerfed) bonus.
                    if (Player.manaRegenBonus > 0)
                    {
                        Player.manaRegenBonus = Player.manaRegenBonus * 2 / 5;
                    }

                    if (Player.velocity == Vector2.Zero)
                    {
                        Player.manaRegenBonus -= Player.statManaMax2 * 2 / 5;
                    }
                    else
                    {
                        Player.manaRegenBonus -= Player.statManaMax2 * 3 / 10;
                    }

                    if (unkindledManaDelayTimer > 0)
                    {
                        Player.manaRegenDelay = Math.Max(Player.manaRegenDelay, unkindledManaDelayTimer);
                    }
                }
            }
            else
            {
                unkindledManaDelayTimer = 0;
            }

            // Lifegem / RadiantLifegem / StarlightShard heal & mana-restoration ticks.
            // These were originally inside the BotC-gated PostUpdateEquips block, which meant Unkindled
            // players could apply the buffs but never receive HP/mana from them. Gated on SoulsMode so
            // both Unkindled and Bearer of the Curse get the actual healing/restoration.
            if (Player.GetModPlayer<tsorcRevampPlayer>().SoulsMode)
            {
                #region Lifegem Healing and Starlight Shard Restoration


                if (LifegemHealing)
                {
                    healingTimer++;

                    if (healingTimer == Lifegem.HealingDivisor)
                    {
                        Player.statLife += 1;
                        healingTimer = 0;
                    }
                }

                if (RadiantLifegemHealing)
                {
                    healingTimer++;

                    if (healingTimer == RadiantLifegem.HealingDivisor)
                    {
                        Player.statLife += 1;
                        healingTimer = 0;
                    }
                }

                if (!RadiantLifegemHealing && !LifegemHealing)
                {
                    healingTimer = 0;
                }

                if (StarlightShardRestoration) //Restores 1% of maximum mana over 12 seconds by default
                {
                    restorationTimer += (float)Player.statManaMax2 / (100f * 60f) * (1f + ((float)Player.manaRegenBonus / 10f)); //1% of maximum mana per second, since there are 60 ticks per second, manaregenbonuses are usually in the double digits so this is insane scaling

                    if (restorationTimer >= 10f)
                    {
                        Player.statMana += 10;
                        restorationTimer -= 10f;
                    }
                    if (restorationTimer >= 1f)
                    {
                        Player.statMana += 1;
                        restorationTimer -= 1f;
                    }
                }

                if (!StarlightShardRestoration)
                {
                    restorationTimer = 0;
                }

                #endregion
            }
            if (PapyrusScarab)
            {
                SummonTagStrength += SummonerEdits.ScarabTagBoost / 100f * (HerculesBeetle ? 0 : 1f);
                SummonTagDuration += SummonerEdits.ScarabTagBoost / 100f * (NecromanticScroll ? 0 : 1f);
            }
            if (HerculesBeetle)
            {
                SummonTagStrength += SummonerEdits.BeetleSummonTagStrengthBoost / 100f;
            }
            if (NecromanticScroll)
            {
                SummonTagDuration += SummonerEdits.ScrollSummonTagDurationBoost / 100f;
            }
            if (SeveringDuskDashTime > 0)
            {
                Player.GetDamage(DamageClass.Melee) += SeveringDusk.DashBonusDmg / 100f;
            }
            if (PhoenixSkull && tsorcRevampWorld.BossAlive && !BossBlockedPhoenixRevive)
            {
                Player.AddBuff(ModContent.BuffType<PhoenixRebirthCooldown>(), Items.Accessories.Defensive.PhoenixSkull.BossChargeDuration * 60);
                BossBlockedPhoenixRevive = true;
            }
            if (BossBlockedPhoenixRevive && !tsorcRevampWorld.BossAlive)
            {
                BossBlockedPhoenixRevive = false;
            }
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableGloveAutoswing)
            {
                Player.autoReuseGlove = false;
            }
            /*if (ModContent.GetInstance<tsorcRevampConfig>().DisableAutomaticQuickMana)
            {
                Player.manaFlower = false;
            }*/ // needs to be set in ceruleanflask file
            if (ModContent.GetInstance<tsorcRevampConfig>().DisableRifleScopeZoom)
            {
                Player.scope = false;
            }
            if (manaShield > 0)
            {
                Player.manaRegenBuff = false;
            }

            if (Shunpo && !Player.HasBuff(ModContent.BuffType<ShunpoBlinkCooldown>()))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];

                    if (other.active && !other.friendly && other.Hitbox.Intersects(Utils.CenteredRectangle(Main.MouseWorld, MouseHitboxSize)))
                    {
                        Lighting.AddLight(other.Center, Color.Red.ToVector3() * 0.35f);
                        UsefulFunctions.DustRing(other.Center, other.width / 2, DustID.Titanium, 5, 1);
                        UsefulFunctions.DustRing(other.Center, other.width / 4, DustID.Adamantite, 5, 2);
                    }
                }
            }
            if (ShadowWeight)
            {
                Player.GetJumpState(ExtraJump.BlizzardInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.FartInAJar).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.TsunamiInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.SandstormInABottle).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.UnicornMount).Enable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.canRocket = false;
                Player.rocketTime = 0;
                Player.jumpBoost = false;
                Player.wingTime = 0;
                float speedCap = 12;
                if (Player.velocity.X > speedCap)
                {
                    Player.velocity.X = speedCap;
                }
                if (Player.velocity.X < -speedCap)
                {
                    Player.velocity.X = -speedCap;
                }
            }

            if (TornWings)
            {
                // Should allow for a single jump, just nothing while midair.

                Player.wingTime = 0;
                Player.moveSpeed *= 0.8f;

                Player.canRocket = false;
                Player.rocketTime = 0;
                Player.jumpBoost = false;
            }

            if (Crippled)
            {
                Player.GetJumpState(ExtraJump.BlizzardInABottle).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.CloudInABottle).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.FartInAJar).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.TsunamiInABottle).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.SandstormInABottle).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.UnicornMount).Disable()/* tModPorter Suggestion: Call Enable() if setting this to true, otherwise call Disable(). */;
                Player.GetJumpState(ExtraJump.BasiliskMount).Disable();
                Player.GetJumpState(ExtraJump.GoatMount).Disable();
                Player.GetJumpState(ExtraJump.SantankMount).Disable();

                Player.canRocket = false;
                Player.rocketTime = 0;
                Player.jumpBoost = false;
                Player.jumpSpeedBoost = 0f;
                Player.wingTime = 0;
                Player.moveSpeed *= 0.9f;
            }

            if (WitchkingsGrasp)
            {
                PullFrame++;
                // Generate dust every 5 frames
                if (PullFrame % 5 == 0)
                {
                    PullFrame = 0;

                    float dustSpeed = Main.rand.NextFloat(0, 2f);
                    Rectangle playerRect = new Rectangle(0, 0, 20, 45);

                    // The stronger the pull, the more dust created.
                    for (int i = 0; i < PullStrength; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2FromRectangle(playerRect);
                        Vector2 velocity = new Vector2(dustSpeed, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                        Dust.NewDustPerfect(Player.position + offset, DustID.Shadowflame, velocity, Scale: 1).noGravity = false;
                    }
                }
            }

            for (int i = 0; i < 50; i++)
            {
                //block souls from going in normal inventory slots
                tsorcRevampPlayer modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                if (Player.inventory[i].type == ModContent.ItemType<DarkSoul>())
                {
                    //if the player's soul slot is empty
                    if (modPlayer.SoulSlot.Item.type != ModContent.ItemType<DarkSoul>())
                    {
                        modPlayer.SoulSlot.Item = Player.inventory[i].Clone();
                    }
                    else
                    {
                        modPlayer.SoulSlot.Item.stack += Player.inventory[i].stack;
                    }
                    //dont send the souls to the normal inventory
                    Player.inventory[i].TurnToAir();
                }

            }


            if (Shockwave)
            {

                if (Player.controlDown && Player.velocity.Y != 0f)
                {
                    Player.gravity += 5f;
                    Player.maxFallSpeed *= 1.25f;
                    if (!Falling)
                    {
                        fallStartY = Player.position.Y;
                    }
                    if (Player.velocity.Y > 12f)
                    {
                        Falling = true;
                        StopFalling = 0;
                        Player.noKnockback = true;
                    }
                }
                if (Player.velocity.Y == 0f && Falling && Player.controlDown && !Player.wet)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        int dustIndex2 = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, 31, 0f, 0f, 100);
                        Main.dust[dustIndex2].scale = 0.1f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].fadeIn = 1.5f + Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex2].noGravity = true;
                    }
                    FallDist = (int)((Player.position.Y - fallStartY) / 16);
                    if (FallDist > 5)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Player.Center);
                        if (Main.myPlayer == Player.whoAmI)
                        {
                            for (int i = -9; i < 10; i++)
                            { //19 projectiles
                                Vector2 shotDirection = new Vector2(0f, -16f);
                                float damageMultiplier = tsorcRevampWorld.SuperHardMode ? 12.0f : (Main.hardMode ? 6.0f : 3.0f);
                                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, shotDirection.RotatedBy(MathHelper.ToRadians(0 - (10f * i))), ModContent.ProjectileType<Projectiles.Shockwave>(), (int)(FallDist * damageMultiplier), 12, Player.whoAmI, 15f);
                            }
                        }
                    }


                    Falling = false;
                }
                if (Player.velocity.Y <= 2f)
                {
                    StopFalling++;
                }
                else
                {
                    StopFalling = 0;
                }
                if (StopFalling > 1)
                {
                    Falling = false;
                }

            }
            if (!Shockwave)
            {
                Falling = false;
            }

            if (CrimsonDrain)
            {

                for (int l = 0; l < 200; l++)
                {
                    NPC nPC = Main.npc[l];
                    if (nPC.active && !nPC.friendly && nPC.damage > 0 && !nPC.dontTakeDamage && !nPC.buffImmune[ModContent.BuffType<CrimsonBurn>()] && Vector2.Distance(Player.Center, nPC.Center) <= 240)
                    {
                        nPC.AddBuff(ModContent.BuffType<CrimsonBurn>(), 60);
                    }
                }

                Vector2 centerOffset = new Vector2(Player.Center.X + 2 - Player.width / 2, Player.Center.Y + 6 - Player.height / 2);
                const float circleRadius = 150f;
                const int dustType = 235;
                const int particleCount = 28;

                for (int j = 1; j < particleCount; j++)
                {
                    Vector2 dustPosition = centerOffset + (Vector2.One * circleRadius).RotatedByRandom(Math.PI * 2);
                    int dustIndex = Dust.NewDust(dustPosition, Player.width / 2, Player.height / 2, dustType, Player.velocity.X, Player.velocity.Y);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].scale *= 0.75f;
                }
            }

            #region Soul Siphon Dusts


            if (SoulSiphon)
            {

                if (Main.rand.NextBool(4))
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 0.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 90f;
                    Main.dust[z].position = Player.Center - vectorother;
                }

                if (Main.rand.NextBool(4)) //innermost "ring"
                {
                    int z = Dust.NewDust(Player.position, Player.width, Player.height, 89, 0f, 0f, 120, default, 1f);
                    Main.dust[z].noGravity = true;
                    Main.dust[z].velocity *= 2.75f;
                    Main.dust[z].fadeIn = 1.3f;
                    Vector2 vectorother = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                    vectorother.Normalize();
                    vectorother *= (float)Main.rand.Next(50, 100) * 0.055f;
                    Main.dust[z].velocity = vectorother;
                    vectorother.Normalize();
                    vectorother *= 45f;
                    Main.dust[z].position = Player.Center - vectorother;
                }
            }

            #endregion
            #region consistent hellstone and spike damage
            float REDUCE = CheckReduceDefense(Player.position, Player.width, Player.height, Player.fireWalk); // <--- added firewalk parameter
            if (REDUCE != 0)
            {
                REDUCE = 1f - REDUCE;
                Player.statDefense *= 0;
                Player.endurance = 0;
            }
            #endregion
            #region boss zen
            if (tsorcRevampWorld.BossAlive)
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().BossZenConfig)
                    Player.AddBuff(ModContent.BuffType<BossZenBuff>(), 2, false);

                if (Player.position.Y < 3200)
                {
                    Player.AddBuff(ModContent.BuffType<GravityField>(), 2, false);
                }
            }
            #endregion
            #region boss magnet
            //actual item grab range is in GlobalItem::GrabRange
            if (bossMagnet)
            {
                bossMagnetTimer--;
            }
            if (bossMagnetTimer == 0)
            {
                bossMagnet = false;
            }
            #endregion

            float shiftDistance = 7;
            #region Reflection Shift

            if (ReflectionShiftKeypressTime > 20)
            {
                Player.immune = true;
            }

            if (ReflectionShiftState != Microsoft.Xna.Framework.Vector2.Zero)
            {
                Player.timeSinceLastDashStarted = 0;
                //Initiate Dash
                for (int i = 0; i < 30; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(-2, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(Player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
                if (Collision.CanHit(Player.Center, 1, 1, Player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1) || Collision.CanHitLine(Player.Center, 1, 1, Player.Center + ReflectionShiftState * shiftDistance * 16, 1, 1))
                {
                    Player.Center += ReflectionShiftState * shiftDistance * 16; //Teleport distance
                }
                FastFallTimer = 30;
                Player.velocity = ReflectionShiftState * 20; //Dash speed
                ReflectionShiftState = Vector2.Zero;

                for (int i = 0; i < 30; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(64, 64);
                    Vector2 velocity = new Vector2(5, 0).RotatedBy(offset.ToRotation()) * Main.rand.NextFloat(2);
                    Dust.NewDustPerfect(Player.Center + offset, DustID.ShadowbeamStaff, velocity, Scale: 2).noGravity = true;
                }
            }
            #endregion
            if (Player.HasBuff(ModContent.BuffType<PhoenixRebirthBuff>()) && Player.statLife >= Player.statLifeMax2 * Items.Accessories.Defensive.PhoenixSkull.LifeThreshold / 100f)
            {
                Player.ClearBuff(ModContent.BuffType<PhoenixRebirthBuff>());
            }
            if (PhoenixSkull && Player.HasBuff(ModContent.BuffType<PhoenixRebirthCooldown>()))
            {
                UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<PhoenixRebirthCooldown>(), -1);
            }
            if (BarrierRing && Player.HasBuff(ModContent.BuffType<BarrierCooldown>()))
            {
                UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<BarrierCooldown>(), -1);
            }
            if (SteraksGage && Player.statLife < (Player.statLifeMax2 * Items.Accessories.Melee.SteraksGage.LifeThreshold / 100f) && !Player.HasBuff(ModContent.BuffType<SteraksGageCooldown>()))
            {
                Player.statLife += Items.Accessories.Melee.SteraksGage.ShieldHeal;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteraksGageShield") with { Volume = 0.6f }, Player.Center);
                Player.AddBuff(ModContent.BuffType<SteraksGageCooldown>(), Items.Accessories.Melee.SteraksGage.Cooldown * 60);
            }
            if (DragoonBoots && DragoonBootsEnable)
            {
                //Player.jumpSpeed += 10f; why
                Player.jumpSpeedBoost += 6f;
            }
            if (DragoonHorn && (((Player.gravDir == 1f) && (Player.velocity.Y > 0)) || ((Player.gravDir == -1f) && (Player.velocity.Y < 0))))
            {
                Player.GetDamage(DamageClass.Melee) += Items.Accessories.Melee.DragoonHorn.MeleeDmg / 100f;
            }
            if (BrokenSpirit)
            {
                Player.noKnockback = false;
            }
            Player.maxMinions *= MaxMinionTurretMultiplier;
            Player.maxTurrets *= MaxMinionTurretMultiplier;

            if (CrystallineShard)
            {
                CrystallinePower = Player.maxMinions / MaxMinionTurretMultiplier * Items.Accessories.Summon.CrystallineShard.CrystallinePowerPerMinion;
                Player.GetDamage(DamageClass.SummonMeleeSpeed) += CrystallinePower * ((float)Items.Accessories.Summon.CrystallineShard.WhipDmgAmp / (float)Items.Accessories.Summon.CrystallineShard.CrystallinePowerPerMinion) / 100f;
                SummonTagStrength += CrystallinePower / 100f;
                Player.whipRangeMultiplier -= CrystallinePower * ((float)Items.Accessories.Summon.CrystallineShard.BadWhipRange / (float)Items.Accessories.Summon.CrystallineShard.CrystallinePowerPerMinion) / 100f;
            }

            if (Player.HasAmmo(Player.HeldItem) && Player.HeldItem.useAmmo != 0 && AmmoBox)
            {
                Player.GetCritChance(DamageClass.Ranged) += Player.ChooseAmmo(Player.HeldItem).damage / 2;
            }

            if (ZirconRing)
            {
                Player.statLifeMax2 += (int)(Player.statLifeMax2 * Items.Accessories.Defensive.Rings.ZirconRing.MaxLifeIncrease / 100f);
            }

            if (CurseActive || powerfulCurseActive)
            {
                AddCurseStats();
                //liferegen is in updateliferegen function
            }

            if (ReboundProjectile && Main.rand.NextBool(8))
            {
                Item item = Player.HeldItem;
                Rectangle ItemBox = ItemMeleeAttackAiming.GetMeleeHitbox(Player, item);
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    //ModContent.GetInstance<MeleeEdits>().TrueMelee(item) && 
                    if (other.active && !other.friendly && ItemBox.Intersects(other.Hitbox) && UsefulFunctions.IsProjectileSafeToFuckWith(i))
                    {
                        Rectangle playerRect = Utils.CenteredRectangle(Player.Center, Player.Size);
                        //CombatText.NewText(playerRect, Color.White, "Intersect!");

                        Vector2 newVec = Main.MouseWorld - other.Center;
                        newVec.Normalize();
                        other.hostile = false;
                        other.friendly = true;
                        other.velocity = newVec * other.velocity.Length();

                        CombatText.NewText(playerRect, Color.White, LangUtils.GetTextValue("UI.Rebound"));
                    }
                }
            }

            if (Player.HeldItem.CountsAsClass(DamageClass.Melee))
            {
                /*Main.NewText("magicDamage: " + player.GetDamage(DamageClass.Magic));
				Main.NewText("magicDamageMult: " + player.GetDamage(DamageClass.Magic)Mult);
				Main.NewText((player.GetDamage(DamageClass.Magic) - player.GetDamage(DamageClass.Magic)Mult) * .5f);*/
                if (Player.HasBuff(ModContent.BuffType<MagicWeapon>()))
                {
                    float bonusDamage = ((Player.GetDamage(DamageClass.Magic).Additive * Player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * Items.tsorcGlobalItem.BonusDamage1 / 100f;
                    if (bonusDamage >= 0)
                    {
                        Player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
                if (Player.HasBuff(ModContent.BuffType<GreatMagicWeapon>()))
                {
                    float bonusDamage = ((Player.GetDamage(DamageClass.Magic).Additive * Player.GetDamage(DamageClass.Magic).Multiplicative) - 1) * Items.tsorcGlobalItem.BonusDamage2 / 100f;
                    if (bonusDamage >= 0)
                    {
                        Player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
                if (Player.HasBuff(ModContent.BuffType<CrystalMagicWeapon>()))
                {
                    float bonusDamage = (Player.GetDamage(DamageClass.Magic).Additive * Player.GetDamage(DamageClass.Magic).Multiplicative) * Items.tsorcGlobalItem.BonusDamage3 / 100f;
                    if (bonusDamage >= 0)
                    {
                        Player.GetDamage(DamageClass.Melee) += bonusDamage;
                    }
                }
            }

            if (WaterWalkingBoots)
            {
                if (LavaWalkCount < LavaWalkTimer)
                {
                    Player.waterWalk = true;
                    Player.waterWalk2 = true;
                }

                bool walkingOnLava = false;
                bool onSolidGround = false;
                bool insideLava = false;

                int startX = (int)(Player.position.X / 16f) - 1;
                int endX = (int)((Player.position.X + (float)Player.width) / 16f) + 1;

                int startY = (int)(Player.position.Y / 16f);
                int endY = (int)((Player.position.Y + (float)Player.height) / 16f) + 1;

                startX = Utils.Clamp(startX, 0, Main.maxTilesX - 1);
                endX = Utils.Clamp(endX, 0, Main.maxTilesX - 1);

                startY = Utils.Clamp(startY, 0, Main.maxTilesY - 1);
                endY = Utils.Clamp(endY, 0, Main.maxTilesY - 1);

                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        Tile tileBelow = Framing.GetTileSafely(x, y);
                        Tile tileOnPlayer = Framing.GetTileSafely(x, y - 1);

                        if (Player.velocity.Y == 0)
                        {
                            if (tileBelow.LiquidType == LiquidID.Lava)
                            {
                                walkingOnLava = true;
                                onSolidGround = false;
                            }
                            // Player is standing on something that isnt lava
                            else 
                            {
                                onSolidGround = true;
                                walkingOnLava = false;
                            }
                        }
                        if (tileOnPlayer.LiquidType == LiquidID.Lava)
                        {
                            insideLava = true;
                        }
                    }
                }

                Vector2 offset = new Vector2(0, 16);

                offset.X += Main.rand.Next(-8, 9);
                offset.Y += Main.rand.Next(-1, 2);

                // Increase timer above max if player is floating in Lava
                if ((insideLava && !walkingOnLava) && LavaWalkCount < (int)(LavaWalkTimer * 1.5f))
                {
                    LavaWalkCount++;
                    TimeAwayFromLava = 0;
                }
                // Player is walking on lava, increase the lava count
                if ((walkingOnLava && !onSolidGround) && LavaWalkCount < LavaWalkTimer)
                {
                    LavaWalkCount++;
                    TimeAwayFromLava = 0;

                    // If the time remaining is greater than 25%, generate a dust visual for the player.
                    if (Main.rand.Next(5) == 0 && LavaWalkTimer - LavaWalkCount > (int)(LavaWalkTimer * .25f))
                    {
                        Dust.NewDustPerfect(Player.Center + offset, DustID.RedTorch);
                    }
                }
                // Dp not regenerate boots if inside or walking on lava
                if (LavaWalkCount > 0 && !(insideLava || walkingOnLava))
                {
                    // Regen the boots if the player is grappled safely, on ground, or has spent more than 3 seconds away from lava
                    if (TimeAwayFromLava >= LavaWalkTimer / 2 || Player.velocity.Y == 0)
                    {
                        LavaWalkCount--;

                        // More dust if the boots are almost fully regened
                        if (Main.rand.Next(8) == 0 || LavaWalkCount < (int)(LavaWalkTimer * .18f))
                        {
                            Dust.NewDustPerfect(Player.Center + offset, DustID.BlueTorch);
                        }
                    }
                    // The player is still midair, progress the timer.
                    else
                    {
                        TimeAwayFromLava++;
                    }
                }
            }

        }

        public override void PostUpdateRunSpeeds()
        {
            if (supersonicLevel != 0)
            {
                float moveSpeedPercentBoost = 1;
                float baseSpeed = 1;

                //SupersonicBoots
                if (supersonicLevel == SoulsModeMobility.SupersonicBootsLevel)
                {
                    //moveSpeedPercentBoost is what percent of a player's moveSpeed bonus should be applied to their max running speed
                    //For vanilla hermes boots and their upgrades, this is 0
                    moveSpeedPercentBoost = 0.35f;
                    //6f is hermes boots speed.
                    baseSpeed = 6f;
                    Player.moveSpeed += 0.2f;
                }
                //SupersonicWings
                if (supersonicLevel == SoulsModeMobility.SupersonicWingsLevel || supersonicLevel == SoulsModeMobility.SupersonicWings2Level)
                {
                    moveSpeedPercentBoost = 0.5f;
                    baseSpeed = 6.8f;
                    Player.moveSpeed += 0.3f;
                }
                //Wings of Seath
                if (supersonicLevel == SoulsModeMobility.WingsOfSeathLevel)
                {
                    moveSpeedPercentBoost = 1f;
                    baseSpeed = 7.5f;
                    Player.moveSpeed += 0.6f;
                }


                //((player.moveSpeed * 0.5f) + 0.5) means 50% of the player's moveSpeed bonus will be applied
                //The general form is ((player.moveSpeed * %theyshouldget) + (1 - %theyshouldget))
                Player.accRunSpeed = baseSpeed * ((Player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));
                Player.maxRunSpeed = baseSpeed * ((Player.moveSpeed * moveSpeedPercentBoost) + (1 - moveSpeedPercentBoost));

                if (SoulsModeMobility.Enabled(Player))
                {
                    float cappedSpeed = supersonicLevel switch
                    {
                        SoulsModeMobility.SupersonicBootsLevel => SoulsModeMobility.SupersonicBootsRunSpeed,
                        SoulsModeMobility.SupersonicWingsLevel => SoulsModeMobility.SupersonicWingsRunSpeed,
                        SoulsModeMobility.SupersonicWings2Level => SoulsModeMobility.SupersonicWings2RunSpeed,
                        SoulsModeMobility.WingsOfSeathLevel => SoulsModeMobility.WingsOfSeathRunSpeed,
                        _ => Player.maxRunSpeed
                    };

                    Player.accRunSpeed = Math.Min(cappedSpeed, SoulsModeMobility.GlobalRunSpeedCap);
                    Player.maxRunSpeed = Math.Min(cappedSpeed, SoulsModeMobility.GlobalRunSpeedCap);
                }

                if (FastFallTimer > 0)
                {
                    Player.maxFallSpeed = 50;
                    FastFallTimer--;
                }

                if (Suppressed)
                {
                    // Roughly early-hardmode boot territory (Spectre/Lightning Boots are 6f-6.75f) for the 3
                    // named items; Wings of Seath is nerfed less harshly since it's a much later-game item and
                    // should still clearly outclass the other three while suppressed.
                    bool isSeath = supersonicLevel == SoulsModeMobility.WingsOfSeathLevel;
                    float suppressedRunSpeed = isSeath ? 7f : 6f;
                    float suppressedAcceleration = isSeath ? 0.14f : 0.1f;

                    Player.accRunSpeed = Math.Min(Player.accRunSpeed, suppressedRunSpeed);
                    Player.maxRunSpeed = Math.Min(Player.maxRunSpeed, suppressedRunSpeed);
                    Player.runAcceleration = Math.Min(Player.runAcceleration, suppressedAcceleration);

                    bool hasSuppressedWings = supersonicLevel == SoulsModeMobility.SupersonicWingsLevel
                        || supersonicLevel == SoulsModeMobility.SupersonicWings2Level
                        || isSeath;
                    if (hasSuppressedWings)
                    {
                        int suppressedWingTime = isSeath ? SoulsModeMobility.WingsOfSeathSuppressedFlightTime : 90; // 1.5s, or 3s for Wings of Seath
                        Player.wingTimeMax = Math.Min(Player.wingTimeMax, suppressedWingTime);
                        if (Player.wingTime > Player.wingTimeMax)
                        {
                            Player.wingTime = Player.wingTimeMax;
                        }
                    }
                }
            }

            if (SoulsModeMobility.Enabled(Player))
            {
                Player.accRunSpeed = Math.Min(Player.accRunSpeed, SoulsModeMobility.GlobalRunSpeedCap);
                Player.maxRunSpeed = Math.Min(Player.maxRunSpeed, SoulsModeMobility.GlobalRunSpeedCap);
            }

            if (Player.HasBuff<MarilithHold>() || Player.HasBuff<MarilithWind>())
            {
                int? marilithIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>());
                if (marilithIndex != null)
                {
                    NPC marilith = Main.npc[marilithIndex.Value];

                    Player.velocity -= UsefulFunctions.Aim(Player.Center, marilith.Center, 0.25f);

                    if (Player.HasBuff<MarilithHold>())
                    {
                        Player.velocity = -UsefulFunctions.Aim(Player.Center, marilith.Center, 4f);
                    }

                    /*
                    double angleDiff = UsefulFunctions.CompareAngles(Player.velocity, vectorDiff);
                    if (angleDiff > MathHelper.Pi / 2)
                    {
                    }*/
                }
            }
        }
        public override void UpdateLifeRegen()
        {
            if (CurseActive && CurseLifeRegenerationBonus > 0)
            {
                Player.lifeRegen += (int)(CurseLifeRegenerationBonus * CursePositiveStatsMultiplier);
            }
            if (powerfulCurseActive && powerfulCurseLifeRegenerationBonus > 0)
            {
                Player.lifeRegen += (int)(powerfulCurseLifeRegenerationBonus * CursePositiveStatsMultiplier);
            }
        }
        public override void UpdateBadLifeRegen()
        {
            if (DarkInferno)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 11;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f); //54 was 58
                    Main.dust[dust2].noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    // Render fire particles [every frame]
                    int particle = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 54, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[particle].noGravity = true;
                    Main.dust[particle].velocity *= 1.4f;
                    int lol = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 58, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 3f);
                    Main.dust[lol].noGravity = true;
                    Main.dust[lol].velocity *= 1.4f;
                }

                // Render smoke particles [every other frame]
                if (Main.GameUpdateCount % 2 == 0)
                {
                    int particle2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 58, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f + (float)Main.rand.Next(2));
                    Main.dust[particle2].noGravity = true;
                    Main.dust[particle2].noLight = true;
                    Main.dust[particle2].fadeIn = 3f;
                }
            }

            if (AbyssInferno)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 16;
                for (int j = 0; j < 2; j++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, DustID.PinkTorch, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1.2f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 223, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1.2f); //54 was 58
                    Main.dust[dust2].noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    // Render fire particles [every frame]
                    int particle = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, DustID.PinkTorch, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 2.3f);
                    Main.dust[particle].noGravity = true;
                    Main.dust[particle].velocity *= 1.4f;
                    int lol = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 223, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 160, default(Color), 2.3f);
                    Main.dust[lol].noGravity = true;
                    Main.dust[lol].velocity *= 1.4f;
                }
            }

            if (PhazonCorruption)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 7;
                for (int j = 0; j < 4; j++)
                {
                    int dust = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, 29, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(Player.position, Player.width / 2, Player.height / 2, DustID.FireworkFountain_Blue, (Player.velocity.X * 0.2f), Player.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (SOADrain || PowerWithin)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                // Both drains are percent-of-max-life and they STACK deliberately — Symbol of Avarice and Power
                // Within are unrelated curses, and wearing the greed helm while burning your own life force
                // should cost what both cost.
                if (SOADrain)
                {
                    Player.lifeRegen -= PercentLifeDrain(Items.Armors.SymbolOfAvarice.LifeDrainPercent);
                }
                if (PowerWithin)
                {
                    Player.lifeRegen -= PercentLifeDrain(Items.Tools.PowerWithin.LifeDrainPercent);
                }
                if (Main.rand.NextBool(3))
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 235, Player.velocity.X, Player.velocity.Y, 140, default, 0.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (VOEGDrain)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 4;
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(Player.position, Player.width, Player.height, 235, Player.velocity.X, Player.velocity.Y, 140, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CowardsAffliction)
            {
                if (Player.lifeRegen > 0)
                {
                    Player.lifeRegen = 0;
                }
                Player.lifeRegenTime = 0;
                Player.lifeRegen -= 240;
            }

            if (gilled && Main.tile[(int)Player.Top.X / 16, ((int)Player.Top.Y + 10) / 16].LiquidAmount == 0)
            {
                if (Player.breath >= 0)
                {
                    Player.breath -= 4;
                }
                if (Player.breath <= 0)
                {
                    Player.lifeRegen -= 20;
                }
            }

            if (Player.HasBuff(BuffID.ShadowFlame))
            {
                Player.lifeRegen -= 12;
            }

            if (CurseActive && CurseLifeRegenerationBonus < 0)
            {
                Player.lifeRegen -= -(int)CurseLifeRegenerationBonus;
            }
            if (powerfulCurseActive && powerfulCurseLifeRegenerationBonus < 0)
            {
                Player.lifeRegen -= -(int)powerfulCurseLifeRegenerationBonus;
            }
            if (Player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Player.GetModPlayer<tsorcRevampStaminaPlayer>().minionStaminaCap && Player.lifeRegen > 0)
            {
                Player.lifeRegen /= 2;
            }

            // Unkindled and Bearer of the Curse now share the same −50% life regen, applied by the
            // "stamina not full" halving above (which is not class-gated, so it covers both).
            //
            // Unkindled previously took an extra ×3/4 on top of that halving, leaving it on 37.5% while
            // BotC sat on 50% — the gentler tier was punished harder than the harsh one, the opposite of
            // the intent. Removing the extra penalty is what puts them on parity; do not re-add a
            // class-specific multiplier here without first making the halving above class-gated.

            Player.lifeRegen -= RashBadLifeRegenPerSec * 2;
        }


        public string PickDeathText()
        {
            string text;
            DeathTextList = new List<string>() {
             LangUtils.GetTextValue("DeathText.01"),
             LangUtils.GetTextValue("DeathText.02"),
             LangUtils.GetTextValue("DeathText.03"),
             LangUtils.GetTextValue("DeathText.04"),
             LangUtils.GetTextValue("DeathText.05"),
             LangUtils.GetTextValue("DeathText.06"),
             LangUtils.GetTextValue("DeathText.07"),
             LangUtils.GetTextValue("DeathText.08"),
             LangUtils.GetTextValue("DeathText.09"),
             LangUtils.GetTextValue("DeathText.PermaPot"),
             LangUtils.GetTextValue("DeathText.10"),
             LangUtils.GetTextValue("DeathText.11"),
             LangUtils.GetTextValue("DeathText.12"),
             LangUtils.GetTextValue("DeathText.13"),
             LangUtils.GetTextValue("DeathText.14"),
             LangUtils.GetTextValue("DeathText.15"),
             LangUtils.GetTextValue("DeathText.16"),
             LangUtils.GetTextValue("DeathText.PermaPot2"),
             LangUtils.GetTextValue("DeathText.17"),
             LangUtils.GetTextValue("DeathText.18"),
             LangUtils.GetTextValue("DeathText.19"),
             LangUtils.GetTextValue("DeathText.20"),
             LangUtils.GetTextValue("DeathText.21"),
             LangUtils.GetTextValue("DeathText.22"),
             LangUtils.GetTextValue("DeathText.23"),
             LangUtils.GetTextValue("DeathText.24"),
             LangUtils.GetTextValue("DeathText.25"),
             LangUtils.GetTextValue("DeathText.26"),
             LangUtils.GetTextValue("DeathText.27"),
            };

            //Allow outside sources to override the text
            if(DeathTextOverride != "")
            {
                text = DeathTextOverride;
                DeathTextOverride = "";
                return LangUtils.GetTextValue("DeathText.Tip") + text;
            }

            //The final one, only ever displays once
            if(currentDeathTextIndex == DeathTextList.Count)
            {
                currentDeathTextIndex++;
                return LangUtils.GetTextValue("DeathText.Tip") + LangUtils.GetTextValue("DeathText.GoodLuck");
            }

            if(currentDeathTextIndex < DeathTextList.Count)
            {
                text = DeathTextList[currentDeathTextIndex];
            }
            else
            {
                text = DeathTextList[Main.rand.Next(DeathTextList.Count)];
            }
            bool deathTextOverridden = false;

            if (BearerOfTheCurse && Main.rand.NextBool(10))
            {
                if (Main.rand.NextBool(3))
                {
                    text = LangUtils.GetTextValue("DeathText.BotC1");
                    deathTextOverridden = true;
                }
                else if(Main.rand.NextBool(3))
                {
                    text = LangUtils.GetTextValue("DeathText.BotC2");
                    deathTextOverridden = true;
                }
                else
                {
                    text = LangUtils.GetTextValue("DeathText.BotC3");
                    deathTextOverridden = true;
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough)
            {
                if (Main.rand.NextBool(10))
                {
                    text = LangUtils.GetTextValue("DeathText.HeavyRoll");
                    deathTextOverridden = true;
                }
            }

            if (Player.statLifeMax >= 200)
            {
                if (Main.rand.NextBool(20))
                {
                    text = LangUtils.GetTextValue("DeathText.IWantToRemoveThisCaptainObviousShit");
                    deathTextOverridden = true;
                }
                if (Main.rand.NextBool(100))
                {
                    text = LangUtils.GetTextValue("DeathText.TotallyNotMeantToBeToxic");
                    deathTextOverridden = true;
                }
            }

            int currentBoss = -1;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].boss)
                {
                    currentBoss = i;
                    break;
                }
            }

            if (currentBoss >= 0)
            {
                if (Main.rand.NextBool())
                {
                    text = LangUtils.GetTextValue("DeathText.BossDeath1");
                    deathTextOverridden = true;
                }
                else if (Main.rand.NextBool() && (Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>() ||
                    Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.SpazmatismV2>() || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.Cataluminance>()
                    || Main.npc[currentBoss].type == NPCID.Plantera || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.Death>()
                    || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()
                    || Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>()))
                {
                    text = LangUtils.GetTextValue("DeathText.BossDeath2");
                    deathTextOverridden = true;
                }

                if (Main.npc[currentBoss].type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>())
                {
                    text = LangUtils.GetTextValue("DeathText.DarkCloud");
                    deathTextOverridden = true;
                }

                //If you want to add custom text for other bosses, stick it here using the line above as a template
            }

            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Cataluminance>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>()) || NPC.AnyNPCs(NPCID.TheDestroyer))
            {
                //Don't tell them rolling is mandatory if they died trying (and failing) to roll out of the way lol
                if (Main.rand.NextBool() && !wasJustRolling && !tsorcRevamp.DodgerollKey.Current)
                {
                    text = LangUtils.GetTextValue("DeathText.Marilith");
                    deathTextOverridden = true;
                }
                else if(Main.rand.NextBool())
                {
                    bool hasRing = false;
                    for (int j = 3; j < 8 + Player.GetAmountOfExtraAccessorySlotsToShow(); j++)
                    {
                        if (Player.armor[j].type == ModContent.ItemType<Items.Accessories.Mobility.ChloranthyRing>() || Player.armor[j].type == ModContent.ItemType<Items.Accessories.Mobility.ChloranthyRing2>())
                        {
                            hasRing = true;
                            break;
                        }
                    }
                    if (!hasRing)
                    {
                        text = LangUtils.GetTextValue("DeathText.ChloranthyRingTip");
                        deathTextOverridden = true;
                    }
                }
            }

            if (!deathTextOverridden)
            {
                currentDeathTextIndex++;
            }

            return LangUtils.GetTextValue("DeathText.Tip") + text;
        }
        internal static bool IsSeathWingFallImmune(Player player)
        {
            return player.controlDown
                && player.equippedWings != null
                && player.equippedWings.type == ModContent.ItemType<Items.Accessories.Mobility.Wings.WingsOfSeath>();
        }

        internal static bool IsWingFallProtected(Player player)
        {
            bool activelyGliding = player.controlJump
                && player.velocity.Y > 0f
                && player.wingsLogic > 0;
            bool activelyHovering = player.controlJump
                && player.TryingToHoverDown
                && player.wingTime > 0f
                && player.wingsLogic > 0
                && !player.merman;

            return player.GetModPlayer<tsorcRevampPlayer>().SlowfallWingActive
                || activelyGliding
                || activelyHovering
                || IsSeathWingFallImmune(player);
        }

        internal static int GetPredictedFallDamage(Player player)
        {
            if (player.noFallDmg || player.velocity.Y * player.gravDir <= 0f)
            {
                return 0;
            }

            int safeFallDistance = 25 + player.extraFall;
            int fallDistance = ((int)(player.position.Y / 16f) - player.fallStart) * (int)player.gravDir;
            int rawDamage = Math.Max(0, fallDistance - safeFallDistance) * 10;
            return player.equippedWings != null ? rawDamage / 2 : rawDamage;
        }

        public override void PreUpdateMovement()
        {
            // This is the last tModLoader player hook before vanilla resolves collision and fall damage.
            // Wings normally set noFallDmg for their entire equipped duration; retain it only while the
            // player actively glides or hovers, or has the Wings of Seath slow-fall toggle enabled.
            Player.noFallDmg = IsWingFallProtected(Player);

            // Gravity Alignment can reverse gravity while the player already has a vertical velocity.
            // That makes the previous ascent look like a new descent to vanilla's fall-distance counter
            // unless the baseline is re-armed at the exact direction change.
            bool gravityDirectionChanged = lastWingFallGravityDirection != Player.gravDir;
            lastWingFallGravityDirection = Player.gravDir;

            // Winged players can fall 15 additional tiles before the intentional wing fall-damage
            // penalty begins. This stacks with vanilla accessory bonuses such as Frog Leg.
            if (Player.equippedWings != null)
            {
                Player.extraFall += 15;

                // The wing penalty must measure only the current unsoftened descent. Vanilla wings
                // never needed this because they always negated fall damage, but after gliding,
                // hovering, or ascending the old fallStart could otherwise be carried into a later fall.
                bool wingFlightInputActive = Player.controlJump
                    && Player.wingsLogic > 0
                    && Player.wingTime > 0f;
                if (gravityDirectionChanged || Player.velocity.Y * Player.gravDir <= 0f || wingFlightInputActive || IsWingFallProtected(Player))
                {
                    Player.fallStart = (int)(Player.position.Y / 16f);
                    Player.fallStart2 = Player.fallStart;
                }
            }

            if (ImpaleFreezeTimer > 0)
            {
                Player.velocity = Vector2.Zero;
                Player.Center = ImpaleWorldPosition;
                ImpaleFreezeTimer--;
                return;
            }
            if (ShunpoTimer == 3)
            {
                Player.velocity = Vector2.Zero;
            }
            if (ShunpoTimer == 2)
            {
                Player.velocity = Player.GetModPlayer<tsorcRevampPlayer>().ShunpoVelocity;
            }
            if (ShunpoTimer == 1)
            {
                Player.velocity = Vector2.Zero;
                Player.RefreshMovementAbilities();
            }
            if (SweepingBladeTimer > 0)
            {
                Player.velocity = Player.GetModPlayer<tsorcRevampPlayer>().SweepingBladeVelocity;
                SweepingBladeTimer--;
                Player.RefreshMovementAbilities();
            }
            if (SpiritRushTimer > 0f)
            {
                Player.velocity = SpiritRushVelocity;
                Player.RefreshMovementAbilities();
            }
        }
        public override void UpdateDead()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().SoulsDropOnDeath)
                {
                    if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>() && Main.mouseItem.stack > 0)
                    {
                        SoulSlot.Item.stack += Main.mouseItem.stack;
                        Player.inventory[58].TurnToAir();
                        Main.mouseItem.TurnToAir();
                    }
                    int soulCount = 0;
                    foreach (Item item in Player.inventory)
                    {
                        //leaving this in case someone decides to move souls to their normal inventory to stop them from being dropped on death :)
                        if (item.type == ModContent.ItemType<DarkSoul>())
                        {
                            soulCount += item.stack;
                            item.stack = 0;
                        }
                    }

                    if (SoulSlot.Item.stack > 0)
                    {
                        soulCount += SoulSlot.Item.stack;
                        SoulSlot.Item.TurnToAir();
                    }

                    if (soulCount > 0)
                    {
                        Projectile.NewProjectileDirect(Player.GetSource_Death(), Player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.SoulDrop>(), 0, 0, Player.whoAmI, soulCount, Player.whoAmI);
                    }
                }


                DarkInferno = false;
                AbyssInferno = false;
                MorgulPoisoning = false;
                PhazonCorruption = false;
                Falling = false;
                FracturingArmor = 1;
                tsorcRevampEstusPlayer estusPlayer = Player.GetModPlayer<tsorcRevampEstusPlayer>();
                estusPlayer.isDrinking = false;
                estusPlayer.estusDrinkTimer = 0;
                var ceruleanPlayer = Player.GetModPlayer<CeruleanFlaskPlayer>();
                ceruleanPlayer.IsDrinking = false;
                ceruleanPlayer.CeruleanDrinkTimer = 0;
            }

            if (Player.respawnTimer > 240 && !tsorcRevampWorld.BossAlive && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Special.AbyssCataclysm>()))
            {
                Player.respawnTimer = 240;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            // Deliberately NOT in PostUpdateEquips: a Celestriad (or other max-mana source) sitting in the
            // Active Shields 2nd slot adds to MaxManaAmplifier from tsorcRevampActiveShieldPlayer.PostUpdateEquips,
            // a *different* ModPlayer's copy of the same hook. Hook order between two ModPlayer classes for the
            // same hook name isn't guaranteed, so consuming the amplifier here (PostUpdateMiscEffects, which
            // vanilla always runs after every mod's PostUpdateEquips has finished) makes this order-independent.
            if (MaxManaAmplifier > 0f)
            {
                Player.statManaMax2 = (int)(Player.statManaMax2 * (1f + MaxManaAmplifier / 100f));
            }
            if (CelestialCloak)
            {
                Player.thorns += 0.1f + (Player.statManaMax2 / 50f);
            }

            AbyssTransitionEffects();
            ReduceGraveyardDesaturation();
            SpawnAbyssMist();

            if (GravityField)
            {
                if (InSpace(Player))
                {
                    Player.gravity = Player.defaultGravity;
                    if (Player.wet)
                    {
                        if (Player.honeyWet)
                        {
                            Player.gravity = 0.1f;
                        }
                        else if (Player.merman)
                        {
                            Player.gravity = 0.3f;
                        }
                        else
                        {
                            Player.gravity = 0.2f;
                        }
                    }
                }
            }
            // spawnRate means how much enemy can spawn every tick, 1 second equals 60 ticks
            spawnRateFieldInfo = typeof(NPC).GetField("spawnRate", BindingFlags.Static | BindingFlags.NonPublic);
            spawnRate = (int)spawnRateFieldInfo.GetValue(null);

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)//could also add condition like !Player.wet, Player.honeyWet
            {
                Tile tile = Main.tile[(int)(Player.Center.X / 16f), (int)(Player.Center.Y / 16f)];
                if (tile.WallType == WallID.StarRoyaleBrickWall)//whatever special walls using in the adventure map
                {
                    Player.gravity = 0f;// no gravity for sure or tiny gravity?
                    Player.velocity.Y *= 0.9f;// >1f could make player move faster and faster, or lower make the player only rise for certain time which limit the height player moved
                    Player.velocity.Y -= 0.15f;
                }
            }

            if (Main.tile[(Player.Center / 16).ToPoint()].WallType == WallID.PinkDungeonTileUnsafe && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.SuperHardMode)
            {
                Player.AddBuff(BuffID.WitheredWeapon, 5*60);
                Player.AddBuff(BuffID.Blackout, 5*60);
            }

            if (Main.LocalPlayer.ZoneGlowshroom && !Main.LocalPlayer.ZoneDungeon && Main.LocalPlayer.ZoneRockLayerHeight && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.SuperHardMode)
            {
                Player.AddBuff(BuffID.WaterCandle, 4*60);
                Player.AddBuff(BuffID.MoonLeech, 4*60);
            }

            if (tsorcRevampWorld.RemixMap)
            {
                if (!Main.LocalPlayer.ZoneHallow && Main.tile[(Player.Center / 16).ToPoint()].WallType == WallID.HallowUnsafe2 && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && Main.hardMode)
                {
                    Player.AddBuff(BuffID.WaterCandle, 8*60);
                }

                if (Main.LocalPlayer.ZoneCorrupt && Main.LocalPlayer.ZoneUnderworldHeight && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.SuperHardMode)
                {
                    bool BossIsAlive = false;

                    // if witchking or ancient abyss demon is alive
                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active && 
                            (npc.type == ModContent.NPCType<Witchking>() || npc.type == ModContent.NPCType<AncientDemonOfTheAbyss>())) 
                        {
                            BossIsAlive = true;
                            break;
                        }
                    }

                    // Applies the effects only if witchking or ancient abyss demon isn't here
                    if (!BossIsAlive)
                    {
                        Player.AddBuff(BuffID.WitheredArmor, 3*60);
                        Player.AddBuff(ModContent.BuffType<BrokenSpirit>(), 3*60, false);
                        Player.AddBuff(BuffID.Darkness, 3*60);
                    }
                }

                if (Main.LocalPlayer.ZoneMeteor && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && (Main.LocalPlayer.ZoneSkyHeight || Main.LocalPlayer.ZoneOverworldHeight) && Main.LocalPlayer.ZoneCorrupt && tsorcRevampWorld.SuperHardMode)
                {
                    bool WyvernShadowIsAlive = false;

                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active &&
                            (npc.type == ModContent.NPCType<GhostDragonHead>() || npc.type == ModContent.NPCType<WyvernMageShadow>()))
                        {
                            WyvernShadowIsAlive = true;
                            break;
                        }
                    }

                    if (!WyvernShadowIsAlive)
                    {
                        Player.AddBuff(BuffID.WitheredWeapon, 2 * 60);
                        Player.AddBuff(BuffID.Battle, 2 * 60);
                        Player.AddBuff(ModContent.BuffType<WeightOfShadow>(), 2 * 60, false);
                    }
                }
                
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())) &&
                Main.LocalPlayer.ZoneHallow && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && (Main.LocalPlayer.ZoneDirtLayerHeight || Main.LocalPlayer.ZoneRockLayerHeight) 
                && tsorcRevampWorld.SuperHardMode)
                {
                    bool ChaosIsAlive = false;

                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active && 
                            (npc.type == ModContent.NPCType<Chaos>())) 
                        {
                            ChaosIsAlive = true;
                            break;
                        }
                    }

                    if (!ChaosIsAlive)
                    {
                        Player.AddBuff(BuffID.WitheredWeapon, 1*60);
                        Player.AddBuff(BuffID.WaterCandle, 1*60);
                        Player.AddBuff(BuffID.Blackout, 1*30);
                        Player.AddBuff(ModContent.BuffType<BrokenSpirit>(), 1*60, false);
                    }
                }

                if (Main.LocalPlayer.ZoneJungle && !Main.LocalPlayer.ZoneDungeon && !Main.LocalPlayer.ZoneOverworldHeight && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.SuperHardMode)
                {
                    Player.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 5*60, false);
                    Player.AddBuff(BuffID.Weak, 5*30);
                }

                if (Main.LocalPlayer.ZoneSnow && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && Main.LocalPlayer.ZoneDungeon && tsorcRevampWorld.SuperHardMode)
                {
                    bool SeathIsAlive = false;

                    foreach (NPC npc in Main.npc)
                    {
                        if (npc.active && npc.type == ModContent.NPCType<SeathTheScalelessHead>()) 
                        {
                            SeathIsAlive = true;
                            break;
                        }
                    }

                    if (!SeathIsAlive)
                    {
                        Player.AddBuff(BuffID.WitheredArmor, 5*60);
                        Player.AddBuff(ModContent.BuffType<BrokenSpirit>(), 5*60, false);
                        Player.AddBuff(BuffID.Chilled, 5*30);
                    }
                }
            }

            if (HasLoveRing)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player ally = Main.player[i];

                    if (ally.whoAmI == Player.whoAmI)
                        continue;

                    if (!ally.active || ally.dead)
                        continue;

                    if (!(Player.team == 0 || ally.team == Player.team))
                        continue;

                    if (Vector2.Distance(Player.Center, ally.Center) < 1000f)
                    {
                        ally.AddBuff(ModContent.BuffType<EverlastingLoveBuff>(), 3);
                    }
                }
            }
        }

        // How much max HP one Life Crystal is worth in SoulsMode, scaled by party size. Read once at the moment
        // the crystal is eaten (see GrantLifeCrystal) and banked permanently into soulsLifeGranted, so a
        // crystal is worth what it was worth when you swallowed it — players joining or leaving later can't
        // retroactively rewrite your max HP. Mana has no party scaling; it is a flat SoulsModeManaCrystalGain.
        private int GetSoulsModeLifeCrystalGain()
        {
            int activePlayers = 0;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active) activePlayers++;
            }

            if (activePlayers >= 4) return 20;
            if (activePlayers >= 2) return 15;
            return 10;
        }

        // Purple dust burst + a portal-ish whoosh on the exact tick EnterTheAbyss flips, so stepping into (or
        // out of) the Abyss reads as a reality shift rather than just a debuff icon appearing. Entering uses a
        // darker tint and a lower pitch (sinking into somewhere heavier); exiting is lighter/brighter-pitched
        // (surfacing back to normal reality). Reuses vanilla's Etherian portal sound rather than new audio.
        private void AbyssTransitionEffects()
        {
            if (EnterTheAbyss == WasInAbyss)
            {
                return;
            }

            bool entering = EnterTheAbyss;
            WasInAbyss = EnterTheAbyss;

            if (Main.dedServ)
            {
                return;
            }

            Color tint = entering ? new Color(70, 20, 110) : new Color(205, 165, 235);

            for (int i = 0; i < 45; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(7f, 7f);
                int dust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.PurpleTorch, velocity.X, velocity.Y, 100, tint, Main.rand.NextFloat(1.5f, 2.3f));
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                int dust = Dust.NewDust(Player.position, Player.width, Player.height, DustID.Shadowflame, velocity.X, velocity.Y, 100, default, 1.6f);
                Main.dust[dust].noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen with { Volume = 0.6f, Pitch = entering ? -0.25f : 0.25f }, Player.Center);

            UsefulFunctions.ScreenShake(Player.Center, uniqueIdentity: "AbyssTransition");
        }

        // Vanilla's "Graveyard" scene filter (Player.UpdateBiomes, runs earlier in the tick) desaturates the
        // screen while GraveyardVisualIntensity > 0, using UseProgress(0..0.75) and a flat UseIntensity(1.2).
        // We can't tune vanilla's own call, so we just overwrite both with weaker values right after it runs.
        // Set GraveyardDesaturationMultiplier to 0f for full removal instead of a reduction.
        private const float GraveyardDesaturationMultiplier = 0.4f;

        private void ReduceGraveyardDesaturation()
        {
            if (!Filters.Scene["Graveyard"].IsActive())
            {
                return;
            }

            if (GraveyardDesaturationMultiplier <= 0f)
            {
                Filters.Scene.Deactivate("Graveyard");
                return;
            }

            float progress = MathHelper.Lerp(0f, 0.75f, Main.GraveyardVisualIntensity) * GraveyardDesaturationMultiplier;
            Filters.Scene["Graveyard"].GetShader().UseProgress(progress).UseIntensity(1.2f * GraveyardDesaturationMultiplier);
        }

        // Ambient ground mist for the Abyss, using real vanilla Dust instead of a custom-drawn sprite/cloud
        // layer (three earlier attempts at that all hit stability or rendering bugs - see git history on
        // MethodSwaps.cs). Dust is simplest here specifically because we don't have to draw, position, fade, or
        // clean it up ourselves - Terraria's own dust system already handles all of that once spawned, the same
        // way Rain (Terraria/Rain.cs) doesn't re-derive anything about the world each frame, it just spawns a
        // particle with its own position/velocity and lets it live its own life.
        //
        // Style (dust type 77/Ebonwood, alpha 180, scale 1.25) matches ArtoriasArmor.cs's own UpdateArmorSet
        // aura dust exactly - that one reads as subtle, ours (Cloud/alpha 80/bigger scale) was too heavy.
        //
        // Spawn positions come from an actual surface scan (first tile with open air above a solid tile) within
        // a fixed 20-tile radius of the player - but unlike the earlier tile-scanning attempts, this list is
        // only rebuilt periodically (every AbyssMistRefreshInterval ticks), not every frame, and it's anchored
        // to the player's position rather than the viewport. That's what avoids the flicker: a per-frame scan
        // tied to the camera changes its answer on every tiny movement, but a radius tied to the player's own
        // position barely changes tick to tick, and only actually re-evaluates on a slow timer regardless.
        private const int AbyssMistDustType = DustID.Ebonwood;
        private static readonly Color AbyssMistTint = new Color(150, 110, 210);
        private const int AbyssMistScanRadius = 20; // tiles
        private const int AbyssMistRefreshInterval = 60; // ticks between rescans (1 second)

        private List<Point> abyssMistSurfaceTiles = new List<Point>();
        private int abyssMistRefreshTimer;

        private void SpawnAbyssMist()
        {
            if ((!EnterTheAbyss && !MethodSwaps.IsAbyssVisualActive()) || Main.dedServ)
            {
                return;
            }

            if (abyssMistRefreshTimer <= 0)
            {
                abyssMistRefreshTimer = AbyssMistRefreshInterval;
                RefreshAbyssMistSurfaceTiles();
            }
            else
            {
                abyssMistRefreshTimer--;
            }

            if (abyssMistSurfaceTiles.Count == 0 || !Main.rand.NextBool(2))
            {
                return;
            }

            Point tile = abyssMistSurfaceTiles[Main.rand.Next(abyssMistSurfaceTiles.Count)];
            Vector2 spawnPos = new Vector2(tile.X * 16f - 5f, tile.Y * 16f);
            int dustIndex = Dust.NewDust(spawnPos, 26, 16, AbyssMistDustType, 0f, -2f, 180, AbyssMistTint, 1.25f);
            Main.dust[dustIndex].noGravity = true;
        }

        private void RefreshAbyssMistSurfaceTiles()
        {
            abyssMistSurfaceTiles.Clear();

            int playerTileX = (int)(Player.Center.X / 16f);
            int playerTileY = (int)(Player.Center.Y / 16f);

            for (int x = Math.Max(1, playerTileX - AbyssMistScanRadius); x <= Math.Min(Main.maxTilesX - 2, playerTileX + AbyssMistScanRadius); x++)
            {
                for (int y = Math.Max(1, playerTileY - AbyssMistScanRadius); y <= Math.Min(Main.maxTilesY - 2, playerTileY + AbyssMistScanRadius); y++)
                {
                    Tile tile = Main.tile[x, y];
                    Tile above = Main.tile[x, y - 1];
                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !above.HasTile)
                    {
                        abyssMistSurfaceTiles.Add(new Point(x, y - 1));
                    }
                }
            }
        }

        public override void PostUpdate()
        {
            // Update the red hurt vignette (local player only, handled inside).
            UpdateHurtVignette();

            if ((Player.HasBuff(ModContent.BuffType<MagicWeapon>()) || Player.HasBuff(ModContent.BuffType<GreatMagicWeapon>()) || Player.HasBuff(ModContent.BuffType<CrystalMagicWeapon>())) && Player.meleeEnchant > 0)
            {
                int buffIndex = 0;

                foreach (int buffType in Player.buffType)
                {

                    if ((buffType == ModContent.BuffType<MagicWeapon>()) || (buffType == ModContent.BuffType<GreatMagicWeapon>()) || (buffType == ModContent.BuffType<CrystalMagicWeapon>()))
                    {
                        Player.buffTime[buffIndex] = 0;
                    }
                    buffIndex++;
                }
            }
            SetDirection();

            if (!HasYoungHunterAccessory)
            {
                Player.ClearBuff(ModContent.BuffType<YoungHunterBuff>());

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile summon = Main.projectile[i];
                    if (summon.active && summon.owner == Player.whoAmI && summon.type == ModContent.ProjectileType<YoungHunter>())
                    {
                        summon.Kill();
                    }
                }
            }

            if (loveHealCooldown > 0)
            loveHealCooldown--;

            if (!Player.mount.Active)
            {
                Player.fullRotation = rotation * Player.gravDir;
            }

            rotation = 0f;
            if (forcedItemRotation.HasValue && !CanUseItemsWhileDodging)
            {
                Player.itemRotation = forcedItemRotation.Value;

                forcedItemRotation = null;
            }          

            TryForceFrame(ref Player.headFrame, ref forcedHeadFrame);
            TryForceFrame(ref Player.bodyFrame, ref forcedBodyFrame);
            TryForceFrame(ref Player.legFrame, ref forcedLegFrame);

            if (CurseLevel > 0)
            {
                curseDecayTimer++;

                if (curseDecayTimer >= 60)
                {
                    CurseLevel--;
                    curseDecayTimer = 0;
                }
            }

            if (PowerfulCurseLevel > 0)
            {
                powerfulCurseDecayTimer++;

                if (powerfulCurseDecayTimer >= 30)
                {
                    PowerfulCurseLevel--;
                    powerfulCurseDecayTimer = 0;
                }
            }
            //shift everything in the array forward one slot, starting from the end
            for (int i = oldPos.Length - 1; i > 0; i--)
            {
                oldPos[i] = oldPos[i - 1];
            }
            //except the first slot
            oldPos[0] = Player.position;
            //Main.NewText("" + Player.lifeRegen);

            if (Player.ZoneGraveyard) { Player.AddBuff(BuffID.WaterCandle, 2); }

            // Dungeon during Super Hard Mode: keep Suppressed topped up at a short fixed duration (rather than
            // silently re-adding every tick) so enemy attacks can also inflict it and have that duration matter.
            // A lit bonfire is a safe rest point and must not immediately reapply the debuff after buff updates.
            if (Player.ZoneDungeon && tsorcRevampWorld.SuperHardMode
                && !Player.HasBuff(ModContent.BuffType<Bonfire>()))
            {
                Player.AddBuff(ModContent.BuffType<Suppressed>(), 60);
            }

            if (gilled && Main.tile[(int)Player.Top.X / 16, ((int)Player.Top.Y + 10) / 16].LiquidAmount != 0 && Main.tile[(int)Player.Top.X / 16, ((int)Player.Top.Y + 10) / 16].LiquidType == LiquidID.Water)
            {
                if (Player.breath < Player.breathMax)
                {
                    Player.breath += 4;
                }
            }

            if (UsefulFunctions.PlayerInZone(Player, 140,2690, 320,820) || UsefulFunctions.PlayerInZone(Player, 430,2690, 50,320))
            {
                //Main.NewText("Desert");
                Player.ZoneDesert = true;
            }
            else if (UsefulFunctions.PlayerInZone(Player, 5359,6120, 50,800) || UsefulFunctions.PlayerInZone(Player, 6120,6300, 400,800) || UsefulFunctions.PlayerInZone(Player, 5726,5937, 1656,1803))
            {
                //Main.NewText("Jungle");
                Player.ZoneJungle = true;
            }
            else if (UsefulFunctions.PlayerInZone(Player, 3330,3580, 120,350) || UsefulFunctions.PlayerInZone(Player, 6850,8360, 50,540) || UsefulFunctions.PlayerInZone(Player, 7430,8360, 540,840))
            {
                //Main.NewText("Snow");
                Player.ZoneSnow = true;
            }
                
            if (tsorcRevampWorld.RemixMap)
            {
                if (UsefulFunctions.PlayerInZone(Player, 2630,3100, 70,320) || UsefulFunctions.PlayerInZone(Player, 2630,3010, 320,550))
                {
                    //Main.NewText("Crimson");
                    Player.ZoneCrimson = true;
                }
                else if (UsefulFunctions.PlayerInZone(Player, 6460,6640, 460,850))
                {
                    //Main.NewText("Hallow");
                    Player.ZoneHallow = true;
                }
            }

            if (Player.GetModPlayer<tsorcRevampPlayer>().EnterTheAbyss)
            {
                // TODO: Slightly reduce the volume and pitch of the music when the player is in the abyss
            }
        }

        void TryForceFrame(ref Rectangle frame, ref PlayerFrames? newFrame)
        {
            if (newFrame.HasValue)
            {
                frame = ToRectangle(newFrame.Value);

                newFrame = null;
            }
        }
        public static Rectangle ToRectangle(PlayerFrames frame)
        {
            return new Rectangle(0, (int)frame * 56, 40, 56);
        }

        //taken straight from player.update, dont ask why it does what it does because i have NO idea
        public static bool InSpace(Player player)
        {
            float x = (float)Main.maxTilesX / 4200f;
            x *= x;
            return (float)((double)(player.position.Y / 16f - (60f + 10f * x)) / (Main.worldSurface / 6.0)) < 1f;
        }

        public bool OverrideCamera;
        public Vector2 newScreenPosition;
        public float progress;
        public override void ModifyScreenPosition()
        {
            if (OverrideCamera)
            {
                Main.screenPosition = newScreenPosition = Vector2.Lerp(Main.screenPosition, newScreenPosition - Main.ScreenSize.ToVector2() / 2f, progress);
                OverrideCamera = false;
                progress = 0;
            }
        }

        ///<summary> 
        ///The total of stats removed by a powerful Curse. A % this value is added to positive stats. Life Regeneration, Defense, Attack and Movement Speed have extra multipliers, to balance their effects
        ///</summary>       
        const float powerfulCurseBonus = 60f;

        ///<summary> 
        ///The total of stats removed by a weak Curse. A % of this value is added to positive stats. Life Regeneration, Defense, Attack and Movement Speed have extra multipliers, to balance their effects
        ///</summary>  
        const float weakCurseBonus = 30f;

        ///<summary> 
        ///The base percentage of positive stats granted to the player by a curse. Used for resetting the active percentage back to normal. 
        ///</summary>  
        public const float BaseCursePositiveStatPercentage = 0.67f;

        ///<summary> 
        ///Multiplies the life regeneration taken/given by curses
        ///</summary>  
        const float CurseLifeRegenerationMultiplier = 0.5f;

        ///<summary> 
        ///Multiplies the defense taken/given by curses
        ///</summary>  
        const float CurseDefenseMultiplier = 1.5f;

        ///<summary> 
        ///Multiplies the attack speed taken/given by curses
        ///</summary>  
        const float CurseAttackSpeedMultiplier = 0.8f;

        ///<summary> 
        ///Multiplies the movement speed taken/given by curses
        ///</summary>  
        const float CurseMovementSpeedMultiplier = 1.5f;


        ///<summary> 
        ///Randomizes a Curse's stats. 
        ///</summary>         
        ///<param name="isCursePowerful">Decides whether it's rolling for the weak or powerful Curse variant</param>
        public void CalculateCurseStats(in bool isCursePowerful)
        {
            bool pickedMaxHealth = false;
            bool pickedLifeRegeneration = false;
            bool pickedDefense = false;
            bool pickedResistance = false;
            bool pickedDamage = false;
            bool pickedAttackSpeed = false;
            bool pickedMovementSpeed = false;

            int pickedNegativeStats = 0;
            int NegativeStatsAmount = Main.rand.Next(isCursePowerful ? 4 : 3) + 1;
            pickedMaxHealth = true;
            pickedNegativeStats++;
            if (isCursePowerful)
            {
                powerfulCurseMaxLifeMultiplier = -powerfulCurseBonus / (float)NegativeStatsAmount;
            }
            else
            {
                CurseMaxLifeMultiplier = -weakCurseBonus / (float)NegativeStatsAmount;
            }

            //calculating negative stats
            while (pickedNegativeStats < NegativeStatsAmount)
            {
                if (Main.rand.NextBool(7) && !pickedMaxHealth)
                {
                    pickedMaxHealth = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseMaxLifeMultiplier = -powerfulCurseBonus / (float)NegativeStatsAmount;
                    }
                    else
                    {
                        CurseMaxLifeMultiplier = -weakCurseBonus / (float)NegativeStatsAmount;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedLifeRegeneration)
                {
                    pickedLifeRegeneration = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseLifeRegenerationBonus = (-powerfulCurseBonus / (float)NegativeStatsAmount) * CurseLifeRegenerationMultiplier;
                    }
                    else
                    {
                        CurseLifeRegenerationBonus = (-weakCurseBonus / (float)NegativeStatsAmount) * CurseLifeRegenerationMultiplier;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedDefense)
                {
                    pickedDefense = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseDefenseBonus = (-powerfulCurseBonus / (float)NegativeStatsAmount) * CurseDefenseMultiplier;
                    }
                    else
                    {
                        CurseDefenseBonus = (-weakCurseBonus / (float)NegativeStatsAmount) * CurseDefenseMultiplier;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedResistance)
                {
                    pickedResistance = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseResistanceBonus = -powerfulCurseBonus / (float)NegativeStatsAmount;
                    }
                    else
                    {
                        CurseResistanceBonus = -weakCurseBonus / (float)NegativeStatsAmount;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedDamage)
                {
                    pickedDamage = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseDamageBonus = -powerfulCurseBonus / (float)NegativeStatsAmount;
                    }
                    else
                    {
                        CurseDamageBonus = -weakCurseBonus / (float)NegativeStatsAmount;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedAttackSpeed)
                {
                    pickedAttackSpeed = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseAttackSpeedBonus = (-powerfulCurseBonus / (float)NegativeStatsAmount) * CurseAttackSpeedMultiplier;
                    }
                    else
                    {
                        CurseAttackSpeedBonus = (-weakCurseBonus / (float)NegativeStatsAmount) * CurseAttackSpeedMultiplier;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedMovementSpeed)
                {
                    pickedMovementSpeed = true;
                    pickedNegativeStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseMovementSpeedBonus = (-powerfulCurseBonus / (float)NegativeStatsAmount) * CurseMovementSpeedMultiplier;
                    }
                    else
                    {
                        CurseMovementSpeedBonus = (-weakCurseBonus / (float)NegativeStatsAmount) * CurseMovementSpeedMultiplier;
                    }
                }
            }

            int pickedPositiveStats = 0;
            int PositiveStatsAmount = Main.rand.Next(isCursePowerful ? 3 : 2) + 1;
            //calculating positive stats
            while (pickedPositiveStats < PositiveStatsAmount)
            {
                if (Main.rand.NextBool(7) && !pickedLifeRegeneration)
                {
                    pickedLifeRegeneration = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseLifeRegenerationBonus = ((powerfulCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage) * CurseLifeRegenerationMultiplier;
                    }
                    else
                    {
                        CurseLifeRegenerationBonus = ((weakCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage) * CurseLifeRegenerationMultiplier;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedDefense)
                {
                    pickedDefense = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseDefenseBonus = ((powerfulCurseBonus / (float)PositiveStatsAmount) * CurseDefenseMultiplier) * BaseCursePositiveStatPercentage;
                    }
                    else
                    {
                        CurseDefenseBonus = ((weakCurseBonus / (float)PositiveStatsAmount) * CurseDefenseMultiplier) * BaseCursePositiveStatPercentage;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedResistance)
                {
                    pickedResistance = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseResistanceBonus = (powerfulCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage;
                    }
                    else
                    {
                        CurseResistanceBonus = (weakCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedDamage)
                {
                    pickedDamage = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseDamageBonus = (powerfulCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage;
                    }
                    else
                    {
                        CurseDamageBonus = (weakCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedAttackSpeed)
                {
                    pickedAttackSpeed = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseAttackSpeedBonus = ((powerfulCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage) * CurseAttackSpeedMultiplier;
                    }
                    else
                    {
                        CurseAttackSpeedBonus = ((weakCurseBonus / (float)PositiveStatsAmount) * BaseCursePositiveStatPercentage) * CurseAttackSpeedMultiplier;
                    }
                }
                else if (Main.rand.NextBool(7) && !pickedMovementSpeed)
                {
                    pickedMovementSpeed = true;
                    pickedPositiveStats++;

                    if (isCursePowerful)
                    {
                        powerfulCurseMovementSpeedBonus = ((powerfulCurseBonus / (float)PositiveStatsAmount) * CurseMovementSpeedMultiplier) * BaseCursePositiveStatPercentage;
                    }
                    else
                    {
                        CurseMovementSpeedBonus = ((weakCurseBonus / (float)PositiveStatsAmount) * CurseMovementSpeedMultiplier) * BaseCursePositiveStatPercentage;
                    }
                }
            }

            //resetting stats that haven't been picked to neutral state
            if (!pickedMaxHealth)
            {
                if (isCursePowerful)
                {
                    powerfulCurseMaxLifeMultiplier = 0;
                }
                else
                {
                    CurseMaxLifeMultiplier = 0;
                }
            }
            if (!pickedLifeRegeneration)
            {
                if (isCursePowerful)
                {
                    powerfulCurseLifeRegenerationBonus = 0;
                }
                else
                {
                    CurseLifeRegenerationBonus = 0;
                }
            }
            if (!pickedDefense)
            {
                if (isCursePowerful)
                {
                    powerfulCurseDefenseBonus = 0;
                }
                else
                {
                    CurseDefenseBonus = 0;
                }
            }
            if (!pickedResistance)
            {
                if (isCursePowerful)
                {
                    powerfulCurseResistanceBonus = 0;
                }
                else
                {
                    CurseResistanceBonus = 0;
                }
            }
            if (!pickedDamage)
            {
                if (isCursePowerful)
                {
                    powerfulCurseDamageBonus = 0;
                }
                else
                {
                    CurseDamageBonus = 0;
                }
            }
            if (!pickedAttackSpeed)
            {
                if (isCursePowerful)
                {
                    powerfulCurseAttackSpeedBonus = 0;
                }
                else
                {
                    CurseAttackSpeedBonus = 0;
                }
            }
            if (!pickedMovementSpeed)
            {
                if (isCursePowerful)
                {
                    powerfulCurseMovementSpeedBonus = 0;
                }
                else
                {
                    CurseMovementSpeedBonus = 0;
                }
            }
        }
        public void AddCurseStats()
        {
            if (CurseActive)
            {
                Player.AddBuff(ModContent.BuffType<Curse>(), 2);
            }
            else
            {
                CurseMaxLifeMultiplier = 0;
                CurseLifeRegenerationBonus = 0;
                CurseDefenseBonus = 0;
                CurseResistanceBonus = 0;
                CurseDamageBonus = 0;
                CurseAttackSpeedBonus = 0;
                CurseMovementSpeedBonus = 0;
            }
            if (powerfulCurseActive)
            {
                Player.AddBuff(ModContent.BuffType<PowerfulCurse>(), 2);
            }
            else
            {
                powerfulCurseMaxLifeMultiplier = 0;
                powerfulCurseLifeRegenerationBonus = 0;
                powerfulCurseDefenseBonus = 0;
                powerfulCurseResistanceBonus = 0;
                powerfulCurseDamageBonus = 0;
                powerfulCurseAttackSpeedBonus = 0;
                powerfulCurseMovementSpeedBonus = 0;
            }
            float curseMaxLifeMultiplier = Math.Min(CurseMaxLifeMultiplier, 0f);
            float powerfulCurseMaxLifeMultiplierToApply = Math.Min(powerfulCurseMaxLifeMultiplier, 0f);
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + (curseMaxLifeMultiplier / 100f) + (powerfulCurseMaxLifeMultiplierToApply / 100f)));
            Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2);
            //life regen is in updateregen functions
            Player.statDefense += (int)MathF.Round((CurseDefenseBonus * ((CurseDefenseBonus > 0) ? CursePositiveStatsMultiplier : 1f)) + (powerfulCurseDefenseBonus * ((powerfulCurseDefenseBonus > 0) ? CursePositiveStatsMultiplier : 1f)));
            Player.endurance += ((CurseResistanceBonus * ((CurseResistanceBonus > 0) ? CursePositiveStatsMultiplier : 1f)) + (powerfulCurseResistanceBonus * ((powerfulCurseResistanceBonus > 0) ? CursePositiveStatsMultiplier : 1f))) / 100f;
            Player.GetDamage(DamageClass.Generic) += ((CurseDamageBonus * ((CurseDamageBonus > 0) ? CursePositiveStatsMultiplier : 1f)) + (powerfulCurseDamageBonus * ((powerfulCurseDamageBonus > 0) ? CursePositiveStatsMultiplier : 1f))) / 100f;
            Player.GetAttackSpeed(DamageClass.Generic) += ((CurseAttackSpeedBonus * ((CurseAttackSpeedBonus > 0) ? CursePositiveStatsMultiplier : 1f)) + (powerfulCurseAttackSpeedBonus * ((powerfulCurseAttackSpeedBonus > 0) ? CursePositiveStatsMultiplier : 1f))) / 100f;
            Player.moveSpeed += ((CurseMovementSpeedBonus * ((CurseMovementSpeedBonus > 0) ? CursePositiveStatsMultiplier : 1f)) + (powerfulCurseMovementSpeedBonus * ((powerfulCurseMovementSpeedBonus > 0) ? CursePositiveStatsMultiplier : 1f))) / 100f;

            // Destined Death debuff logic
            if (DestinedDeathActive)
            {
                float targetLoss = DestinedDeathStacks * 0.10f; // 10% per stack, up to 60%
                if (DestinedDeathCurrentHPLoss < targetLoss)
                {
                    // Drain max HP by 5% per second (0.05f / 60f per tick)
                    DestinedDeathCurrentHPLoss = Math.Min(targetLoss, DestinedDeathCurrentHPLoss + (0.05f / 60f));
                }
            }
            else if (DestinedDeathCurrentHPLoss > 0f)
            {
                // Debuff expired — recover life at 10% per second (0.10f / 60f per tick)
                DestinedDeathCurrentHPLoss = Math.Max(0f, DestinedDeathCurrentHPLoss - (0.10f / 60f));
                if (DestinedDeathCurrentHPLoss <= 0f)
                {
                    DestinedDeathStacks = 0;
                }
            }

            if (DestinedDeathCurrentHPLoss > 0f)
            {
                // Apply max HP reduction
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f - DestinedDeathCurrentHPLoss));
                Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2);

                // At 60% max HP loss cap, gain 15% extra damage (additive)
                if (DestinedDeathCurrentHPLoss >= 0.60f)
                {
                    Player.GetDamage(DamageClass.Generic) += 0.15f;
                }
            }
        }
    }
}
