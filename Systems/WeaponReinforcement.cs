using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials.Ember;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Magic;
using tsorcRevamp.Content.Items.Weapons.Magic.Tomes.CausticMaw;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords.SerpentsFang;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows.SerpentsCoil;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Items.Weapons.Summon.Whips.SerpentsTailLash;
using tsorcRevamp.Content.Items.Weapons.Summon.Whips.UhlansSiegebow;
using tsorcRevamp.NPCs.Bosses.Birds;
using tsorcRevamp.NPCs.Bosses.Triad;

namespace tsorcRevamp.Systems
{
    public readonly struct ReinforcementPerk
    {
        public string LocalizationKey { get; init; }
        public object[] FormatArgs { get; init; }

        public string GetText()
        {
            if (FormatArgs != null && FormatArgs.Length > 0)
            {
                return LangUtils.GetTextValue(LocalizationKey, FormatArgs);
            }

            return LangUtils.GetTextValue(LocalizationKey);
        }
    }

    public sealed class WeaponReinforcementEntry
    {
        public byte NativeLevel { get; init; }
        public byte MaxLevel { get; init; }
        public int[] DamageByLevel { get; init; }
        public Dictionary<byte, ReinforcementPerk> MilestonePerks { get; init; }
    }

    public readonly struct ReinforcementCost
    {
        public int MaterialItemType { get; init; }
        public int MaterialCount { get; init; }
        public int DarkSouls { get; init; }
        public string MaterialName => Lang.GetItemNameValue(MaterialItemType);
    }

    public readonly struct BossShardDrop
    {
        public int ShardItemType { get; init; }
        public int Count { get; init; }
    }

    public sealed class ReinforcementFirstBagRule : FirstBagRule
    {
        public override bool CanDrop(DropAttemptInfo info)
        {
            if (!WeaponReinforcement.Enabled)
            {
                return false;
            }

            return base.CanDrop(info);
        }
    }

    public sealed class ReinforcementFirstKillRule : FirstBossKillRule
    {
        public ReinforcementFirstKillRule(bool requireBoss = false) : base(requireBoss)
        {
        }

        public override bool CanDrop(DropAttemptInfo info)
        {
            if (!WeaponReinforcement.Enabled)
            {
                return false;
            }

            return base.CanDrop(info);
        }
    }

    /// <summary>
    /// Self-contained iconic weapon reinforcement registry, boss-cap evaluator,
    /// multi-storage material/soul consumer, boss shard drop tables, and policy API.
    /// </summary>
    public class WeaponReinforcement : ModSystem
    {
        public const byte MaxPreShmLevel = 5;
        public const byte MaxShmLevel = 10;

        private static Dictionary<int, WeaponReinforcementEntry> registry;
        private static Dictionary<int, BossShardDrop> bossBagShardDrops;
        private static Dictionary<int, BossShardDrop> bossNpcShardDrops;
        private static IItemDropRuleCondition firstBagCondition;
        private static IItemDropRuleCondition firstKillCondition;
        private static byte lastAnnouncedCap = byte.MaxValue;

        public static IItemDropRuleCondition FirstBagCondition
        {
            get
            {
                if (firstBagCondition == null)
                {
                    firstBagCondition = new ReinforcementFirstBagRule();
                }

                return firstBagCondition;
            }
        }

        public static IItemDropRuleCondition FirstKillCondition
        {
            get
            {
                if (firstKillCondition == null)
                {
                    firstKillCondition = new ReinforcementFirstKillRule(requireBoss: false);
                }

                return firstKillCondition;
            }
        }

        public static bool Enabled
        {
            get
            {
                tsorcRevampGameplayConfig config = ModContent.GetInstance<tsorcRevampGameplayConfig>();
                if (config == null)
                {
                    return true;
                }

                return config.NewWeaponReinforcement;
            }
        }

        public override void PostSetupContent()
        {
            BuildRegistry();
            BuildBossShardDropRegistries();
        }

        public override void Unload()
        {
            registry = null;
            bossBagShardDrops = null;
            bossNpcShardDrops = null;
            firstBagCondition = null;
            firstKillCondition = null;
            lastAnnouncedCap = byte.MaxValue;
        }

        public override void OnWorldLoad()
        {
            lastAnnouncedCap = byte.MaxValue;

            // Always convert 2x2 Yellow/Pink Team Block markers on world load so raw Team Blocks never show on the map
            // (EmberVessel hides and disables itself whenever WeaponReinforcement.Enabled is false).
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ConvertEmberVesselMarkers();
            }
        }

        private static void ConvertEmberVesselMarkers()
        {
            ushort vesselType = (ushort)ModContent.TileType<EmberVessel>();

            for (int x = 1; x < Main.maxTilesX - 2; x++)
            {
                for (int y = 1; y < Main.maxTilesY - 2; y++)
                {
                    Tile topLeft = Main.tile[x, y];
                    if (!topLeft.HasTile)
                    {
                        continue;
                    }

                    bool isYellow = topLeft.TileType == TileID.TeamBlockYellow;
                    bool isPink = topLeft.TileType == TileID.TeamBlockPink;
                    if (!isYellow && !isPink)
                    {
                        continue;
                    }

                    ushort markerType = topLeft.TileType;
                    Tile topRight = Main.tile[x + 1, y];
                    Tile bottomLeft = Main.tile[x, y + 1];
                    Tile bottomRight = Main.tile[x + 1, y + 1];

                    if (!topRight.HasTile || topRight.TileType != markerType
                        || !bottomLeft.HasTile || bottomLeft.TileType != markerType
                        || !bottomRight.HasTile || bottomRight.TileType != markerType)
                    {
                        continue;
                    }

                    short baseFrameX = isPink ? EmberVessel.RadiantFrameOffset : EmberVessel.PaleFrameOffset;
                    for (int dx = 0; dx < 2; dx++)
                    {
                        for (int dy = 0; dy < 2; dy++)
                        {
                            Tile part = Main.tile[x + dx, y + dy];
                            part.TileType = vesselType;
                            part.TileFrameX = (short)(baseFrameX + dx * 18);
                            part.TileFrameY = (short)(dy * 18);
                            part.Slope = SlopeType.Solid;
                            part.IsHalfBlock = false;
                        }
                    }
                }
            }
        }

        public override void OnWorldUnload()
        {
            if (!Main.dedServ)
            {
                WeaponReinforcementUIState.Close(playSound: false, allowWorldDrop: false);
            }

            lastAnnouncedCap = byte.MaxValue;
        }

        public override void PreSaveAndQuit()
        {
            // Return any slotted weapon to inventory or Storage before player data serializes so quitting never drops or loses it.
            if (!Main.dedServ)
            {
                WeaponReinforcementUIState.Close(playSound: false, allowWorldDrop: false);
            }
        }

        public override void PostUpdateWorld()
        {
            if (!Enabled)
            {
                return;
            }

            byte currentCap = GetWorldCapLevel();

            // Seed on the first tick after world load without announcing so existing worlds never spam chat.
            if (lastAnnouncedCap == byte.MaxValue)
            {
                lastAnnouncedCap = currentCap;
                return;
            }

            if (currentCap <= lastAnnouncedCap)
            {
                return;
            }

            lastAnnouncedCap = currentCap;

            string announcementKey;
            if (currentCap == MaxPreShmLevel + 1)
            {
                announcementKey = "UI.WeaponReinforcement.CapUnlockedRadiant";
            }
            else
            {
                announcementKey = "UI.WeaponReinforcement.CapUnlocked";
            }

            Color toastColor = new Color(245, 210, 118);
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                string announcement = LangUtils.GetTextValue(announcementKey, currentCap);
                Main.NewText(announcement, toastColor);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                NetworkText announcement = NetworkText.FromKey("Mods.tsorcRevamp." + announcementKey, currentCap);
                ChatHelper.BroadcastChatMessage(announcement, toastColor);
            }
        }

        private static void BuildBossShardDropRegistries()
        {
            int paleEmber = ModContent.ItemType<PaleEmber>();
            int radiantEmber = ModContent.ItemType<RadiantEmber>();

            // Single source of truth for first-open boss bag Pale Ember / Radiant Ember rewards
            // (modeled on AssignedBossExtras; injected automatically via GlobalItem.ModifyItemLoot).
            bossBagShardDrops = new Dictionary<int, BossShardDrop>
            {
                // Pre-Hardmode boss bags from Skeletron / Ossric onward (1 Pale Ember on first open)
                [ItemID.SkeletronBossBag] = new BossShardDrop { ShardItemType = paleEmber, Count = 1 },
                [ModContent.ItemType<GravelordNitoBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 1 }, // Barrowlord Ossric
                [ModContent.ItemType<JungleWyvernBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 1 },
                [ModContent.ItemType<AncientDemonBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 1 }, // Defiled Demon
                [ItemID.WallOfFleshBossBag] = new BossShardDrop { ShardItemType = paleEmber, Count = 1 },

                // Hardmode boss bags (2 Pale Embers on first open)
                [ItemID.DestroyerBossBag] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<TheRageBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<TheSorrowBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<TheHunterBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<TheMachineBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<TriadBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<WyvernMageBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<DeathBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },
                [ModContent.ItemType<AttraidiesBag>()] = new BossShardDrop { ShardItemType = paleEmber, Count = 2 },

                // Super-Hardmode boss bags (12 SHM bosses x 2 = 24 Radiant Embers on first open)
                [ModContent.ItemType<KrakenBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<MarilithBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<LichBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<BlightBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<ChaosBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<WyvernMageShadowBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<OolacileSorcererBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<ArtoriasBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<HellkiteBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<SeathBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<WitchkingBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 },
                [ModContent.ItemType<DarkCloudBag>()] = new BossShardDrop { ShardItemType = radiantEmber, Count = 2 }
            };

            // Optional first-kill shard drops for non-bag invaders / minibosses (injected via GlobalNPC.ModifyNPCLoot).
            bossNpcShardDrops = new Dictionary<int, BossShardDrop>();
        }

        public static bool TryGetBossBagShardDrop(int bagItemType, out BossShardDrop drop)
        {
            if (bossBagShardDrops == null)
            {
                BuildBossShardDropRegistries();
            }

            return bossBagShardDrops.TryGetValue(bagItemType, out drop);
        }

        public static bool TryGetBossNpcShardDrop(int npcType, out BossShardDrop drop)
        {
            if (bossNpcShardDrops == null)
            {
                BuildBossShardDropRegistries();
            }

            return bossNpcShardDrops.TryGetValue(npcType, out drop);
        }

        private static void BuildRegistry()
        {
            registry = new Dictionary<int, WeaponReinforcementEntry>
            {
                [ModContent.ItemType<ArcherSpiritBell>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 24, 32, 44, 58, 74, 95, 114, 138, 166, 200, 240 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [2] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.ArcherSpiritBell.ReinforcementPerk2"
                        }
                    }
                },
                [ModContent.ItemType<UhlansSiegebowItem>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 22, 29, 40, 54, 70, 90, 108, 130, 156, 188, 226 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [2] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.UhlansSiegebowItem.ReinforcementPerk2"
                        }
                    }
                },
                [ModContent.ItemType<GravemawTome>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 18, 26, 38, 54, 74, 102, 122, 146, 175, 210, 252 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [GravemawTome.ShadowflamePerkLevel] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.GravemawTome.ReinforcementPerk3"
                        }
                    }
                },
                // Provisional +5..+10 SHM curves (pending WeaponBench tuning runs)
                [ModContent.ItemType<HeartOfWinter>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = MaxPreShmLevel,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 95, 110, 128, 148, 172, 200 }
                },
                [ModContent.ItemType<WrathOfGold>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = MaxPreShmLevel,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 95, 110, 128, 148, 172, 200 }
                },
                [ModContent.ItemType<SerpentsFangItem>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 32, 36, 46, 62, 95, 145, 168, 196, 228, 266, 310 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [SerpentsFangItem.PerkTier3Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsFangItem.ReinforcementPerk3",
                            FormatArgs = new object[] { SerpentsFangItem.Tier3LungeStaminaCost }
                        },
                        [SerpentsFangItem.PerkTier6Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsFangItem.ReinforcementPerk6",
                            FormatArgs = new object[] { SerpentsFangItem.Tier6LungeDropletCount, SerpentsFangItem.Tier6LungeCritBonusPercent }
                        }
                    }
                },
                [ModContent.ItemType<SerpentsCoilItem>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 32, 36, 46, 60, 92, 155, 168, 194, 224, 258, 298 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [SerpentsCoilItem.PerkTier3Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsCoilItem.ReinforcementPerk3",
                            FormatArgs = new object[] { SerpentsCoilItem.Tier3SplashDropletCount }
                        },
                        [SerpentsCoilItem.PerkTier6Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsCoilItem.ReinforcementPerk6",
                            FormatArgs = new object[] { SerpentsCoilItem.Tier6VolleyInterval, SerpentsCoilItem.Tier6DropletDamagePercent }
                        }
                    }
                },
                [ModContent.ItemType<CausticMawItem>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 34, 38, 48, 60, 88, 145, 160, 185, 214, 248, 288 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [CausticMawItem.PerkTier3Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.CausticMawItem.ReinforcementPerk3",
                            FormatArgs = new object[] { CausticMawItem.Tier3PoolDurationTicks / 60f }
                        },
                        [CausticMawItem.PerkTier6Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.CausticMawItem.ReinforcementPerk6",
                            FormatArgs = new object[] { CausticMawItem.Tier6EruptionDropletCount }
                        }
                    }
                },
                [ModContent.ItemType<SerpentsTailLashItem>()] = new WeaponReinforcementEntry
                {
                    NativeLevel = 0,
                    MaxLevel = MaxShmLevel,
                    DamageByLevel = new[] { 32, 36, 46, 60, 82, 112, 122, 140, 162, 188, 218 },
                    MilestonePerks = new Dictionary<byte, ReinforcementPerk>
                    {
                        [SerpentsTailLashItem.PerkTier3Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsTailLashItem.ReinforcementPerk3",
                            FormatArgs = new object[] { SerpentsTailLashItem.Tier3SummonTagDamage, SerpentsTailLashItem.Tier3SummonTagCrit }
                        },
                        [SerpentsTailLashItem.PerkTier6Level] = new ReinforcementPerk
                        {
                            LocalizationKey = "Items.SerpentsTailLashItem.ReinforcementPerk6",
                            FormatArgs = new object[]
                            {
                                SerpentsTailLashItem.Tier6NeedleStrikeInterval,
                                SerpentsTailLashItem.Tier6NeedleDropletCount,
                                SerpentsTailLashItem.Tier6NeedleDropletPercent,
                                SerpentsTailLashItem.Tier6SummonTagDamage,
                                SerpentsTailLashItem.Tier6SummonTagCrit
                            }
                        }
                    }
                }
            };
        }

        public static bool TryGetEntry(int itemType, out WeaponReinforcementEntry entry)
        {
            if (registry == null)
            {
                BuildRegistry();
            }

            return registry.TryGetValue(itemType, out entry);
        }

        public static bool CanEverReinforce(Item item)
        {
            if (!Enabled || item == null || item.IsAir)
            {
                return false;
            }

            return TryGetEntry(item.type, out _);
        }

        public static byte GetUpgradesEarned(Item item)
        {
            if (item == null || item.IsAir)
            {
                return 0;
            }

            if (item.TryGetGlobalItem(out WeaponReinforcementGlobalItem globalItem))
            {
                return globalItem.UpgradesEarned;
            }

            return 0;
        }

        public static byte GetDisplayedLevel(Item item)
        {
            if (!TryGetEntry(item.type, out WeaponReinforcementEntry entry))
            {
                return 0;
            }

            int level = entry.NativeLevel + GetUpgradesEarned(item);
            if (level > entry.MaxLevel)
            {
                level = entry.MaxLevel;
            }

            return (byte)level;
        }

        public static int GetDamageForLevel(WeaponReinforcementEntry entry, byte displayedLevel)
        {
            // DamageByLevel starts at entry.NativeLevel so native HM/SHM weapons only list their active tiers.
            int index = displayedLevel - entry.NativeLevel;
            if (index < 0)
            {
                index = 0;
            }
            else if (index >= entry.DamageByLevel.Length)
            {
                index = entry.DamageByLevel.Length - 1;
            }

            return entry.DamageByLevel[index];
        }

        public static int GetEffectiveBaseDamage(Item item)
        {
            if (item == null || item.IsAir)
            {
                return 0;
            }

            if (!Enabled || !TryGetEntry(item.type, out WeaponReinforcementEntry entry))
            {
                return item.damage;
            }

            byte displayedLevel = GetDisplayedLevel(item);
            return GetDamageForLevel(entry, displayedLevel);
        }

        public static byte GetWorldCapLevel()
        {
            byte cap = 0;

            if (NPC.downedBoss3)
            {
                cap = Math.Max(cap, (byte)1);
            }

            if (Main.hardMode)
            {
                cap = Math.Max(cap, (byte)2);

                // Count only the 7 required Hardmode bosses (Rage, Sorrow, Hunter, Destroyer, Triad, Machine, Golem).
                // Plantera and Empress of Light are excluded; 6 kills unlocks +5 in time for Golem and Attraidies.
                int keyHardmodeBosses = ProgressionScaling.HardmodeBossesDowned;

                if (keyHardmodeBosses >= 2)
                {
                    cap = Math.Max(cap, (byte)3);
                }

                if (keyHardmodeBosses >= 4)
                {
                    cap = Math.Max(cap, (byte)4);
                }

                if (keyHardmodeBosses >= 6)
                {
                    cap = Math.Max(cap, (byte)5);
                }
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                // Entering SHM guarantees at least +5; every 2 SHM bosses unlocks one Radiant step up to +10 (at 10 bosses).
                int maxShmSteps = MaxShmLevel - MaxPreShmLevel;
                int shmSteps = Math.Min(maxShmSteps, ProgressionScaling.ShmBossesDowned / 2);
                byte shmCap = (byte)(MaxPreShmLevel + shmSteps);
                cap = Math.Max(cap, shmCap);
            }

            return cap;
        }

        public static string GetGateLabelForLevel(byte targetLevel)
        {
            if (targetLevel <= 1)
            {
                return LangUtils.GetTextValue("UI.WeaponReinforcement.GateSkeletron");
            }

            if (targetLevel == 2)
            {
                return LangUtils.GetTextValue("UI.WeaponReinforcement.GateWallOfFlesh");
            }

            if (targetLevel <= MaxPreShmLevel)
            {
                if (!Main.hardMode)
                {
                    return LangUtils.GetTextValue("UI.WeaponReinforcement.GateWallOfFlesh");
                }

                int requiredHardmode = (targetLevel - 2) * 2;
                int killedHardmode = Math.Min(requiredHardmode, ProgressionScaling.HardmodeBossesDowned);
                return LangUtils.GetTextValue("UI.WeaponReinforcement.GateHardmodeProgress", requiredHardmode, killedHardmode);
            }

            int requiredShmBosses = (targetLevel - MaxPreShmLevel) * 2;
            if (!tsorcRevampWorld.SuperHardMode)
            {
                return LangUtils.GetTextValue("UI.WeaponReinforcement.GateShmBefore", requiredShmBosses);
            }

            int killedShm = Math.Min(requiredShmBosses, ProgressionScaling.ShmBossesDowned);
            return LangUtils.GetTextValue("UI.WeaponReinforcement.GateShmProgress", requiredShmBosses, killedShm);
        }

        public static ReinforcementCost GetUpgradeCost(byte targetLevel)
        {
            if (targetLevel <= MaxPreShmLevel)
            {
                int preShmIndex = Math.Clamp(targetLevel - 1, 0, 4);
                int[] shardCounts = { 1, 1, 2, 2, 3 };
                int[] darkSouls = { 3000, 8000, 18000, 35000, 60000 };

                return new ReinforcementCost
                {
                    MaterialItemType = ModContent.ItemType<PaleEmber>(),
                    MaterialCount = shardCounts[preShmIndex],
                    DarkSouls = darkSouls[preShmIndex]
                };
            }

            int shmIndex = Math.Clamp(targetLevel - MaxPreShmLevel - 1, 0, 4);
            int[] radiantCounts = { 1, 1, 2, 2, 3 };
            int[] shmSouls = { 35000, 50000, 65000, 85000, 100000 };

            return new ReinforcementCost
            {
                MaterialItemType = ModContent.ItemType<RadiantEmber>(),
                MaterialCount = radiantCounts[shmIndex],
                DarkSouls = shmSouls[shmIndex]
            };
        }

        /// <summary>
        /// Counts an item type across every player container: Soul Slot (for Dark Souls),
        /// main inventory, Void Bag, Piggy Bank, Safe, and the 3,000-slot Storage box.
        /// </summary>
        public static long CountItemAcrossAllStorage(Player player, int itemType)
        {
            long total = 0;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (itemType == ModContent.ItemType<DarkSoulItem>() && modPlayer.SoulSlot != null)
            {
                Item soulSlotItem = modPlayer.SoulSlot.Item;
                if (soulSlotItem != null && !soulSlotItem.IsAir && soulSlotItem.type == itemType)
                {
                    total += soulSlotItem.stack;
                }
            }

            Item[][] containers =
            {
                player.inventory,
                player.bank4.item,
                player.bank.item,
                player.bank2.item
            };

            for (int c = 0; c < containers.Length; c++)
            {
                Item[] items = containers[c];
                int limit = items.Length;
                if (c == 0 && limit > 58)
                {
                    limit = 58;
                }

                for (int i = 0; i < limit; i++)
                {
                    Item candidate = items[i];
                    if (candidate != null && !candidate.IsAir && candidate.type == itemType)
                    {
                        total += candidate.stack;
                    }
                }
            }

            for (int i = 0; i < modPlayer.StorageItems.Count; i++)
            {
                Item stored = modPlayer.StorageItems[i];
                if (stored != null && !stored.IsAir && stored.type == itemType)
                {
                    total += stored.stack;
                }
            }

            return total;
        }

        /// <summary>
        /// Deducts an exact item count across all player storage containers in priority order.
        /// Caller must verify CountItemAcrossAllStorage >= amount first for atomic transactions.
        /// </summary>
        public static void ConsumeItemAcrossAllStorage(Player player, int itemType, int amount)
        {
            int remaining = amount;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (remaining > 0 && itemType == ModContent.ItemType<DarkSoulItem>() && modPlayer.SoulSlot != null)
            {
                Item soulSlotItem = modPlayer.SoulSlot.Item;
                if (soulSlotItem != null && !soulSlotItem.IsAir && soulSlotItem.type == itemType)
                {
                    int take = Math.Min(remaining, soulSlotItem.stack);
                    soulSlotItem.stack -= take;
                    remaining -= take;
                    if (soulSlotItem.stack <= 0)
                    {
                        soulSlotItem.TurnToAir();
                    }
                }
            }

            Item[][] containers =
            {
                player.inventory,
                player.bank4.item,
                player.bank.item,
                player.bank2.item
            };

            for (int c = 0; c < containers.Length && remaining > 0; c++)
            {
                Item[] items = containers[c];
                int limit = items.Length;
                if (c == 0 && limit > 58)
                {
                    limit = 58;
                }

                for (int i = 0; i < limit && remaining > 0; i++)
                {
                    Item candidate = items[i];
                    if (candidate != null && !candidate.IsAir && candidate.type == itemType)
                    {
                        int take = Math.Min(remaining, candidate.stack);
                        candidate.stack -= take;
                        remaining -= take;
                        if (candidate.stack <= 0)
                        {
                            candidate.TurnToAir();
                        }
                    }
                }
            }

            bool touchedStorage = false;
            for (int i = 0; i < modPlayer.StorageItems.Count && remaining > 0; i++)
            {
                Item stored = modPlayer.StorageItems[i];
                if (stored != null && !stored.IsAir && stored.type == itemType)
                {
                    int take = Math.Min(remaining, stored.stack);
                    stored.stack -= take;
                    remaining -= take;
                    touchedStorage = true;
                    if (stored.stack <= 0)
                    {
                        stored.TurnToAir();
                    }
                }
            }

            if (touchedStorage)
            {
                modPlayer.CompactStorage();
            }
        }

        public static bool CanReinforceNow(Player player, Item item, out string blockReason)
        {
            blockReason = string.Empty;

            if (!Enabled)
            {
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorDisabled");
                return false;
            }

            if (item == null || item.IsAir || !TryGetEntry(item.type, out WeaponReinforcementEntry entry))
            {
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorNotReinforceable");
                return false;
            }

            byte currentLevel = GetDisplayedLevel(item);
            if (currentLevel >= entry.MaxLevel)
            {
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorMaxReached");
                return false;
            }

            byte nextLevel = (byte)(currentLevel + 1);
            byte worldCap = GetWorldCapLevel();
            if (nextLevel > worldCap)
            {
                string gateLabel = GetGateLabelForLevel(nextLevel);
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorLocked", gateLabel, nextLevel);
                return false;
            }

            ReinforcementCost cost = GetUpgradeCost(nextLevel);
            long ownedMaterial = CountItemAcrossAllStorage(player, cost.MaterialItemType);
            if (ownedMaterial < cost.MaterialCount)
            {
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorNeedMaterial", cost.MaterialCount, cost.MaterialName, ownedMaterial);
                return false;
            }

            long ownedSouls = CountItemAcrossAllStorage(player, ModContent.ItemType<DarkSoulItem>());
            if (ownedSouls < cost.DarkSouls)
            {
                blockReason = LangUtils.GetTextValue("UI.WeaponReinforcement.ErrorNeedSouls", cost.DarkSouls.ToString("N0"));
                return false;
            }

            return true;
        }

        public static bool TryReinforce(Player player, Item item, out string statusMessage)
        {
            if (!CanReinforceNow(player, item, out string blockReason))
            {
                statusMessage = blockReason;
                return false;
            }

            byte nextLevel = (byte)(GetDisplayedLevel(item) + 1);
            ReinforcementCost cost = GetUpgradeCost(nextLevel);

            ConsumeItemAcrossAllStorage(player, cost.MaterialItemType, cost.MaterialCount);
            ConsumeItemAcrossAllStorage(player, ModContent.ItemType<DarkSoulItem>(), cost.DarkSouls);

            WeaponReinforcementGlobalItem globalItem = item.GetGlobalItem<WeaponReinforcementGlobalItem>();
            globalItem.UpgradesEarned++;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item37, player.Center);
                PopupText.NewText(PopupTextContext.ItemReforge, item, item.stack, true, false);
            }

            statusMessage = LangUtils.GetTextValue("UI.WeaponReinforcement.ReinforcedSuccess", nextLevel);
            return true;
        }
    }

    /// <summary>
    /// Stores per-instance earned reinforcement upgrades on eligible iconic weapons,
    /// persists across saves/net/reforges, applies band damage, and updates tooltips.
    /// </summary>
    public class WeaponReinforcementGlobalItem : GlobalItem
    {
        public byte UpgradesEarned;

        private byte pendingReforgeUpgrades;

        public override bool InstancePerEntity => true;

        public override GlobalItem Clone(Item from, Item to)
        {
            WeaponReinforcementGlobalItem clone = (WeaponReinforcementGlobalItem)base.Clone(from, to);
            clone.UpgradesEarned = UpgradesEarned;
            return clone;
        }

        public override void PreReforge(Item item)
        {
            pendingReforgeUpgrades = UpgradesEarned;
        }

        public override void PostReforge(Item item)
        {
            UpgradesEarned = pendingReforgeUpgrades;
            pendingReforgeUpgrades = 0;
        }

        public override void SaveData(Item item, TagCompound tag)
        {
            if (UpgradesEarned > 0)
            {
                tag["WeaponReinforcementUpgrades"] = UpgradesEarned;
            }
        }

        public override void LoadData(Item item, TagCompound tag)
        {
            if (tag.ContainsKey("WeaponReinforcementUpgrades"))
            {
                UpgradesEarned = tag.GetByte("WeaponReinforcementUpgrades");
            }
        }

        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(UpgradesEarned);
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            UpgradesEarned = reader.ReadByte();
        }

        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (WeaponReinforcement.TryGetBossBagShardDrop(item.type, out BossShardDrop drop))
            {
                itemLoot.Add(ItemDropRule.ByCondition(
                    WeaponReinforcement.FirstBagCondition,
                    drop.ShardItemType,
                    1,
                    drop.Count,
                    drop.Count));
            }
        }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!WeaponReinforcement.Enabled || UpgradesEarned == 0)
            {
                return;
            }

            if (!WeaponReinforcement.TryGetEntry(item.type, out WeaponReinforcementEntry entry))
            {
                return;
            }

            byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(item);
            int targetDamage = WeaponReinforcement.GetDamageForLevel(entry, displayedLevel);
            int nativeDamage = WeaponReinforcement.GetDamageForLevel(entry, entry.NativeLevel);
            int flatIncrease = targetDamage - nativeDamage;

            if (flatIncrease > 0)
            {
                // Apply as Base damage so vanilla prefixes and class % bonuses multiply cleanly on top.
                damage.Base += flatIncrease;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!WeaponReinforcement.Enabled || !WeaponReinforcement.TryGetEntry(item.type, out WeaponReinforcementEntry entry))
            {
                return;
            }

            byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(item);

            if (displayedLevel > 0)
            {
                TooltipLine nameLine = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "ItemName");
                if (nameLine != null)
                {
                    nameLine.Text = nameLine.Text + " +" + displayedLevel;
                }
            }

            string badgeText = LangUtils.GetTextValue("UI.WeaponReinforcement.TooltipBadge", displayedLevel, entry.MaxLevel);
            tooltips.Add(new TooltipLine(Mod, "ReinforcementBadge", badgeText));

            if (entry.MilestonePerks != null)
            {
                foreach (KeyValuePair<byte, ReinforcementPerk> perk in entry.MilestonePerks)
                {
                    string perkText = perk.Value.GetText();
                    if (displayedLevel >= perk.Key)
                    {
                        string activeLine = LangUtils.GetTextValue("UI.WeaponReinforcement.TooltipPerkActive", perk.Key, perkText);
                        tooltips.Add(new TooltipLine(Mod, "ReinforcementPerk" + perk.Key, activeLine));
                    }
                    else
                    {
                        string lockedLine = LangUtils.GetTextValue("UI.WeaponReinforcement.TooltipPerkLocked", perk.Key, perkText);
                        tooltips.Add(new TooltipLine(Mod, "ReinforcementPerk" + perk.Key, lockedLine));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Injects first-kill Tempering / Radiant Tempering Shard drops for non-bag invaders and minibosses
    /// from WeaponReinforcement's central registry.
    /// </summary>
    public class WeaponReinforcementGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (WeaponReinforcement.TryGetBossNpcShardDrop(npc.type, out BossShardDrop drop))
            {
                npcLoot.Add(ItemDropRule.ByCondition(
                    WeaponReinforcement.FirstKillCondition,
                    drop.ShardItemType,
                    1,
                    drop.Count,
                    drop.Count));
            }
        }
    }
}
