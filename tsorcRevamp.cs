using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;
using tsorcRevamp.Banners;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items;
using tsorcRevamp.Items.BossBags;
using tsorcRevamp.Items.Lore;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Pets;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.NPCs.Bosses;
using tsorcRevamp.NPCs.Bosses.JungleWyvern;
using tsorcRevamp.NPCs.Bosses.Okiku.FinalForm;
using tsorcRevamp.NPCs.Bosses.Okiku.FirstForm;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm;
using tsorcRevamp.NPCs.Bosses.Pinwheel;
using tsorcRevamp.NPCs.Bosses.PrimeV2;
using tsorcRevamp.NPCs.Bosses.Serris;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.HellkiteDragon;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath;
using tsorcRevamp.NPCs.Bosses.WyvernMage;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Enemies.JungleWyvernJuvenile;
using tsorcRevamp.NPCs.Enemies.ParasyticWorm;
using tsorcRevamp.NPCs.Enemies.SuperHardMode.SerpentOfTheAbyss;
using tsorcRevamp.NPCs.Special;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.NullSprite;
using tsorcRevamp.Projectiles.Summon.Phoenix;
using tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.SunsetQuasar;
using tsorcRevamp.Projectiles.Summon.Tetsujin;
using tsorcRevamp.Projectiles.Summon.EtherianWyvern;
using tsorcRevamp.Projectiles.Summon.PhotonicDownpour;
using tsorcRevamp.Projectiles.Summon.ShatteredReflection;
using tsorcRevamp.Projectiles.Summon.TripleThreat;
using tsorcRevamp.Tiles;
using tsorcRevamp.Tiles.BuffStations;
using tsorcRevamp.Tiles.Relics;
using tsorcRevamp.Tiles.Trophies;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;
using static tsorcRevamp.ILEdits;
using static tsorcRevamp.MethodSwaps;

namespace tsorcRevamp
{
    public class tsorcRevamp : Mod
    {
        public class tsorcItemDropRuleConditions
        {
            public static IItemDropRuleCondition SuperHardmodeRule;
            public static IItemDropRuleCondition FirstBagRule;
            public static IItemDropRuleCondition CursedRule;
            public static IItemDropRuleCondition FirstBagCursedRule;
            public static IItemDropRuleCondition AdventureModeRule;
            public static IItemDropRuleCondition NonAdventureModeRule;
            public static IItemDropRuleCondition NonExpertFirstKillRule;
            public static IItemDropRuleCondition DownedSkeletronRule;
        }

        public enum BossExtras
        {
            EstusFlaskShard = 0b1000,
            GuardianSoul = 0b0100,
            StaminaVessel = 0b0010,
            SublimeBoneDust = 0b0001,
            DarkSoulsOnly = 0b0000,
            SoulVessel = 0b10000
        };

        public static ModKeybind toggleDragoonBoots;
        public static ModKeybind reflectionShiftKey;
        public static ModKeybind specialAbility;
        public static ModKeybind NecromancersSpell;
        public static ModKeybind KrakensCast;
        public static ModKeybind WolfRing;
        public static ModKeybind WingsOfSeath;
        public static ModKeybind Shunpo;
        public static bool isAdventureMap = false;
        public static int DarkSoulCustomCurrencyId;
        internal bool UICooldown = false;
        internal bool worldButtonClicked = false;
        internal static int worldDownloadFailures = 0;
        internal static int musicModDownloadFailures = 0;
        public static List<int> KillAllowed;
        public static List<int> PlaceAllowed;
        public static List<int> DroppableTiles = new List<int>();
        public static List<int> Unbreakable;
        public static List<int> IgnoredTiles;
        public static List<int> CrossModTiles;
        public static List<int> PlaceAllowedModTiles;
        public static List<int> BannedItems;
        public static List<int> RestrictedHooks;
        public static List<int> DisabledRecipes;
        public static List<int> GiantWormSegments;
        public static List<int> DevourerSegments;
        public static List<int> TombCrawlerSegments;
        public static List<int> DiggerSegments;
        public static List<int> LeechSegments;
        public static List<int> SeekerSegments;
        public static List<int> DuneSplicerSegments;
        public static List<int> StardustWormSegments;
        public static List<int> CrawltipedeSegments;
        public static List<int> EaterOfWorldsSegments;
        public static List<int> DestroyerSegments;
        public static List<int> JungleWyvernSegments;
        public static List<int> JuvenileJungleWyvernSegments;
        public static List<int> ParasyticWormSegments;
        public static List<int> SerpentOfTheAbyssSegments;
        public static List<int> MechaDragonSegments;
        public static List<int> SerrisSegments;
        public static List<int> GhostDragonSegments;
        public static List<int> HellkiteDragonSegments;
        public static List<int> LichKingSerpentSegments;
        public static List<int> SeathSegments;
        public static List<int> UntargetableNPCs;
        public static List<int> MageNPCs;
        public static Dictionary<BossExtras, (IItemDropRuleCondition Condition, int ID)> BossExtrasDescription;
        public static Dictionary<int, BossExtras> AssignedBossExtras;
        public static Dictionary<int, int> BossBagIDtoNPCID;
        public static Dictionary<int, List<int>> RemovedBossBagLoot;
        public static Dictionary<int, List<IItemDropRule>> AddedBossBagLoot;
        public static Dictionary<int, List<(int ID, int Count)>> ModifiedRecipes;
        public static Dictionary<int, Vector2> WhipTipBases;
        public static Dictionary<int, float> WhipRanges;

        internal BonfireUIState BonfireUIState;
        internal UserInterface _bonfireUIState; //"but zeo!", you say
        internal DarkSoulCounterUIState DarkSoulCounterUIState;
        internal UserInterface _darkSoulCounterUIState; //"prefacing a name with an underscore is supposed to be for private fields!"
        internal UserInterface EmeraldHeraldUserInterface;
        internal EstusFlaskUIState EstusFlaskUIState;
        internal CeruleanFlaskUIState CeruleanFlaskUIState;
        internal UserInterface _estusFlaskUIState; //okay
        internal UserInterface _ceruleanFlaskUIState; //idk what to say
        internal PotionBagUIState PotionUIState;
        internal UserInterface PotionBagUserInterface;
        internal CustomMapUIState DownloadUIState;
        internal UserInterface DownloadUI;
        internal MapMarkersUIState MarkerState;
        internal UserInterface MarkerInterface;

        public static FieldInfo AudioLockInfo;
        public static FieldInfo ActiveSoundInstancesInfo;
        public static FieldInfo AreSoundsPausedInfo;
        public static FieldInfo TrackedSoundsInfo;

        public static Effect TheAbyssEffect;
        public static Effect RetShockwaveEffect;
        public static Effect SpazShockwaveEffect;
        public static Effect CatShockwaveEffect;
        //public static Effect AttraidiesEffect;

        public static bool MusicNeedsUpdate = false;
        public static bool justUpdatedMusic = false;
        public static bool ReloadNeeded = false;
        public static bool SpecialReloadNeeded = false;
        public static bool DownloadingMusic = false;
        public static float MusicDownloadProgress = 0;
        public static string newMapUpdateString = "";
        public static float MapDownloadProgress = 0;
        public static float MapDownloadTotalBytes = 0;
        public static ModKeybind DodgerollKey;
        //public static ModHotKey SwordflipKey;

        internal static bool[] CustomDungeonWalls;

        public static bool ActuationBypassActive = false;

        public static int MarkerSelected = -1;

        public static SoapstoneTileEntity NearbySoapstone;
        public static bool NearbySoapstoneMouse;
        public static float NearbySoapstoneMouseDistance;

        public static Texture2D NoiseTurbulent;
        public static Texture2D NoiseSplotchy;
        public static Texture2D NoiseWavy;
        public static Texture2D NoiseCircuit;
        public static Texture2D NoiseVoronoi;
        public static Texture2D NoiseMarble;
        public static Texture2D NoiseSmooth;
        public static Texture2D NoiseSwirly;
        public static Texture2D NoisePerlin;

        public override void Load()
        {
            TextureAssets.Npc[NPCID.Deerclops] = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/AncestralSpirit");
            TextureAssets.NpcHeadBoss[39] = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/AncestralSpirit_Head_Boss");

            toggleDragoonBoots = KeybindLoader.RegisterKeybind(this, "Dragoon Boots", Microsoft.Xna.Framework.Input.Keys.Z);
            reflectionShiftKey = KeybindLoader.RegisterKeybind(this, "Reflection Shift", Microsoft.Xna.Framework.Input.Keys.O);
            DodgerollKey = KeybindLoader.RegisterKeybind(this, "Dodge Roll", Microsoft.Xna.Framework.Input.Keys.LeftAlt);
            specialAbility = KeybindLoader.RegisterKeybind(this, "Special Ability", Microsoft.Xna.Framework.Input.Keys.Q);
            WolfRing = KeybindLoader.RegisterKeybind(this, "Wolf Ring", Microsoft.Xna.Framework.Input.Keys.Y);
            WingsOfSeath = KeybindLoader.RegisterKeybind(this, "Wings of Seath speed toggle", Microsoft.Xna.Framework.Input.Keys.U);
            //armor set bonus keybinds
            NecromancersSpell = KeybindLoader.RegisterKeybind(this, "Necromancers Spell", Microsoft.Xna.Framework.Input.Keys.V);
            KrakensCast = KeybindLoader.RegisterKeybind(this, "Krakens Cast", Microsoft.Xna.Framework.Input.Keys.V);
            Shunpo = KeybindLoader.RegisterKeybind(this, "Shunpo", Microsoft.Xna.Framework.Input.Keys.V);
            //SwordflipKey = KeybindLoader.RegisterKeybind(this, "Sword Flip", Microsoft.Xna.Framework.Input.Keys.P);

            DarkSoulCustomCurrencyId = CustomCurrencyManager.RegisterCurrency(new DarkSoulCustomCurrency(ModContent.ItemType<SoulCoin>(), 99999L));

            BonfireUIState = new BonfireUIState();
            _bonfireUIState = new UserInterface();

            DarkSoulCounterUIState = new DarkSoulCounterUIState();
            //if (!Main.dedServ) DarkSoulCounterUIState.Activate();
            _darkSoulCounterUIState = new UserInterface();

            EstusFlaskUIState = new EstusFlaskUIState();
            //if (!Main.dedServ) EstusFlaskUIState.Activate();
            _estusFlaskUIState = new UserInterface();

            CeruleanFlaskUIState = new CeruleanFlaskUIState();
            _ceruleanFlaskUIState = new UserInterface();

            PotionUIState = new PotionBagUIState();
            PotionBagUserInterface = new UserInterface();


            DownloadUIState = new CustomMapUIState();
            DownloadUI = new UserInterface();

            MarkerState = new MapMarkersUIState();
            MarkerInterface = new UserInterface();


            ApplyMethodSwaps();
            ApplyILs();
            PopulateArrays();

            if (Main.dedServ)
                return;

            BonfireUIState.Activate();
            _bonfireUIState.SetState(BonfireUIState);
            _darkSoulCounterUIState.SetState(DarkSoulCounterUIState);

            _estusFlaskUIState.SetState(EstusFlaskUIState);

            _ceruleanFlaskUIState.SetState(CeruleanFlaskUIState);

            PotionBagUserInterface.SetState(PotionUIState);
            DownloadUI.SetState(DownloadUIState);
            MarkerState.Activate();
            MarkerInterface.SetState(MarkerState);

            Main.QueueMainThreadAction(TransparentTextureHandler.TransparentTextureFix);

            tsorcRevamp Instance = this;

            SpazmatismV2.secondStageHeadSlot = AddBossHeadTexture("tsorcRevamp/NPCs/Bosses/SpazmatismV2_Head_Boss_2", -1);
            RetinazerV2.secondStageHeadSlot = AddBossHeadTexture("tsorcRevamp/NPCs/Bosses/RetinazerV2_Head_Boss_2", -1);
            Cataluminance.secondStageHeadSlot = AddBossHeadTexture("tsorcRevamp/NPCs/Bosses/Cataluminance_Head_Boss_2", -1);

            TheAbyssEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TheAbyssShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Filters.Scene["tsorcRevamp:TheAbyss"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(TheAbyssEffect), "TheAbyssShaderPass").UseImage("Images/Misc/noise"), EffectPriority.Low);


            RetShockwaveEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TriadShockwave", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Filters.Scene["tsorcRevamp:RetShockwave"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(RetShockwaveEffect), "TriadShockwavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
            SpazShockwaveEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TriadShockwave", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Filters.Scene["tsorcRevamp:SpazShockwave"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(SpazShockwaveEffect), "TriadShockwavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
            CatShockwaveEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TriadShockwave", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Filters.Scene["tsorcRevamp:CatShockwave"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(CatShockwaveEffect), "TriadShockwavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);

            NoiseTurbulent = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/TurbulentNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseSplotchy = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SplotchyNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseWavy = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/WavyNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseCircuit = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/CircuitNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseVoronoi = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/VoronoiNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseMarble = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/MarbleNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseSmooth = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SmoothNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoiseSwirly = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SwirlyNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            NoisePerlin = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/PerlinNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);

            //AttraidiesEffect = Instance.GetEffect("Effects/ScreenFilters/AttraidiesShader");
            //Filters.Scene["tsorcRevamp:AttraidiesShader"] = new Filter(new ScreenShaderData(new Terraria.Ref<Effect>(AttraidiesEffect), "AttraidiesShaderPass").UseImage("Images/Misc/noise"), EffectPriority.Low);

            EmeraldHeraldUserInterface = new UserInterface();


            /*
            if (!Main.dedServ)
            {
                Main.instance.LoadNPC(NPCID.TheDestroyer);
                TextureAssets.Npc[NPCID.TheDestroyer] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_134");
                Main.instance.LoadNPC(NPCID.TheDestroyerBody);
                TextureAssets.Npc[NPCID.TheDestroyerBody] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_135");
                Main.instance.LoadNPC(NPCID.TheDestroyerTail);
                TextureAssets.Npc[NPCID.TheDestroyerTail] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_136");
                Main.instance.LoadGore(156);
                TextureAssets.Gore[156] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/Gore_156");
                Main.instance.LoadNPC(NPCID.Probe);
                TextureAssets.Npc[NPCID.Probe] = ModContent.Request<Texture2D>("NPCs/Bosses/TheDestroyer/NPC_139");
            }*/

            UpdateCheck();
        }

        private void PopulateArrays()
        {
            #region tsorcItemDropRuleConditions class
            tsorcItemDropRuleConditions.SuperHardmodeRule = new SuperHardmodeRule();
            tsorcItemDropRuleConditions.FirstBagRule = new FirstBagRule();
            tsorcItemDropRuleConditions.CursedRule = new CursedRule();
            tsorcItemDropRuleConditions.FirstBagCursedRule = new FirstBagCursedRule();
            tsorcItemDropRuleConditions.AdventureModeRule = new AdventureModeRule();
            tsorcItemDropRuleConditions.NonAdventureModeRule = new NonAdventureModeRule();
            tsorcItemDropRuleConditions.NonExpertFirstKillRule = new NonExpertFirstKillRule();
            tsorcItemDropRuleConditions.DownedSkeletronRule = new DownedSkeletronRule();
            #endregion
            //--------
            #region Unbreakable list
            Unbreakable = new List<int>() {
                TileID.Platforms, TileID.Signs, TileID.Teleporter, TileID.TeleportationPylon, //platforms, altars, signs, teleporters, pylons, 22 ie demonite ore was removed here as it should be breakable
                TileID.MusicBoxes, TileID.LunarMonolith, TileID.BloodMoonMonolith, TileID.VoidMonolith, //music boxes, all monoliths
                TileID.Rope, TileID.Chain, TileID.VineRope, TileID.SilkRope, TileID.WebRope, //all ropes and chain
                TileID.Spikes, TileID.WoodenSpikes, TileID.LandMine, TileID.RollingCactus, //spikes, jungle spikes, land mines, rolling cactus
                TileID.Statues, TileID.AlphabetStatues, TileID.BoulderStatue, TileID.Traps, TileID.Boulder, TileID.Explosives, TileID.Firework, TileID.Detonator, TileID.FakeContainers, TileID.FakeContainers2, //all statues, traps, boulders, explosives, party rockets, detonator, trapped chests
                TileID.ActiveStoneBlock, TileID.InactiveStoneBlock, TileID.Bubble, TileID.Grate, TileID.GrateClosed, TileID.LunarOre, //toggled stone blocks, bubbles, grates open and closed, Luminite
                TileID.Lever, TileID.PressurePlates, TileID.Switches, TileID.InletPump, TileID.OutletPump, TileID.Timers, TileID.LogicGateLamp, TileID.LogicGate, TileID.ConveyorBeltLeft, TileID.ConveyorBeltRight, TileID.LogicSensor, TileID.WirePipe, TileID.AnnouncementBox, TileID.WeightedPressurePlate, TileID.WireBulb, TileID.GemLocks, TileID.ProjectilePressurePad, // ALL (other) WIRING
                ItemID.RedLight, ItemID.GreenLight, TileID.DesertFossil
            };
            #endregion
            //--------
            #region KillAllowed list
            KillAllowed = new List<int>() {
                //6, 7, 8, 9, 22, 37, 58, 63, 64, 65, 66, 67, 67, 68, 107, 108, 111, 166, 167, 168, 169, 211, 221, 222, 223, //All Ores
                //50, //books (Boss tome can be bought, or a few books can be found in the village for crafting it)
                //56, 79, 85, //obsidian, beds, tombstones (misc notable disables)
                TileID.Torches, TileID.Bottles, // torches, tabled bottles 
                TileID.Heart, TileID.LifeFruit, TileID.ManaCrystal,
                TileID.Trees, TileID.Saplings, TileID.MushroomTrees, TileID.PalmTree, TileID.Bamboo, TileID.TreeAmethyst, TileID.TreeTopaz, TileID.TreeSapphire, TileID.TreeEmerald, TileID.TreeRuby, TileID.TreeDiamond, TileID.TreeAmber, TileID.GemSaplings, TileID.VanityTreeSakuraSaplings, TileID.VanityTreeSakura, TileID.VanityTreeWillowSaplings, TileID.VanityTreeYellowWillow, // all trees and saplings
                TileID.Tables, TileID.Tables2, TileID.Kegs, TileID.Blendomatic, TileID.MeatGrinder, TileID.DyeVat, TileID.ImbuingStation, TileID.TeaKettle, //tables, specialized crafting stations
                TileID.Anvils, TileID.Furnaces, TileID.WorkBenches, TileID.Hellforge, TileID.Loom, TileID.CookingPots, TileID.Bookcases, TileID.Sawmill, TileID.TinkerersWorkbench, TileID.AdamantiteForge, TileID.MythrilAnvil, TileID.Sinks, TileID.Autohammer, TileID.HeavyWorkBench, TileID.AlchemyTable, TileID.LunarCraftingStation, //core crafting stations
                TileID.Solidifier, TileID.BoneWelder, TileID.FleshCloningVat, TileID.GlassKiln, TileID.LihzahrdFurnace, TileID.LivingLoom, TileID.SkyMill, TileID.IceMachine, TileID.SteampunkBoiler, TileID.HoneyDispenser, TileID.LesionStation, //theme furniture crafting stations
                TileID.Containers, TileID.Containers2, TileID.PiggyBank, TileID.Safes, TileID.DefendersForge, TileID.Banners, TileID.WarTableBanner, TileID.SharpeningStation, TileID.AmmoBox, TileID.CrystalBall, TileID.BewitchingTable, TileID.WarTable, TileID.CatBast, TileID.SliceOfCake, //chests, piggy bank, safe, defenders forge, banners, buff stations
                TileID.Candles, TileID.WaterCandle, TileID.PlatinumCandle, TileID.PeaceCandle, TileID.ClayPot, TileID.Cannon, TileID.BeachPiles, TileID.Crystals, //all candles, clay pot, cannons, seashells, crystal/gelatin shards 
                TileID.MushroomPlants, TileID.Cactus, TileID.Coral, TileID.ImmatureHerbs, TileID.MatureHerbs, TileID.BloomingHerbs, TileID.DyePlants, TileID.Pumpkins, //mushrooms, cactus, coral, all forms of herbs, dye plants, pumpkins
                TileID.Mannequin, TileID.Womannequin, TileID.DisplayDoll, TileID.Painting3X3, TileID.GolfTrophies, TileID.MasterTrophyBase, //all mannequins, trophies and relics
                TileID.BreakableIce, TileID.MagicalIceBlock, TileID.SeaweedPlanter, TileID.AbigailsFlower, //thin ice (breakable kind), Ice Rod's ice, seaweed/herb planters, abigail's flower
                TileID.CrackedBlueDungeonBrick, TileID.CrackedGreenDungeonBrick, TileID.CrackedPinkDungeonBrick,
                TileID.Sunflower, TileID.Pots, TileID.Cobweb, TileID.Vines, TileID.JungleVines, TileID.HallowedVines, TileID.CrimsonVines, TileID.VineFlowers, TileID.MushroomVines, //sunflower, pots, cobwebs, all cuttable vine
                TileID.ShadowOrbs, TileID.CorruptThorns, TileID.JungleThorns, TileID.CrimsonThorns, TileID.Sand, TileID.Pearlsand, TileID.Ebonsand, TileID.Crimsand, TileID.Silt, TileID.Slush, TileID.LandMine, TileID.RollingCactus, //orbs/hearts, all thorns, all sands, land mines, rolling cactus
                TileID.Stalactite, TileID.ExposedGems, TileID.SmallPiles, TileID.LargePiles, TileID.LargePiles2, TileID.PlantDetritus, TileID.OasisPlants, TileID.Larva, TileID.PlanteraBulb, TileID.AntlionLarva, //all ambient objects (background breakables), QB Larva, Plantera Bulb
                TileID.Campfire, TileID.HangingLanterns, //Sunflowers, Campfires, Lanterns(including Heart Lantern and Star in a Bottle)
                TileID.Sundial, TileID.Moondial, //Sundial, Moondial
                TileID.LivingFire, TileID.LivingCursedFire, TileID.LivingDemonFire, TileID.LivingFrostFire, TileID.LivingIchor
            };
            #endregion
            //--------
            #region PlaceAllowed list
            PlaceAllowed = new List<int>() {
                TileID.Torches, TileID.Bottles, // torches, tabled bottles 
                TileID.Trees, TileID.Saplings, TileID.MushroomTrees, TileID.PalmTree, TileID.Bamboo, TileID.TreeAmethyst, TileID.TreeTopaz, TileID.TreeSapphire, TileID.TreeEmerald, TileID.TreeRuby, TileID.TreeDiamond, TileID.TreeAmber, TileID.GemSaplings, TileID.VanityTreeSakuraSaplings, TileID.VanityTreeSakura, TileID.VanityTreeWillowSaplings, TileID.VanityTreeYellowWillow, // all trees and saplings
                TileID.Tables, TileID.Tables2, TileID.Kegs, TileID.Blendomatic, TileID.MeatGrinder, TileID.DyeVat, TileID.ImbuingStation, TileID.TeaKettle, //tables, specialized crafting stations
                TileID.Anvils, TileID.Furnaces, TileID.WorkBenches, TileID.Hellforge, TileID.Loom, TileID.CookingPots, TileID.Bookcases, TileID.Sawmill, TileID.TinkerersWorkbench, TileID.AdamantiteForge, TileID.MythrilAnvil, TileID.Sinks, TileID.Autohammer, TileID.HeavyWorkBench, TileID.AlchemyTable, TileID.LunarCraftingStation, //core crafting stations
                TileID.Solidifier, TileID.BoneWelder, TileID.FleshCloningVat, TileID.GlassKiln, TileID.LihzahrdFurnace, TileID.LivingLoom, TileID.SkyMill, TileID.IceMachine, TileID.SteampunkBoiler, TileID.HoneyDispenser, TileID.LesionStation, //theme furniture crafting stations
                TileID.Containers, TileID.Containers2, TileID.PiggyBank, TileID.Safes, TileID.DefendersForge, TileID.Banners, TileID.WarTableBanner, TileID.SharpeningStation, TileID.AmmoBox, TileID.CrystalBall, TileID.BewitchingTable, TileID.WarTable, TileID.CatBast, TileID.SliceOfCake, //chests, piggy bank, safe, defenders forge, banners, buff stations
                TileID.Candles, TileID.WaterCandle, TileID.PlatinumCandle, TileID.PeaceCandle, TileID.ClayPot, TileID.Cannon, TileID.BeachPiles, //all candles, clay pot, cannons, seashells
                TileID.MushroomPlants, TileID.Cactus, TileID.Coral, TileID.ImmatureHerbs, TileID.MatureHerbs, TileID.BloomingHerbs, TileID.DyePlants, TileID.Pumpkins, //mushrooms, cactus, coral, all forms of herbs, dye plants, pumpkins
                TileID.Mannequin, TileID.Womannequin, TileID.DisplayDoll, TileID.Painting3X3, TileID.GolfTrophies, TileID.MasterTrophyBase, //all mannequins, trophies and relics
                TileID.SeaweedPlanter, //seaweed/herb planters
                TileID.Sunflower, TileID.Campfire, TileID.HangingLanterns, //Sunflowers, Campfires, Lanterns(including Heart Lantern and Star in a Bottle)
                TileID.Sundial, TileID.Moondial, //Sundial, Moondial
                TileID.Grass
            };
            #endregion
            //--------
            #region IgnoredTiles list
            IgnoredTiles = new List<int>() {
                TileID.Torches, TileID.Heart, TileID.LifeFruit, TileID.Banners,
                TileID.Trees, TileID.Saplings, TileID.MushroomTrees, TileID.PalmTree, TileID.Bamboo, TileID.TreeAmethyst, TileID.TreeTopaz, TileID.TreeSapphire, TileID.TreeEmerald, TileID.TreeRuby, TileID.TreeDiamond, TileID.TreeAmber, TileID.GemSaplings, TileID.VanityTreeSakuraSaplings, TileID.VanityTreeSakura, TileID.VanityTreeWillowSaplings, TileID.VanityTreeYellowWillow, // all trees and saplings
                TileID.Crystals, TileID.BeachPiles, TileID.BreakableIce, TileID.AbigailsFlower, //crystal/gelatin shards, seashells, thin ice (breakable kind), abigail's flower
                TileID.MushroomPlants, TileID.Cactus, TileID.Coral, TileID.ImmatureHerbs, TileID.MatureHerbs, TileID.BloomingHerbs, TileID.DyePlants, TileID.Pumpkins, //mushrooms, cactus, coral, all forms of herbs, dye plants, pumpkins
                TileID.Sunflower, TileID.Pots, TileID.Cobweb, TileID.Vines, TileID.JungleVines, TileID.HallowedVines, TileID.CrimsonVines, TileID.VineFlowers, TileID.MushroomVines, //sunflower, pots, cobwebs, all cuttable vine
                TileID.ShadowOrbs, TileID.CorruptThorns, TileID.JungleThorns, TileID.CrimsonThorns, TileID.LandMine, TileID.RollingCactus, //orbs/hearts, all thorns, land mines, rolling cactus
                TileID.Stalactite, TileID.ExposedGems, TileID.SmallPiles, TileID.LargePiles, TileID.LargePiles2, TileID.PlantDetritus, TileID.OasisPlants, TileID.Larva, TileID.PlanteraBulb, TileID.AntlionLarva, //all ambient objects (background breakables), QB Larva, Plantera Bulb
                TileID.Plants, TileID.Plants2, TileID.CorruptPlants, TileID.JunglePlants, TileID.JunglePlants2, TileID.HallowedPlants, TileID.HallowedPlants2, TileID.LongMoss, TileID.CrimsonPlants, TileID.LilyPad, TileID.Cattail, TileID.SeaOats, TileID.OasisPlants, TileID.Seaweed, //cuttable plants - all biomes
                TileID.Lever, TileID.PressurePlates, TileID.Switches, TileID.InletPump, TileID.OutletPump, TileID.Timers, TileID.LogicGateLamp, TileID.LogicGate, TileID.ConveyorBeltLeft, TileID.ConveyorBeltRight, TileID.LogicSensor, TileID.WirePipe, TileID.AnnouncementBox, TileID.WeightedPressurePlate, TileID.WireBulb, TileID.GemLocks, TileID.ProjectilePressurePad, //wiring, incl pressure plates
                TileID.WoodenBeam, TileID.LivingFire, TileID.LivingFrostFire, TileID.LivingCursedFire, TileID.LivingDemonFire, TileID.LivingUltrabrightFire, TileID.ChimneySmoke
            };
            #endregion
            //--------
            #region CrossModTiles list
            CrossModTiles = new List<int>();

            Mod MagicStorage;
            if (ModLoader.TryGetMod("MagicStorage", out MagicStorage))
            {
                CrossModTiles.Add(MagicStorage.Find<ModTile>("CraftingAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("RemoteAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageAccess").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageComponent").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageHeart").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageUnit").Type);
                CrossModTiles.Add(MagicStorage.Find<ModTile>("StorageConnector").Type);
            }

            Mod MagicStorageExtra;
            if (ModLoader.TryGetMod("MagicStorageExtra", out MagicStorageExtra))
            {
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("CraftingAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("CreativeStorageUnit").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("RemoteAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageAccess").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageComponent").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageHeart").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageUnit").Type);
                CrossModTiles.Add(MagicStorageExtra.Find<ModTile>("StorageConnector").Type);
            }
            #endregion
            //--------
            #region PlaceAllowedModTiles list
            PlaceAllowedModTiles = new List<int>()
            {
                ModContent.TileType<NecromancyAltarTile>(),
                ModContent.TileType<EnemyBannerTile>(),
                ModContent.TileType<AncestralSpiritTrophyTile>(), ModContent.TileType<AncestralSpiritRelicTile>(),
                ModContent.TileType<TheRageTrophyTile>(), ModContent.TileType<TheRageRelicTile>(),
                ModContent.TileType<TheSorrowTrophyTile>(), ModContent.TileType<TheSorrowRelicTile>(),
                ModContent.TileType<TheHunterTrophyTile>(), ModContent.TileType<TheHunterRelicTile>(),
                ModContent.TileType<TheMachineTrophyTile>(), ModContent.TileType<TheMachineRelicTile>(),
                ModContent.TileType<RetinazerTrophyTile>(), ModContent.TileType<SpazmatismTrophyTile>(), ModContent.TileType<CataluminanceTrophyTile>(), ModContent.TileType<TheTriadRelicTile>()
            };
            #endregion
            //--------
            #region DroppableTiles list
            DroppableTiles = new List<int>()
            {
                TileID.AbigailsFlower,
                TileID.Crystals,
                TileID.Heart,
                TileID.LifeFruit,
                TileID.ManaCrystal,
                TileID.ShadowOrbs,
                //TileID.Cobweb,
                TileID.Plants,
                TileID.Plants2,
                TileID.CorruptPlants,
                TileID.CrimsonPlants,
                TileID.HallowedPlants,
                TileID.HallowedPlants2,
                TileID.JungleGrass,
                TileID.JunglePlants,
                TileID.JunglePlants2,
                TileID.BloomingHerbs,
                TileID.ImmatureHerbs,
                TileID.MatureHerbs,
                TileID.MushroomGrass,
                TileID.MushroomPlants,
                TileID.MushroomTrees,
                TileID.MushroomVines,
                TileID.Trees,
                TileID.TreeAmber,
                TileID.TreeAmethyst,
                TileID.TreeAsh,
                TileID.TreeDiamond,
                TileID.TreeEmerald,
                TileID.TreeRuby,
                TileID.TreeSapphire,
                TileID.TreeTopaz,
                TileID.ChristmasTree,
                TileID.PalmTree,
                TileID.PineTree,
                TileID.VanityTreeSakura,
                TileID.VanityTreeSakuraSaplings,
                TileID.VanityTreeWillowSaplings,
                TileID.VanityTreeYellowWillow,
                TileID.Books
            };
            #endregion
            //--------
            #region BannedItems list
            BannedItems = new List<int>()
            {
                ItemID.RodofDiscord,
                ItemID.CorruptionKey,
                ItemID.CrimsonKey,
                ItemID.HallowedKey,
                ItemID.JungleKey,
                ItemID.FrozenKey,
                ItemID.MechanicalEye,
                ItemID.MechanicalSkull,
                ItemID.MechanicalWorm
            };
            #endregion
            //--------
            #region RestrictedHooks list
            RestrictedHooks = new List<int>()
            {
                ItemID.SlimeHook,
                ItemID.SquirrelHook,
                ItemID.BatHook,
                ItemID.CandyCaneHook,
                ItemID.FishHook
            };
            #endregion
            //--------
            #region DisabledRecipes list
            DisabledRecipes = new List<int>()
            {
                #region Accessories IDs
                ItemID.ObsidianSkull,
                ItemID.BalloonHorseshoeSharkron,
                ItemID.BlueHorseshoeBalloon,
                ItemID.WhiteHorseshoeBalloon,
                ItemID.YellowHorseshoeBalloon,
                ItemID.BalloonHorseshoeHoney
                #endregion
                ,
                #region Armor IDs
                    #region Multiclass
                    ItemID.HallowedPlateMail,
                    ItemID.HallowedGreaves
                    #endregion
                    ,
                    #region Mage
                    ItemID.JungleHat,
                    ItemID.JungleShirt,
                    ItemID.JunglePants
                    ,
                    ItemID.MeteorHelmet,
                    ItemID.MeteorSuit,
                    ItemID.MeteorLeggings
                    ,
                    ItemID.HallowedHeadgear
                    ,
                    ItemID.SpectreHood,
                    ItemID.SpectreMask,
                    ItemID.SpectreRobe,
                    ItemID.SpectrePants
                    #endregion
                    ,
                    #region Melee
                    ItemID.MoltenHelmet,
                    ItemID.MoltenBreastplate,
                    ItemID.MoltenGreaves
                    ,
                    ItemID.HallowedMask
                    ,
                    ItemID.TurtleHelmet,
                    ItemID.TurtleScaleMail,
                    ItemID.TurtleLeggings
                    ,
                    ItemID.BeetleHelmet,
                    ItemID.BeetleShell,
                    ItemID.BeetleScaleMail,
                    ItemID.BeetleLeggings
                    #endregion
                    ,
                    #region Ranged
                    ItemID.NecroHelmet,
                    ItemID.NecroBreastplate,
                    ItemID.NecroGreaves
                    ,
                    ItemID.HallowedHelmet
                    ,
                    ItemID.ShroomiteHeadgear,
                    ItemID.ShroomiteHelmet,
                    ItemID.ShroomiteMask,
                    ItemID.ShroomiteBreastplate,
                    ItemID.ShroomiteLeggings
                    #endregion
                    ,
                    #region Summoner
                    ItemID.ObsidianHelm,
                    ItemID.ObsidianShirt,
                    ItemID.ObsidianPants
                    ,
                    ItemID.SpiderBreastplate,
                    ItemID.SpiderGreaves
                    ,
                    ItemID.HallowedHood
                    ,
                    ItemID.SpookyHelmet,
                    ItemID.SpookyBreastplate,
                    ItemID.SpookyLeggings
                    #endregion
                #endregion
                ,
                #region BossSummoners IDs
                ItemID.GoblinBattleStandard,
                ItemID.SlimeCrown,
                ItemID.SuspiciousLookingEye,
                ItemID.WormFood,
                ItemID.BloodySpine,
                ItemID.Abeemination,
                ItemID.DeerThing,
                ItemID.MechanicalEye,
                ItemID.MechanicalSkull,
                ItemID.MechanicalWorm,
                ItemID.CelestialSigil
                #endregion
                ,
                #region Large Gems IDs
                ItemID.LargeAmber,
                ItemID.LargeAmethyst,
                ItemID.LargeDiamond,
                ItemID.LargeEmerald,
                ItemID.LargeRuby,
                ItemID.LargeSapphire,
                ItemID.LargeTopaz
                #endregion
                ,
                #region Pickaxes IDs
                ItemID.MoltenPickaxe,
                ItemID.CobaltDrill,
                ItemID.CobaltPickaxe,
                ItemID.PalladiumDrill,
                ItemID.PalladiumPickaxe,
                ItemID.MythrilDrill,
                ItemID.MythrilPickaxe,
                ItemID.OrichalcumDrill,
                ItemID.OrichalcumPickaxe,
                ItemID.AdamantiteDrill,
                ItemID.AdamantitePickaxe,
                ItemID.TitaniumDrill,
                ItemID.TitaniumPickaxe,
                ItemID.Drax,
                ItemID.PickaxeAxe
                #endregion
                ,
                #region Potions
                ItemID.FlaskofFire,         // default recipe which contains Hellstone Ore
                ModContent.ItemType<ArmorDrugPotion>(),
                ItemID.ObsidianSkinPotion
                #endregion
                ,
                #region Ropes IDs
                ItemID.RopeCoil,
                ItemID.VineRopeCoil,
                ItemID.WebRope,
                ItemID.SilkRope,
                ItemID.WebRopeCoil,
                ItemID.SilkRopeCoil
                #endregion
                ,
                #region Weapon IDs
                    #region Mage
                    // nothing here yet
                    #endregion
                    // ,
                    #region Melee
                    ItemID.BladeofGrass,
                    ItemID.Excalibur
                    #endregion
                    ,
                    #region Ranged
                    ItemID.Sandgun
                    #endregion
                    ,
                    #region Summoner
                        #region Whips
                        ItemID.ThornWhip,
                        ItemID.BoneWhip,
                        ItemID.CoolWhip,
                        ItemID.SwordWhip,
                        #endregion
                        
                        #region Minions
                        ItemID.SpiderStaff
                        #endregion
                        
                        #region Sentries

                        #endregion
                    #endregion
                #endregion
                ,
                #region Wings IDs
                ItemID.AngelWings,
                ItemID.DemonWings,
                ItemID.FairyWings,
                ItemID.HarpyWings,
                ItemID.ButterflyWings,
                ItemID.BoneWings,
                ItemID.FlameWings,
                ItemID.FrozenWings,
                ItemID.BatWings,
                ItemID.BeeWings,
                ItemID.TatteredFairyWings,
                ItemID.SpookyWings,
                ItemID.GhostWings,
                ItemID.BeetleWings,
                ItemID.WingsSolar,
                ItemID.WingsNebula,
                ItemID.WingsStardust,
                ItemID.WingsVortex
                #endregion
                ,
                #region Blocks/Furniture
                ItemID.DirtBlock, ItemID.StoneBlock, ItemID.Wood, ItemID.WoodenDoor, ItemID.StoneWall, ItemID.DirtWall, ItemID.WoodenChair,
                ItemID.EbonstoneBlock, ItemID.WoodWall, ItemID.WoodPlatform, ItemID.CopperChandelier, ItemID.SilverChandelier, ItemID.GoldChandelier,
                ItemID.GrayBrick, ItemID.GrayBrickWall, ItemID.RedBrick, ItemID.RedBrickWall, ItemID.ClayBlock, ItemID.BlueBrick, ItemID.BlueBrickWall,
                ItemID.ChainLantern, ItemID.GreenBrick, ItemID.GreenBrickWall, ItemID.PinkBrick, ItemID.PinkBrickWall, ItemID.GoldBrick, ItemID.GoldBrickWall,
                ItemID.SilverBrick, ItemID.SilverBrickWall, ItemID.CopperBrick, ItemID.CopperBrickWall, ItemID.Sign, ItemID.MudBlock, ItemID.ObsidianBrick,
                ItemID.HellstoneBrick, ItemID.Bed, ItemID.Piano, ItemID.Dresser, ItemID.Bench, ItemID.Bathtub, ItemID.LampPost, ItemID.TikiTorch,
                ItemID.CookingPot, ItemID.Candelabra, ItemID.Throne, ItemID.Bowl, ItemID.Toilet, ItemID.GrandfatherClock, ItemID.ArmorStatue,
                ItemID.GlassWall, ItemID.PearlsandBlock, ItemID.PearlstoneBlock, ItemID.PearlstoneBrick, ItemID.IridescentBrick, ItemID.MudstoneBlock,
                ItemID.CobaltBrick, ItemID.MythrilBrick, ItemID.PearlstoneBrickWall, ItemID.IridescentBrickWall, ItemID.MudstoneBrickWall,
                ItemID.CobaltBrickWall, ItemID.MythrilBrickWall, ItemID.SiltBlock, ItemID.SwordStatue, ItemID.ShieldStatue, ItemID.BatStatue,
                ItemID.FishStatue, ItemID.BunnyStatue, ItemID.ReaperStatue, ItemID.WomanStatue, ItemID.GargoyleStatue, ItemID.GloomStatue,
                ItemID.CrabStatue, ItemID.HammerStatue, ItemID.PotionStatue, ItemID.SpearStatue, ItemID.CrossStatue, ItemID.BowStatue, ItemID.BoomerangStatue,
                ItemID.BootStatue, ItemID.BirdStatue, ItemID.AxeStatue, ItemID.TreeStatue, ItemID.AnvilStatue, ItemID.PickaxeStatue, ItemID.PillarStatue,
                ItemID.PotStatue, ItemID.SunflowerStatue, ItemID.PlankedWall, ItemID.WoodenBeam, ItemID.ActiveStoneBlock, ItemID.InactiveStoneBlock,
                ItemID.Boulder, ItemID.DemoniteBrick, ItemID.Explosives, ItemID.InletPump, ItemID.OutletPump, ItemID.Timer1Second, ItemID.Timer3Second,
                ItemID.Timer5Second, ItemID.CandyCaneBlock, ItemID.CandyCaneWall, ItemID.GreenCandyCaneBlock, ItemID.GreenCandyCaneWall, ItemID.SnowBlock,
                ItemID.SnowBrick, ItemID.SnowBrickWall, ItemID.AdamantiteBeam, ItemID.AdamantiteBeamWall, ItemID.DemoniteBrickWall, ItemID.SandstoneBrick,
                ItemID.SandstoneBrickWall, ItemID.EbonstoneBrick, ItemID.EbonstoneBrickWall, ItemID.RedStucco, ItemID.YellowStucco, ItemID.GreenStucco,
                ItemID.GrayStucco, ItemID.RedStuccoWall, ItemID.YellowStuccoWall, ItemID.GreenStuccoWall, ItemID.GrayStuccoWall, ItemID.Ebonwood,
                ItemID.RichMahogany, ItemID.Pearlwood, ItemID.EbonwoodWall, ItemID.RichMahoganyWall, ItemID.PearlwoodWall, ItemID.EbonwoodPlatform,
                ItemID.RichMahoganyPlatform, ItemID.PearlwoodPlatform, ItemID.BonePlatform, ItemID.EbonwoodPiano, ItemID.RichMahoganyPiano,
                ItemID.PearlwoodPiano, ItemID.EbonwoodBed, ItemID.RichMahoganyBed, ItemID.PearlwoodBed, ItemID.EbonwoodDresser, ItemID.RichMahoganyDresser,
                ItemID.PearlwoodDresser, ItemID.EbonwoodDoor, ItemID.RichMahoganyDoor, ItemID.PearlwoodDoor, ItemID.RainbowBrick, ItemID.RainbowBrickWall,
                ItemID.IceBlock, ItemID.TinChandelier, ItemID.TungstenChandelier, ItemID.PlatinumChandelier, ItemID.PlatinumCandelabra, ItemID.TinBrick,
                ItemID.TungstenBrick, ItemID.PlatinumBrick, ItemID.TinBrickWall, ItemID.TungstenBrickWall, ItemID.PlatinumBrickWall, ItemID.CactusWall,
                ItemID.Cloud, ItemID.CloudWall, ItemID.SlimeBlock, ItemID.FleshBlock, ItemID.MushroomWall, ItemID.RainCloud, ItemID.BoneBlock,
                ItemID.FrozenSlimeBlock, ItemID.BoneBlockWall, ItemID.SlimeBlockWall, ItemID.FleshBlockWall, ItemID.AsphaltBlock, ItemID.CactusDoor,
                ItemID.FleshDoor, ItemID.MushroomDoor, ItemID.LivingWoodDoor, ItemID.BoneDoor, ItemID.SunplateBlock, ItemID.DiscWall, ItemID.CrimstoneBlock,
                ItemID.SkywareDoor//currently up to ID 837
                #endregion
            };
            #endregion
            //--------
            #region AssignedBossExtras dictionary
            AssignedBossExtras = new Dictionary<int, BossExtras>()
            {   
                #region Vanilla
                {   ItemID.KingSlimeBossBag         , BossExtras.StaminaVessel      },
                {   ItemID.EyeOfCthulhuBossBag      , BossExtras.StaminaVessel
                                                    | BossExtras.SublimeBoneDust    },
                {   ItemID.EaterOfWorldsBossBag     , BossExtras.DarkSoulsOnly      },
                {   ItemID.BrainOfCthulhuBossBag    , BossExtras.StaminaVessel      },
                {   ItemID.QueenBeeBossBag          , BossExtras.DarkSoulsOnly      },
                {   ItemID.SkeletronBossBag         , BossExtras.SublimeBoneDust    },
                {   ItemID.WallOfFleshBossBag       , BossExtras.EstusFlaskShard    },
                {   ItemID.DestroyerBossBag         , BossExtras.DarkSoulsOnly      },
                {   ItemID.TwinsBossBag             , BossExtras.DarkSoulsOnly      },
                {   ItemID.SkeletronPrimeBossBag    , BossExtras.SublimeBoneDust    },
                {   ItemID.PlanteraBossBag          , BossExtras.DarkSoulsOnly      },
                {   ItemID.GolemBossBag             , BossExtras.DarkSoulsOnly      },
                {   ItemID.FishronBossBag           , BossExtras.StaminaVessel      },
                {   ItemID.CultistBossBag           , BossExtras.DarkSoulsOnly      },
                {   ItemID.MoonLordBossBag          , BossExtras.DarkSoulsOnly      },
                {   ItemID.QueenSlimeBossBag        , BossExtras.DarkSoulsOnly      },
                {   ItemID.FairyQueenBossBag        , BossExtras.DarkSoulsOnly      },
                {   ItemID.BossBagBetsy             , BossExtras.DarkSoulsOnly      },
                {   ItemID.DeerclopsBossBag         , BossExtras.DarkSoulsOnly      },
                #endregion
                //--------
                #region tsorc
                {   ModContent.ItemType<PinwheelBag>()              , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<OolacileDemonBag>()         , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<SlograBag>()                , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<GaibonBag>()                , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<JungleWyvernBag>()          , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<AncestralSpiritBag>()       , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<AncientDemonBag>()          , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<HeroOfLumeliaBag>()         , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<TheRageBag>()               , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<TheSorrowBag>()             , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<TheHunterBag>()             , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<TheMachineBag>()            , BossExtras.SublimeBoneDust    },
                {   ModContent.ItemType<TriadBag>()                 , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<WyvernMageBag>()            , BossExtras.SoulVessel         },
                {   ModContent.ItemType<SerrisBag>()                , BossExtras.StaminaVessel      },
                {   ModContent.ItemType<DeathBag>()                 , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<MindflayerIllusionBag>()    , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<AttraidiesBag>()            , BossExtras.EstusFlaskShard    },
                {   ModContent.ItemType<KrakenBag>()                , BossExtras.GuardianSoul
                                                                    | BossExtras.StaminaVessel      },
                {   ModContent.ItemType<MarilithBag>()              , BossExtras.GuardianSoul
                                                                    | BossExtras.StaminaVessel      },
                {   ModContent.ItemType<LichBag>()                  , BossExtras.GuardianSoul
                                                                    | BossExtras.StaminaVessel      },
                {   ModContent.ItemType<BlightBag>()                , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<ChaosBag>()                 , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<WyvernMageShadowBag>()      , BossExtras.SoulVessel         },
                {   ModContent.ItemType<OolacileSorcererBag>()      , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<ArtoriasBag>()              , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<HellkiteBag>()              , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<SeathBag>()                 , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<WitchkingBag>()             , BossExtras.GuardianSoul       },
                {   ModContent.ItemType<DarkCloudBag>()             , BossExtras.DarkSoulsOnly      },
                {   ModContent.ItemType<GwynBag>()                  , BossExtras.DarkSoulsOnly      }
                #endregion
            };
            #endregion
            //--------
            #region BossExtrasDescription dictionary
            BossExtrasDescription = new Dictionary<BossExtras, (IItemDropRuleCondition Condition, int ID)>()
            {
                {   BossExtras.EstusFlaskShard  , ( tsorcItemDropRuleConditions.FirstBagCursedRule , ModContent.ItemType<EstusFlaskShard>() )   },
                {   BossExtras.GuardianSoul     , ( tsorcItemDropRuleConditions.FirstBagRule       , ModContent.ItemType<GuardianSoul>()    )   },
                {   BossExtras.StaminaVessel    , ( tsorcItemDropRuleConditions.FirstBagRule       , ModContent.ItemType<StaminaVessel>()   )   },
                {   BossExtras.SublimeBoneDust  , ( tsorcItemDropRuleConditions.FirstBagCursedRule , ModContent.ItemType<SublimeBoneDust>() )   },
                {   BossExtras.SoulVessel       , ( tsorcItemDropRuleConditions.FirstBagCursedRule , ModContent.ItemType<SoulVessel>()      )   }
            };
            #endregion
            //--------
            #region BossBagIDtoNPCID dictionary
            BossBagIDtoNPCID = new Dictionary<int, int>()
            {
                #region Vanilla
                {   ItemID.KingSlimeBossBag         , NPCID.KingSlime           },
                {   ItemID.EyeOfCthulhuBossBag      , NPCID.EyeofCthulhu        },
                {   ItemID.EaterOfWorldsBossBag     , NPCID.EaterofWorldsHead   },
                {   ItemID.BrainOfCthulhuBossBag    , NPCID.BrainofCthulhu      },
                {   ItemID.QueenBeeBossBag          , NPCID.QueenBee            },
                {   ItemID.SkeletronBossBag         , NPCID.SkeletronHead       },
                {   ItemID.WallOfFleshBossBag       , NPCID.WallofFlesh         },
                {   ItemID.DestroyerBossBag         , NPCID.TheDestroyer        },
                {   ItemID.TwinsBossBag             , NPCID.Retinazer           },      // and also NPCID.Spazmatism 
                {   ItemID.SkeletronPrimeBossBag    , NPCID.SkeletronPrime      },      // but it doesn't really matter
                {   ItemID.PlanteraBossBag          , NPCID.Plantera            },      // while their values are the same
                {   ItemID.GolemBossBag             , NPCID.Golem               },
                {   ItemID.FishronBossBag           , NPCID.DukeFishron         },
                {   ItemID.CultistBossBag           , NPCID.CultistBoss         },
                {   ItemID.MoonLordBossBag          , NPCID.MoonLordCore        },
                {   ItemID.QueenSlimeBossBag        , NPCID.QueenSlimeBoss      },
                {   ItemID.FairyQueenBossBag        , NPCID.HallowBoss          },
                {   ItemID.BossBagBetsy             , NPCID.DD2Betsy            },
                {   ItemID.DeerclopsBossBag         , NPCID.Deerclops           },
                #endregion
                //--------
                #region tsorc
                {   ModContent.ItemType<PinwheelBag>()              , ModContent.NPCType<Pinwheel>()                                                    },
                {   ModContent.ItemType<OolacileDemonBag>()         , ModContent.NPCType<AncientOolacileDemon>()                                        },
                {   ModContent.ItemType<SlograBag>()                , ModContent.NPCType<Slogra>()                                                      },
                {   ModContent.ItemType<GaibonBag>()                , ModContent.NPCType<Gaibon>()                                                      },
                {   ModContent.ItemType<JungleWyvernBag>()          , ModContent.NPCType<JungleWyvernHead>()                                            },
                {   ModContent.ItemType<AncientDemonBag>()          , ModContent.NPCType<AncientDemon>()                                                },
                {   ModContent.ItemType<HeroOfLumeliaBag>()         , ModContent.NPCType<HeroofLumelia>()                                               },
                {   ModContent.ItemType<TheRageBag>()               , ModContent.NPCType<TheRage>()                                                     },
                {   ModContent.ItemType<TheSorrowBag>()             , ModContent.NPCType<TheSorrow>()                                                   },
                {   ModContent.ItemType<TheHunterBag>()             , ModContent.NPCType<TheHunter>()                                                   },
                {   ModContent.ItemType<TheMachineBag>()            , ModContent.NPCType<TheMachine>()                                                  },
                {   ModContent.ItemType<TriadBag>()                 , ModContent.NPCType<Cataluminance>()                                               },
                {   ModContent.ItemType<WyvernMageBag>()            , ModContent.NPCType<WyvernMage>()                                                  },
                {   ModContent.ItemType<SerrisBag>()                , ModContent.NPCType<SerrisX>()                                                     },
                {   ModContent.ItemType<DeathBag>()                 , ModContent.NPCType<Death>()                                                       },
                {   ModContent.ItemType<MindflayerIllusionBag>()    , ModContent.NPCType<BrokenOkiku>()                                                 },
                {   ModContent.ItemType<AttraidiesBag>()            , ModContent.NPCType<Attraidies>()                                                  },
                {   ModContent.ItemType<KrakenBag>()                , ModContent.NPCType<WaterFiendKraken>()                                            },
                {   ModContent.ItemType<MarilithBag>()              , ModContent.NPCType<FireFiendMarilith>()                                           },
                {   ModContent.ItemType<LichBag>()                  , ModContent.NPCType<EarthFiendLich>()                                              },
                {   ModContent.ItemType<BlightBag>()                , ModContent.NPCType<Blight>()                                                      },
                {   ModContent.ItemType<ChaosBag>()                 , ModContent.NPCType<Chaos>()                                                       },
                {   ModContent.ItemType<WyvernMageShadowBag>()      , ModContent.NPCType<WyvernMageShadow>()                                            },
                {   ModContent.ItemType<OolacileSorcererBag>()      , ModContent.NPCType<AbysmalOolacileSorcerer>()                                     },
                {   ModContent.ItemType<ArtoriasBag>()              , ModContent.NPCType<Artorias>()                                                    },
                {   ModContent.ItemType<HellkiteBag>()              , ModContent.NPCType<HellkiteDragonHead>()                                          },
                {   ModContent.ItemType<SeathBag>()                 , ModContent.NPCType<SeathTheScalelessHead>()                                       },
                {   ModContent.ItemType<WitchkingBag>()             , ModContent.NPCType<Witchking>()                                                   },
                {   ModContent.ItemType<DarkCloudBag>()             , ModContent.NPCType<DarkCloud>()                                                   },
                {   ModContent.ItemType<GwynBag>()                  , ModContent.NPCType<Gwyn>()                                                        }
                #endregion
            };
            #endregion
            //--------
            #region RemovedBossBagLoot dictionary
            RemovedBossBagLoot = new Dictionary<int, List<int>>()
            {
                #region Vanilla
                {   ItemID.KingSlimeBossBag         ,   new List<int>()
                                                        {
                                                            ItemID.SlimySaddle
                                                        }                                   },
                {   ItemID.EyeOfCthulhuBossBag      ,   new List<int>()
                                                        {
                                                            ItemID.CorruptSeeds
                                                        }
                                                                                            },
                {   ItemID.EaterOfWorldsBossBag     ,   new List<int>()                     },
                {   ItemID.BrainOfCthulhuBossBag    ,   new List<int>()                     },
                {   ItemID.QueenBeeBossBag          ,   new List<int>()                     },
                {   ItemID.SkeletronBossBag         ,   new List<int>()                     },
                {   ItemID.WallOfFleshBossBag       ,   new List<int>()                     },
                {   ItemID.DestroyerBossBag         ,   new List<int>()                     },
                {   ItemID.TwinsBossBag             ,   new List<int>()                     },
                {   ItemID.SkeletronPrimeBossBag    ,   new List<int>()                     },
                {   ItemID.PlanteraBossBag          ,   new List<int>()                     },
                {   ItemID.GolemBossBag             ,   new List<int>()
                                                        {
                                                            ItemID.Picksaw
                                                        }                                   },
                {   ItemID.FishronBossBag           ,   new List<int>()                     },
                {   ItemID.CultistBossBag           ,   new List<int>()                     },
                {   ItemID.MoonLordBossBag          ,   new List<int>()                     },
                {   ItemID.QueenSlimeBossBag        ,   new List<int>()                     },
                {   ItemID.FairyQueenBossBag        ,   new List<int>()                     },
                {   ItemID.BossBagBetsy             ,   new List<int>()                     },
                {   ItemID.DeerclopsBossBag         ,   new List<int>()                     }
                #endregion
            };
            #endregion
            //--------
            #region AddedBossBagLoot dictionary
            AddedBossBagLoot = new Dictionary<int, List<IItemDropRule>>()
            {
                #region Vanilla
                {   ItemID.KingSlimeBossBag         ,   new List<IItemDropRule>()   {
                                                            ItemDropRule.Common(ItemID.SlimySaddle),
                                                            ItemDropRule.Common(ItemID.Katana)
                                                        }                                                          },
                {   ItemID.EyeOfCthulhuBossBag      ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ItemID.HermesBoots),
                                                            ItemDropRule.Common(ItemID.HerosHat),
                                                            ItemDropRule.Common(ItemID.HerosPants),
                                                            ItemDropRule.Common(ItemID.HerosShirt)
                                                        }                                                                                },
                {   ItemID.EaterOfWorldsBossBag     ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.ByCondition(tsorcItemDropRuleConditions.FirstBagRule, ModContent.ItemType<DarkSoul>(), 1, 5000, 5000),
                                                            ItemDropRule.Common(ItemID.GoldCoin, 1, 5, 7),
                                                         }                                                        },
                {   ItemID.BrainOfCthulhuBossBag    ,   new List<IItemDropRule>()                                                        },
                {   ItemID.QueenBeeBossBag          ,   new List<IItemDropRule>()                                                        },
                {   ItemID.SkeletronBossBag         ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<MiakodaFull>())
                                                        }                                                                                },
                {   ItemID.WallOfFleshBossBag       ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ItemID.MoltenPickaxe)
                                                        }                                                                                },
                {   ItemID.DestroyerBossBag         ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<CrestOfCorruption>()),
                                                            ItemDropRule.Common(ModContent.ItemType<RTQ2>())
                                                        }                                                                                },
                {   ItemID.SkeletronPrimeBossBag    ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<CrestOfSteel>())
                                                        }                                                                                },
                {   ItemID.PlanteraBossBag          ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<CrestOfLife>()),
                                                            ItemDropRule.Common(ModContent.ItemType<SoulOfLife>(), 1, 30, 30)
                                                        }                                                                                },
                {   ItemID.GolemBossBag             ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<CrestOfStone>()),
                                                            ItemDropRule.ByCondition(tsorcItemDropRuleConditions.AdventureModeRule,
                                                                                     ModContent.ItemType<BrokenPicksaw>()),
                                                            ItemDropRule.ByCondition(tsorcItemDropRuleConditions.NonAdventureModeRule,
                                                                                     ItemID.Picksaw, 3)
                                                        }                                                                                },
                {   ItemID.FishronBossBag           ,   new List<IItemDropRule>()                                                        },
                {   ItemID.CultistBossBag           ,   new List<IItemDropRule>()                                                        },
                {   ItemID.MoonLordBossBag          ,   new List<IItemDropRule>()                                                        },
                {   ItemID.QueenSlimeBossBag        ,   new List<IItemDropRule>()                                                        },
                {   ItemID.FairyQueenBossBag        ,   new List<IItemDropRule>()                                                        },
                {   ItemID.BossBagBetsy             ,   new List<IItemDropRule>()
                                                        {
                                                            ItemDropRule.Common(ModContent.ItemType<EtherianWyvernStaff>())
                                                        } },
                {   ItemID.DeerclopsBossBag         ,   new List<IItemDropRule>()                                                        }
                #endregion
            };
            #endregion
            //--------
            #region ModifiedRecipes List
            ModifiedRecipes = new Dictionary<int, List<(int ID, int Count)>>()
            {
                #region Boss Items
                { ItemID.PumpkinMoonMedallion,       new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofFright, 1),
                                            (ItemID.SoulofMight, 1),
                                            (ItemID.SoulofSight, 1)
                                        }                                       },
                { ItemID.NaughtyPresent,       new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofMight, 1),
                                            (ItemID.SoulofSight, 1)
                                        }                                       },
                #endregion

                #region Hooks
                { ItemID.IvyWhip,       new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.GrapplingHook, new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.AmethystHook,  new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.TopazHook,     new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.SapphireHook,  new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.EmeraldHook,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.RubyHook,      new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.DiamondHook,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                { ItemID.AmberHook,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.BeeWax, 1)
                                        }                                       },
                #endregion

                #region Robes
                { ItemID.AmethystRobe,  new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 550)
                                        }                                                   },
                { ItemID.TopazRobe,     new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 600)
                                        }                                                   },
                { ItemID.SapphireRobe,  new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 650)
                                        }                                                   },
                { ItemID.EmeraldRobe,   new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 700)
                                        }                                                   },
                { ItemID.RubyRobe,      new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 750)
                                        }                                                   },
                { ItemID.DiamondRobe,   new List<(int ItemID, int Count)>()
                                        {
                                            (ModContent.ItemType<DarkSoul>(), 800)
                                        }                                                   },
                #endregion

                #region Phasesabers
                { ItemID.BluePhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.GreenPhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.YellowPhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.OrangePhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.PurplePhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.RedPhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                { ItemID.WhitePhasesaber,   new List<(int ItemID, int Count)>()
                                        {
                                            (ItemID.SoulofLight, 5)
                                        }                                                   },
                #endregion
            };
            #endregion

            //--------
            #region CustomDungeonTiles list
            CustomDungeonWalls = new bool[500];
            for (int i = 0; i < 500; i++)
            {
                CustomDungeonWalls[i] = false;
            }
            CustomDungeonWalls[0] = true; //no wall
            CustomDungeonWalls[34] = true; //sandstone brick wall
            CustomDungeonWalls[63] = true; //flower wall
            CustomDungeonWalls[65] = true; //grass wall
            CustomDungeonWalls[71] = true; //ice wall
            #endregion

            //--------
            #region Worm Segment Lists
            GiantWormSegments = new List<int>()
            {
            NPCID.GiantWormHead,
            NPCID.GiantWormBody,
            NPCID.GiantWormTail
            };

            DevourerSegments = new List<int>()
            {
                NPCID.DevourerHead,
                NPCID.DevourerBody,
                NPCID.DevourerTail
            };

            TombCrawlerSegments = new List<int>()
        {
            NPCID.TombCrawlerHead,
            NPCID.TombCrawlerBody,
            NPCID.TombCrawlerTail
        };

            DiggerSegments = new List<int>()
        {
            NPCID.DiggerHead,
            NPCID.DiggerBody,
            NPCID.DiggerTail
        };

            LeechSegments = new List<int>()
        {
            NPCID.LeechHead,
            NPCID.LeechBody,
            NPCID.LeechTail
        };

            SeekerSegments = new List<int>()
        {
            NPCID.SeekerHead,
            NPCID.SeekerBody,
            NPCID.SeekerTail
        };

            DuneSplicerSegments = new List<int>()
        {
            NPCID.DuneSplicerHead,
            NPCID.DuneSplicerBody,
            NPCID.DuneSplicerTail
        };

            StardustWormSegments = new List<int>()
        {
            NPCID.StardustWormHead,
            NPCID.StardustWormBody,
            NPCID.StardustWormTail
        };

            CrawltipedeSegments = new List<int>()
        {
            NPCID.SolarCrawltipedeHead,
            NPCID.SolarCrawltipedeBody,
            NPCID.SolarCrawltipedeTail
        };

            EaterOfWorldsSegments = new List<int>()
        {
            NPCID.EaterofWorldsHead,
            NPCID.EaterofWorldsBody,
            NPCID.EaterofWorldsTail
        };

            DestroyerSegments = new List<int>()
        {
            NPCID.TheDestroyer,
            NPCID.TheDestroyerBody,
            NPCID.TheDestroyerTail
        };

            JungleWyvernSegments = new List<int>()
        {
            ModContent.NPCType<JungleWyvernHead>(),
            ModContent.NPCType<JungleWyvernBody>(),
            ModContent.NPCType<JungleWyvernBody2>(),
            ModContent.NPCType<JungleWyvernBody3>(),
            ModContent.NPCType<JungleWyvernLegs>(),
            ModContent.NPCType<JungleWyvernTail>()
        };

            JuvenileJungleWyvernSegments = new List<int>()
        {
            ModContent.NPCType<JungleWyvernJuvenileHead>(),
            ModContent.NPCType<JungleWyvernJuvenileBody>(),
            ModContent.NPCType<JungleWyvernJuvenileBody2>(),
            ModContent.NPCType<JungleWyvernJuvenileBody3>(),
            ModContent.NPCType<JungleWyvernJuvenileLegs>(),
            ModContent.NPCType<JungleWyvernJuvenileTail>()
        };

            ParasyticWormSegments = new List<int>()
        {
            ModContent.NPCType<ParasyticWormHead>(),
            ModContent.NPCType<ParasyticWormBody>(),
            ModContent.NPCType<ParasyticWormTail>()
        };

            SerpentOfTheAbyssSegments = new List<int>()
        {
            ModContent.NPCType<SerpentOfTheAbyssHead>(),
            ModContent.NPCType<SerpentOfTheAbyssBody>(),
            ModContent.NPCType<SerpentOfTheAbyssTail>()
        };

            MechaDragonSegments = new List<int>() //Wyvern Mage's Dragon
        {
            ModContent.NPCType<MechaDragonHead>(),
            ModContent.NPCType<MechaDragonBody>(),
            ModContent.NPCType<MechaDragonBody2>(),
            ModContent.NPCType<MechaDragonBody3>(),
            ModContent.NPCType<MechaDragonLegs>(),
            ModContent.NPCType<MechaDragonTail>()
        };

            SerrisSegments = new List<int>()
        {
            ModContent.NPCType<SerrisHead>(),
            ModContent.NPCType<SerrisBody>(),
            ModContent.NPCType<SerrisTail>()
        };

            GhostDragonSegments = new List<int>() //Wyvern Mage Shadow's Dragon
        {
            ModContent.NPCType<GhostDragonHead>(),
            ModContent.NPCType<GhostDragonBody>(),
            ModContent.NPCType<GhostDragonBody2>(),
            ModContent.NPCType<GhostDragonBody3>(),
            ModContent.NPCType<GhostDragonLegs>(),
            ModContent.NPCType<GhostDragonTail>()
        };

            HellkiteDragonSegments = new List<int>()
        {
            ModContent.NPCType<HellkiteDragonHead>(),
            ModContent.NPCType<HellkiteDragonBody>(),
            ModContent.NPCType<HellkiteDragonBody2>(),
            ModContent.NPCType<HellkiteDragonBody3>(),
            ModContent.NPCType<HellkiteDragonLegs>(),
            ModContent.NPCType<HellkiteDragonTail>()
        };

            LichKingSerpentSegments = new List<int>()
        {
            ModContent.NPCType<LichKingSerpentHead>(),
            ModContent.NPCType<LichKingSerpentBody>(),
            ModContent.NPCType<LichKingSerpentTail>()
        };

            SeathSegments = new List<int>()
        {
            ModContent.NPCType<SeathTheScalelessHead>(),
            ModContent.NPCType<SeathTheScalelessBody>(),
            ModContent.NPCType<SeathTheScalelessBody2>(),
            ModContent.NPCType<SeathTheScalelessBody3>(),
            ModContent.NPCType<SeathTheScalelessLegs>(),
            ModContent.NPCType<SeathTheScalelessTail>()
        };
            #endregion

            //--------
            #region WhipTipBaseSize dictionary and WhipRange dictionary
            WhipTipBases = new Dictionary<int, Vector2>()
        {
            { ProjectileID.BlandWhip, new Vector2(10, 18) },
            { ProjectileID.ThornWhip, new Vector2(22, 26) },
            { ProjectileID.BoneWhip, new Vector2(14, 18) },
            { ProjectileID.FireWhip, new Vector2(18, 26) },
            { ProjectileID.CoolWhip, new Vector2(14, 24) },
            { ProjectileID.SwordWhip, new Vector2(10, 16) },
            { ProjectileID.MaceWhip, new Vector2(14, 14) },
            { ProjectileID.ScytheWhip, new Vector2(28, 20) },
            { ProjectileID.RainbowWhip, new Vector2(14, 30) }
        };
            WhipRanges = new Dictionary<int, float>()
        {
            { ProjectileID.BlandWhip, 0.65f },
            { ProjectileID.ThornWhip, 0.8f },
            { ProjectileID.BoneWhip, 0.9f },
            { ProjectileID.FireWhip, 1.25f },
            { ProjectileID.CoolWhip, 1.35f },
            { ProjectileID.SwordWhip, 1.55f },
            { ProjectileID.MaceWhip, 1.3f },
            { ProjectileID.ScytheWhip, 1.75f },
            { ProjectileID.RainbowWhip, 1.7f }
        };
            #endregion

            //-------
            #region Untargetable NPC list
            UntargetableNPCs = new List<int>()
            {
            ModContent.NPCType<Bonfirefly>(),
            ModContent.NPCType<AbyssPortal>(), 
            ModContent.NPCType<AttraidiesApparition>(),
            ModContent.NPCType<GwynBossVision>()
            };
            #endregion

            //-------
            #region Mage NPC list
            MageNPCs = new List<int>()
            {
                NPCID.DarkCaster,
                NPCID.GoblinSorcerer,
                NPCID.GoblinSummoner,
                NPCID.CultistBoss,
                NPCID.DiabolistRed,
                NPCID.DiabolistWhite,
                NPCID.RaggedCaster,
                NPCID.RaggedCasterOpenCoat,
                NPCID.Necromancer,
                NPCID.NecromancerArmored,
                ModContent.NPCType<UndeadCaster>(),
                ModContent.NPCType<MindflayerServant>(),
                ModContent.NPCType<DungeonMage>(),
                ModContent.NPCType<MountedSandsprogMage>(),
                ModContent.NPCType<SandsprogMage>(),
                ModContent.NPCType<Necromancer>(),
                ModContent.NPCType<NecromancerElemental>(),
                ModContent.NPCType<Warlock>(),
                ModContent.NPCType<DemonSpirit>(),
                ModContent.NPCType<ShadowMage>(),
                ModContent.NPCType<AttraidiesIllusion>(),
                ModContent.NPCType<AttraidiesManifestation>(),
                ModContent.NPCType<AttraidiesMimic>(),
                ModContent.NPCType<WyvernMage>(),
                ModContent.NPCType<DarkShogunMask>(),
                ModContent.NPCType<DarkDragonMask>(),
                ModContent.NPCType<Okiku>(),
                ModContent.NPCType<BrokenOkiku>(),
                ModContent.NPCType<Attraidies>(),
                ModContent.NPCType<MindflayerKingServant>(),
                ModContent.NPCType<MindflayerServant>(),
                ModContent.NPCType<MindflayerIllusion>(),
                ModContent.NPCType<LichKingDisciple>()
            };
            #endregion
        }

        public override void Unload()
        {
            TextureAssets.Npc[NPCID.Deerclops] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{NPCID.Deerclops}");
            TextureAssets.NpcHeadBoss[39] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_Head_Boss_39");
            tsorcItemDropRuleConditions.SuperHardmodeRule = null;
            tsorcItemDropRuleConditions.FirstBagRule = null;
            tsorcItemDropRuleConditions.CursedRule = null;
            tsorcItemDropRuleConditions.FirstBagCursedRule = null;
            tsorcItemDropRuleConditions.AdventureModeRule = null;
            tsorcItemDropRuleConditions.NonAdventureModeRule = null;
            tsorcItemDropRuleConditions.NonExpertFirstKillRule = null;
            tsorcItemDropRuleConditions.DownedSkeletronRule = null;
            toggleDragoonBoots = null;
            reflectionShiftKey = null;
            specialAbility = null;
            NecromancersSpell = null;
            KrakensCast = null;
            WolfRing = null;
            WingsOfSeath = null;
            Shunpo = null;
            KillAllowed = null;
            PlaceAllowed = null;
            Unbreakable = null;
            IgnoredTiles = null;
            CrossModTiles = null;
            BannedItems = null;
            RestrictedHooks = null;
            DisabledRecipes = null;
            BossExtrasDescription = null;
            AssignedBossExtras = null;
            BossBagIDtoNPCID = null;
            tsorcRevampWorld.NewSlain = null;
            RemovedBossBagLoot = null;
            ModifiedRecipes = null;
            //the following sun and moon texture changes are failsafes. they should be set back to default in PreSaveAndQuit 
            TextureAssets.Sun = ModContent.Request<Texture2D>("Terraria/Images/Sun", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            TextureAssets.Sun2 = ModContent.Request<Texture2D>("Terraria/Images/Sun2");
            TextureAssets.Sun3 = ModContent.Request<Texture2D>("Terraria/Images/Sun3");
            NoiseTurbulent = null;
            NoiseSplotchy = null;
            NoiseWavy = null;

            for (int i = 0; i < TextureAssets.Moon.Length; i++)
            {
                TextureAssets.Moon[i] = ModContent.Request<Texture2D>("Terraria/Images/Moon_" + i);
            }
            DarkSoulCounterUIState.ConfigInstance = null;

            if (TransparentTextureHandler.TransparentTextures != null)
            {
                TransparentTextureHandler.TransparentTextures.Clear();
                TransparentTextureHandler.TransparentTextures = null;
            }

            /*
            for (int m = 1; m < Main.maxMusic; m++)
            {
                if (Main.music[m] != null)
                {
                    if (Main.music[m].IsPlaying)
                    {
                        Main.music[m].Stop(Microsoft.Xna.Framework.Audio.AudioStopOptions.Immediate);
                    }
                }
            }*/

            UnloadILs();
            CustomDungeonWalls = null;
            DodgerollKey = null;
            //SwordflipKey = null;

            /* IIRC this was to change the Destroyer's texture, which was never fully implemented?
             * I think there's a new way to do it now though
            if (!Main.dedServ)
            {
                Main.NPCLoaded[NPCID.TheDestroyer] = false;
                Main.NPCLoaded[NPCID.TheDestroyerBody] = false;
                Main.NPCLoaded[NPCID.TheDestroyerTail] = false;
                Main.NPCLoaded[NPCID.Probe] = false;
                Main.goreLoaded[156] = false;
            }*/
        }

        [Obsolete]
        public override void AddRecipes()/* tModPorter Note: Removed. Use ModSystem.AddRecipes */
        {
            foreach (var recipe in Main.recipe)
            {
                int itemID = recipe.createItem.type;
                // disable recipes
                if (DisabledRecipes.Contains(itemID))
                {
                    recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
                }

                //No processing recipes that have already been modified once
                if (ModifiedRecipes.ContainsKey(itemID))
                {
                    if (!(recipe.HasCondition(tsorcRevampWorld.AdventureModeDisabled) || recipe.HasCondition(tsorcRevampWorld.AdventureModeEnabled)))
                    {
                        //Split the recipe into two copies
                        Recipe modifiedRecipe = recipe.Clone();

                        //Give the unmodified one the AdventureModeDisabled condition
                        recipe.AddCondition(tsorcRevampWorld.AdventureModeDisabled);

                        //Give the modified one the AdventureModeEnabled condition
                        modifiedRecipe.AddCondition(tsorcRevampWorld.AdventureModeEnabled);

                        //Add the ingredients to the modified one
                        foreach (var ingredient in ModifiedRecipes[itemID])
                        {
                            modifiedRecipe.AddIngredient(ingredient.ID, ingredient.Count);
                        }

                        //Register it
                        modifiedRecipe.Register();
                    }
                }
            }
            // add new recipes
            ModRecipeHelper.AddRecipes();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            int message = reader.ReadByte();
            switch (message)
            {
                case tsorcPacketID.SyncSoulSlot:
                    {
                        byte player = reader.ReadByte(); //player.whoAmI
                        tsorcRevampPlayer modPlayer = Main.player[player].GetModPlayer<tsorcRevampPlayer>();
                        modPlayer.SoulSlot.Item = ItemIO.Receive(reader);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            modPlayer.SendSingleItemPacket(tsorcPacketID.SyncSoulSlot, modPlayer.SoulSlot.Item, -1, whoAmI);
                        }
                        break;
                    }
                case tsorcPacketID.SyncEventDust:
                    {
                        if (Main.netMode != NetmodeID.Server)
                        {
                            tsorcScriptedEvents.NetworkEvents = new List<NetworkEvent>();

                            int count = reader.ReadInt32();
                            /*
                             * 
                            eventPacket.WriteVector2(thisEvent.centerpoint);
                            eventPacket.Write(thisEvent.radius);
                            eventPacket.Write(thisEvent.dustID);
                            eventPacket.Write(thisEvent.square);
                            eventPacket.Write(thisEvent.queued);
                             */
                            for (int i = 0; i < count; i++)
                            {
                                //Main.NewText("Recieved event:");
                                Vector2 center = reader.ReadVector2();
                                //Main.NewText("center: " + center);
                                float radius = reader.ReadSingle();
                                //Main.NewText("radius: " + radius);
                                int dustID = reader.ReadInt32();
                                //Main.NewText("DustID: " + dustID);
                                bool square = reader.ReadBoolean();
                                //Main.NewText("Square: " + square);
                                bool queued = reader.ReadBoolean();
                                //Main.NewText("Queued: " + queued);

                                if (queued)
                                {
                                    //Main.NewText("Recieved queued event");
                                }
                                if (center.Y < 2000)
                                {
                                    //Main.NewText("Recieved broken centerpoint " + center.Y);
                                }

                                tsorcScriptedEvents.NetworkEvents.Add(new NetworkEvent(center, radius, dustID, square, queued));
                            }
                        }
                        break;
                    }

                //Sync time change
                case tsorcPacketID.SyncTimeChange:
                    {
                        Main.dayTime = reader.ReadBoolean();
                        Main.time = reader.ReadInt32();

                        if (Main.dayTime)
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.CosmicWatch.Day"), Color.Orange);
                        }
                        else
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("Items.CosmicWatch.Night"), new Color(175, 75, 255));
                        }

                        //Sync it to clients
                        NetMessage.SendData(MessageID.WorldData);

                        break;
                    }

                case tsorcPacketID.DispelShadow:
                    {
                        int npcID = reader.ReadInt32();
                        Main.npc[npcID].AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
                        break;
                    }

                case tsorcPacketID.DropSouls:
                    {
                        Vector2 position = reader.ReadVector2();
                        int count = reader.ReadInt32();
                        if (Main.netMode == NetmodeID.Server)
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("World.DropSouls1") + count + LangUtils.GetTextValue("World.DropSouls2"));
                            //You can not drop items in a stack larger than 32766 in multiplayer, because the stack size gets converted to a short when syncing
                            while (count > 32000)
                            {
                                //UsefulFunctions.ServerText("Dropping " + 32000 + "souls");
                                Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), position + Main.rand.NextVector2Circular(10, 10), ModContent.ItemType<DarkSoul>(), 32000);
                                count -= 32000;
                            }

                            Item.NewItem(new EntitySource_Misc("¯\\_(ツ)_/¯"), position, ModContent.ItemType<DarkSoul>(), count);
                            //UsefulFunctions.NewItemInstanced(position, new Vector2(1, 1), ModContent.ItemType<Items.DarkSoul>(), count);
                        }
                        break;
                    }

                case tsorcPacketID.SyncPlayerDodgeroll:
                    {
                        //First, check whether this is a new packet originating from a client who just rolled, or a packet that has been bounced by the server to the other clients
                        bool bounced = reader.ReadBoolean();
                        byte who = reader.ReadByte();

                        //If we're a client, go ahead and sync based on it. If we're the server, only sync and bounce it if this is a new packet
                        if (Main.netMode == NetmodeID.MultiplayerClient || !bounced)
                        {
                            //Sync everything
                            Player player = Main.player[who];
                            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                            modPlayer.forceDodgeroll = true;
                            modPlayer.wantedDodgerollDir = reader.ReadSByte();
                            player.velocity = reader.ReadVector2();

                            //If we're the server in specific, bounce it to the other clients, passing "true" as the bounced flag to ensure this only happens once
                            if (Main.netMode == NetmodeID.Server)
                            {
                                ModPacket rollPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                                rollPacket.Write(tsorcPacketID.SyncPlayerDodgeroll);
                                rollPacket.Write(true);
                                rollPacket.Write((byte)player.whoAmI);
                                rollPacket.Write(modPlayer.wantedDodgerollDir);
                                rollPacket.WriteVector2(player.velocity);

                                //Iterate through all active clients and send it specifically to them
                                for (int i = 0; i < Main.maxPlayers; i++)
                                {
                                    if (Main.player[i].active && i != player.whoAmI)
                                    {
                                        rollPacket.Send(i);
                                    }
                                }
                            }
                        }
                        break;
                    }
                case tsorcPacketID.SyncBonfire:
                    {
                        if (tsorcRevampWorld.LitBonfireList == null)
                        {
                            tsorcRevampWorld.LitBonfireList = new List<Vector2>();
                        }

                        Vector2 bonfireLocation = reader.ReadVector2();
                        if (!tsorcRevampWorld.LitBonfireList.Contains(bonfireLocation))
                        {
                            tsorcRevampWorld.LitBonfireList.Add(bonfireLocation);
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.WorldData);
                        }
                        break;
                    }
                case tsorcPacketID.SpawnNPC:
                    {
                        int npcID = reader.ReadInt32();
                        Vector2 npcLocation = reader.ReadVector2();

                        int Spawned = NPC.NewNPC(null, (int)npcLocation.X, (int)npcLocation.Y, npcID, 0);
                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                        }
                        break;
                    }
                case tsorcPacketID.SyncNPCExtras:
                    {
                        int npcIndex = reader.ReadInt32();
                        Main.npc[npcIndex].lifeMax = reader.ReadInt32();
                        Main.npc[npcIndex].defense = reader.ReadInt32();
                        Main.npc[npcIndex].damage = reader.ReadInt32();
                        Main.npc[npcIndex].value = reader.ReadInt32();
                        break;
                    }

                case tsorcPacketID.SyncMasterScroll:
                    {
                        Main.GameMode = reader.ReadInt32();
                        NetMessage.SendData(MessageID.WorldData);
                        break;
                    }

                case tsorcPacketID.SyncMinionRadius:
                    {
                        byte player = reader.ReadByte(); //player.whoAmI
                        tsorcRevampPlayer modPlayer = Main.player[player].GetModPlayer<tsorcRevampPlayer>();
                        modPlayer.MinionCircleRadius = reader.ReadSingle();
                        modPlayer.InterstellarBoost = reader.ReadBoolean();

                        //If the server recieved this from a client, then forward it to all the other clients
                        if (Main.netMode == NetmodeID.Server)
                        {
                            ModPacket minionPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                            minionPacket.Write(tsorcPacketID.SyncMinionRadius);
                            minionPacket.Write(player);
                            minionPacket.Write(modPlayer.MinionCircleRadius);
                            minionPacket.Write(modPlayer.InterstellarBoost);
                            minionPacket.Send();
                        }
                        break;
                    }
                case tsorcPacketID.TeleportAllPlayers:
                    {
                        Vector2 targetLocation = reader.ReadVector2();
                        for (int i = 0; i < Main.maxPlayers; i++)
                        {
                            if (Main.player[i].active && !Main.player[i].dead)
                            {
                                Main.player[i].Teleport(targetLocation);
                                Main.player[i].fallStart = (int)Main.player[i].Center.Y;
                                NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, i, targetLocation.X, targetLocation.Y, 0);
                            }
                        }
                        break;
                    }
                case tsorcPacketID.DeleteNPC:
                    {
                        Main.npc[reader.ReadInt32()].active = false;
                        break;
                    }

                case tsorcPacketID.SyncCurse:
                    {
                        byte player = reader.ReadByte(); //player.whoAmI
                        tsorcRevampPlayer modPlayer = Main.player[player].GetModPlayer<tsorcRevampPlayer>();
                        //modPlayer.cursePoints = reader.ReadInt32();
                        break;
                    }

                default:
                    {
                        Logger.InfoFormat("[tsorcRevamp] Sync failed. Unknown message ID: {0}", message);
                        break;
                    }
            }
        }

        public override object Call(params object[] args)
        {
            return base.Call(args);
        }

        public override void PostAddRecipes()/* tModPorter Note: Removed. Use ModSystem.PostAddRecipes */
        {
            tsorcGlobalItem.populateSoulRecipes();
        }
        public override void PostSetupContent()
        {
            #region Summoners Association Compatibility

            if (ModLoader.TryGetMod("SummonersAssociation", out Mod summonersAssociation))
            {
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<PhoenixEgg>(),
                    ModContent.BuffType<PhoenixBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<PhoenixProjectile>(),
                        ["Slot"] = 2f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<SunsetQuasar>(),
                    ModContent.BuffType<SunsetQuasarBuff>(),
                    ModContent.ProjectileType<SunsetQuasarMinion>()
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<ScorchingPoint>(),
                    ModContent.BuffType<CenterOfTheHeat>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<ScorchingPointFireball>(),
                        ["Slot"] = 0.5f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<InterstellarVesselGauntlet>(),
                    ModContent.BuffType<InterstellarCommander>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<InterstellarVesselShip>(),
                        ["Slot"] = 0.5f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<CenterOfTheUniverse>(),
                    ModContent.BuffType<CenterOfTheUniverseBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<CenterOfTheUniverseStar>(),
                        ["Slot"] = 0.5f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<TetsujinRemote>(),
                    ModContent.BuffType<TetsujinBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<TetsujinProjectile>(),
                        ["Slot"] = 2f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<NullSpriteStaff>(),
                    ModContent.BuffType<NullSpriteBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<NullSprite>(),
                        ["Slot"] = 0.75f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<BeetleIdol>(),
                    ModContent.BuffType<SamuraiBeetleBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<SamuraiBeetleProjectile>(),
                        ["Slot"] = 6f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<PeculiarSphere>(),
                    ModContent.BuffType<NondescriptOwlBuff>(),
                    ModContent.ProjectileType<NondescriptOwlProjectile>()
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<SpiritBell>(),
                    ModContent.BuffType<SpiritAshKnightBuff>(),
                    ModContent.ProjectileType<SpiritAshKnightMinion>()
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<EtherianWyvernStaff>(),
                    ModContent.BuffType<EtherianWyvernBuff>(),
                    ModContent.ProjectileType<EtherianWyvernProjectile>()
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<PhotonicDownpour>(),
                    ModContent.BuffType<PhotonicDownpourBuff>(),
                    new List<int>
                    {
                        ModContent.ProjectileType<PhotonicDownpourLaserDrone>(),
                        ModContent.ProjectileType<PhotonicDownpourDefenseDrone>(),
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<ShatteredReflection>(),
                    ModContent.BuffType<ShatteredReflectionBuff>(),
                    new Dictionary<string, object>()
                    {
                        ["ProjID"] = ModContent.ProjectileType<ShatteredReflectionProjectile>(),
                        ["Slot"] = 2f,
                    }
                    );
                summonersAssociation.Call(
                    "AddMinionInfo",
                    ModContent.ItemType<TripleThreat>(),
                    ModContent.BuffType<TripleThreatBuff>(),
                    new List<int>
                    {
                        ModContent.ProjectileType<FriendlyRetinazer>(),
                        ModContent.ProjectileType<FriendlySpazmatism>(),
                        ModContent.ProjectileType<FriendlyCataluminance>(),
                    }
                    );
            }

            #endregion

            #region Boss Checklist Compatibility
            Mod bossChecklist;
            if (ModLoader.TryGetMod("BossChecklist", out bossChecklist))
            {  //See https://github.com/JavidPack/BossChecklist/wiki/Support-using-Mod-Call for instructions


                // AddBoss, bossname, order or value in terms of vanilla bosses, inline method for retrieving downed value.
                /*
                public const float SlimeKing = 1f;
                public const float TorchGod = 1.5f;
                public const float EyeOfCthulhu = 2f;
                public const float BloodMoon = 2.5f
                public const float EaterOfWorlds = 3f;
                public const float GoblinArmy = 3.33f
                public const float OldOnesArmy = 3.66f
                public const float DarkMage = 3.67f
                public const float QueenBee = 4f;
                public const float Skeletron = 5f;
                public const float Deerclops = 6f;
                public const float WallOfFlesh = 7f;
                public const float FrostLegion = 7.33f
                public const float PirateInvasion = 7.66f
                public const float FlyingDutchman = 7.67f
                public const float QueenSlime = 8f
                public const float TheTwins = 9f
                public const float TheDestroyer = 10f;
                public const float SkeletronPrime = 11f;
                public const float Ogre = 11.01f
                public const float Eclipse = 11.5f
                public const float Plantera = 12f;
                public const float Golem = 13f;
                public const float PumpkinMoon = 13.25f
                public const float MourningWood = 13.26f
                public const float Pumpking = 13.27f
                public const float FrostMoon = 13.5f
                public const float Everscream = 13.51f
                public const float Santa-NK1 = 13.52f
                public const float IceQueen = 13.53f
                public const float MartianMadness = 13.75f
                public const float MartianSaucer = 13.76f
                public const float DukeFishron = 14f;
                public const float EmpressOfLight = 15f;
                public const float Betsy = 16f;
                public const float LunaticCultist = 17f;
                public const float LunarEvent = 17.01f;
                public const float Moonlord = 18f;*/
                var SlograAndGaibonPortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/SlograAndGaibon_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };
                var JungleWyvernPortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/JungleWyvern_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };
                var WyvernMagePortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/WyvernMage_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };
                var SerrisPortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/Serris_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };
                var HellkiteDragonPortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/HellkiteDragon_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };
                var SeathPortrait = (SpriteBatch sb, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/Seath_Portrait").Value;
                    Vector2 centered = new Vector2(rect.X + (rect.Width / 2) - (texture.Width / 2), rect.Y + (rect.Height / 2) - (texture.Height / 2));
                    sb.Draw(texture, centered, color);
                };


                // PRE-HM
                bossChecklist.Call(
                    "LogMiniBoss",
                    this,
                    nameof(LeonhardPhase1),
                    2.01f, // Tier (look above, for determining where it will display)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<LeonhardPhase1>())),
                    ModContent.NPCType<LeonhardPhase1>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.LeonhardPhase1.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.LeonhardDesc"),
                        ["collectibles"] = ModContent.ItemType<Items.Weapons.Melee.ShatteredMoonlight>(),
                        ["overrideHeadTextures"] = "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/LeonhardPhase1_Head_Boss"
                    }
                    );


                bossChecklist.Call(
                    "LogMiniBoss",
                    this,
                    nameof(Pinwheel),
                    3.1f, // Tier (look above, for determining where it will display)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Pinwheel>())),
                    ModContent.NPCType<Pinwheel>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.Pinwheel.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.Pinwheel"),
                    }
                    );


                bossChecklist.Call(
                    "LogMiniBoss",
                    this,
                    nameof(AncientOolacileDemon),
                    3.9f, // Tier (look above, for determining where it will display)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AncientOolacileDemon>())),
                    ModContent.NPCType<AncientOolacileDemon>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.AncientOolacileDemon.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.AncientOolacileDemonDesc"),
                        ["overrideHeadTextures"] = "tsorcRevamp/NPCs/Bosses/AncientOolacileDemon_Head_Boss"
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Slogra),
                    4.1f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Slogra>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<Slogra>(), ModContent.NPCType<Gaibon>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.SlograAndGaibonName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.SlograAndGaibonDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.TomeOfSlograAndGaibon>(),
                        ["overrideHeadTextures"] = "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/SlograAndGaibon_Head_Boss",
                        ["customPortrait"] = SlograAndGaibonPortrait
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(JungleWyvernHead),
                    6.1f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<JungleWyvernHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<JungleWyvernHead>(), ModContent.NPCType<JungleWyvernBody>(), ModContent.NPCType<JungleWyvernBody2>(), ModContent.NPCType<JungleWyvernBody3>(), ModContent.NPCType<JungleWyvernLegs>(), ModContent.NPCType<JungleWyvernTail>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.JungleWyvernHead.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.JungleWyvernDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.JungleFeather>(),
                        ["customPortrait"] = JungleWyvernPortrait
                    }
                    );


                bossChecklist.Call(
                    "LogMiniBoss",
                    this,
                    nameof(AncientDemon),
                    6.51f, // Tier (look above, for determining where it will display)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AncientDemon>())),
                    ModContent.NPCType<AncientDemon>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.AncientDemon.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.AncientDemonDesc"),
                        ["overrideHeadTextures"] = "tsorcRevamp/NPCs/Bosses/AncientDemon_Head_Boss"
                    }
                    );


                // HM


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(TheRage),
                    8.2f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<TheRage>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<TheRage>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.TheRage.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.TheRageDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.FieryEgg>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(WyvernMage),
                    8.4f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMage>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<WyvernMage>(), ModContent.NPCType<MechaDragonHead>(), ModContent.NPCType<MechaDragonBody>(), ModContent.NPCType<MechaDragonBody2>(), ModContent.NPCType<MechaDragonBody3>(), ModContent.NPCType<MechaDragonLegs>(), ModContent.NPCType<MechaDragonTail>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.WyvernMage.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.WyvernMageDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.WingOfTheFallen>(),
                        ["overrideHeadTextures"] = "tsorcRevamp/NPCs/Bosses/Boss Checklist Replacement Sprites/WyvernMageAndMechaDragon_Head_Boss",
                        ["customPortrait"] = WyvernMagePortrait
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(TheSorrow),
                    8.6f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<TheSorrow>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<TheSorrow>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.TheSorrow.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.TheSorrowDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.WateryEgg>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(TheHunter),
                    8.8f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<TheHunter>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<TheHunter>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.TheHunter.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.TheHunterDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.GrassyEgg>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(SerrisX),
                    14.5f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<SerrisX>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<SerrisX>(), ModContent.NPCType<SerrisHead>(), ModContent.NPCType<SerrisBody>(), ModContent.NPCType<SerrisTail>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.SerrisName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.SerrisDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.SerrisBait>(),
                        ["customPortrait"] = SerrisPortrait
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Death),
                    16.5f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Death>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Death>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.Death.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.DeathDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DeathBringer>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(BrokenOkiku),
                    17.001f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<BrokenOkiku>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<BrokenOkiku>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.AttraidiesName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.AttraidiesDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.MindCube>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Attraidies),
                    17.002f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Attraidies>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Attraidies>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.RealAttraidiesName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.RealAttraidiesDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>(),
                        ["availability"] = (Func<bool>)(() => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<BrokenOkiku>())))
                    }
                    );




                // SHM



                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(HellkiteDragonHead),
                    20.1f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<HellkiteDragonHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<HellkiteDragonHead>(), ModContent.NPCType<HellkiteDragonBody>(), ModContent.NPCType<HellkiteDragonBody2>(), ModContent.NPCType<HellkiteDragonBody3>(), ModContent.NPCType<HellkiteDragonLegs>(), ModContent.NPCType<HellkiteDragonTail>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.HellkiteDragonHead.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.HellkiteDragonDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.HellkiteStone>(),
                        ["customPortrait"] = HellkiteDragonPortrait
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Blight),
                    20.2f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Blight>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Blight>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.BlightName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.BlightDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.BlightStone>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(EarthFiendLich),
                    20.3f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<EarthFiendLich>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<EarthFiendLich>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.EarthFiendLich.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.EarthFiendLichDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DyingEarthCrystal>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Witchking),
                    20.4f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Witchking>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Witchking>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.Witchking.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.WitchkingDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DarkMagicRing>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Artorias),
                    20.5f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Artorias>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Artorias>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.Artorias.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.ArtoriasDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.StrangeMagicRing>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(WaterFiendKraken),
                    20.6f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<WaterFiendKraken>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.WaterFiendKraken.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.WaterFiendKrakenDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DyingWaterCrystal>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(SeathTheScalelessHead),
                    20.7f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<SeathTheScalelessHead>())), // Downed variable (the one keeping track the boss has been defeated once)
                    new List<int>() { ModContent.NPCType<SeathTheScalelessHead>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessTail>(), ModContent.NPCType<PrimordialCrystal>() },
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.SeathTheScalelessHead.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.SeathTheScalelessDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.StoneOfSeath>(),
                        ["customPortrait"] = SeathPortrait
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(AbysmalOolacileSorcerer),
                    20.8f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<AbysmalOolacileSorcerer>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<AbysmalOolacileSorcerer>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.AbysmalOolacileSorcerer.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.AbysmalOolacileSorcererDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.AbysmalStone>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(FireFiendMarilith),
                    20.9f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<FireFiendMarilith>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.FireFiendMarilith.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.FireFiendMarilithDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DyingFireCrystal>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(WyvernMageShadow),
                    20.91f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WyvernMageShadow>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<WyvernMageShadow>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.WyvernMageShadow.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.WyvernMageShadowDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.WingOfTheGhostWyvern>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Chaos),
                    20.92f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Chaos>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Chaos>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.NPCs.Chaos.DisplayName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.ChaosDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DyingWindCrystal>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(DarkCloud),
                    21f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<DarkCloud>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<DarkCloud>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.DarkCloudName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.DarkCloudDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.DarkMirror>()
                    }
                    );


                bossChecklist.Call(
                    "LogBoss", // Name of the call
                    this,
                    nameof(Gwyn),
                    22f, // Tier (look above)
                    () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Gwyn>())), // Downed variable (the one keeping track the boss has been defeated once)
                    ModContent.NPCType<Gwyn>(),
                    new Dictionary<string, object>()
                    {
                        ["displayName"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.GwynName"),
                        ["spawnInfo"] = Language.GetText("Mods.tsorcRevamp.BossChecklist.GwynDesc"),
                        ["spawnItems"] = ModContent.ItemType<Items.BossItems.LostScrollOfGwyn>()
                    }
                    );


            }
            #endregion
            //--------
            #region Magic Storage Compatability
            List<int> toDisable = new List<int>();
            Mod magicStorage;
            bool magicStorageUsed = ModLoader.TryGetMod("MagicStorageExtra", out magicStorage);
            if (!magicStorageUsed)
            {
                magicStorageUsed = ModLoader.TryGetMod("MagicStorage", out magicStorage);
            }
            if (magicStorageUsed)
            {
                toDisable.Add(magicStorage.Find<ModTile>("CraftingAccess").Type);
                toDisable.Add(magicStorage.Find<ModTile>("RemoteAccess").Type);
                toDisable.Add(magicStorage.Find<ModTile>("StorageAccess").Type);
                toDisable.Add(magicStorage.Find<ModTile>("StorageComponent").Type);
                toDisable.Add(magicStorage.Find<ModTile>("StorageHeart").Type);
                toDisable.Add(magicStorage.Find<ModTile>("StorageUnit").Type);
                toDisable.Add(magicStorage.Find<ModTile>("StorageConnector").Type);
            }
            foreach (var tileID in toDisable)
            {
                Main.tileSolidTop[tileID] = false;
            }
            #endregion
        }

        internal async void UpdateCheck()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData";
            string changelogPath = dataDir + separator + "tsorcChangelog.txt"; //Downloaded changelog from the github
            string musicTempPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to
            string mapBasePath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcBaseMap.wld"; //Where the map template is downloaded to

            //Check if the data directory exists, if not then create it
            if (!Directory.Exists(dataDir))
            {
                CreateDataDirectory();
            }

            //If it finds a music mod in the data folder, do the second phase of loading it
            if (File.Exists(musicTempPath))
            {
                FileInfo musicModFileInfo = new FileInfo(musicTempPath);


                if (IsMusicInvalid(musicTempPath))
                {
                    //System.Windows.Forms.MessageBox.Show("The Story of Red Cloud failed to download the music mod automatically!\nYou must download it manually from our discord instead: https://discord.gg/UGE6Mstrgz", "TSORC: Music Mod Download Failure!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    try
                    {
                        musicModFileInfo.Delete();
                        musicModDownloadFailures++;
                        ModContent.GetInstance<tsorcRevamp>().WriteVersionInfo("", "000000");
                    }
                    catch
                    {
                        //do nothing
                    }
                }
                else
                {
                    InstallMusicMod();
                }
            }

            //If the template file doesn't exist or fails to load for whatever reason, flag its update id to 0 to force a download
            if (IsMapInvalid(mapBasePath))
            {
                ModContent.GetInstance<tsorcRevamp>().WriteVersionInfo("000000", "");
            }

            try
            {
                using StreamReader reader = await GetChangelogAsync();

                //If it's null, that means the changelog download failed
                if (reader == null)
                {
                    throw new Exception("Failed to download changelog");
                }

                //If it exists, read from it. If not, put a warning in the log that it failed to download.
                if (File.Exists(changelogPath))
                {
                    string mapString = "0000000000";
                    string musicString = "0000000000";

                    //Pull the version numbers from the file

                    string currentString = "";

                    while ((currentString = reader.ReadLine()) != null)
                    {
                        currentString = currentString.ToUpper(); //Convert it all to uppercase so we don't have to worry about case
                        if (currentString.Contains("UNRELEASED")) //Ignore strings with UNRELEASED in their name
                        {
                            continue;
                        }

                        if (currentString.Contains("MAP ") && mapString == "0000000000") //Store the first line with the word MAP in it here
                        {
                            mapString = currentString;
                        }
                        if (currentString.Contains("MUSIC ") && musicString == "0000000000") //Store the first line with the word MUSIC in it here
                        {
                            musicString = currentString;
                        }

                        if (mapString != "0000000000" && musicString != "0000000000") //If both the music and map 
                        {
                            break;
                        }
                    }

                    if (mapString == "0000000000" || musicString == "0000000000")
                    {
                        Logger.Warn("WARNING: Failed to read version data from downloaded changelog! This will prevent the mod from downloading the map, music mod, or updates!");
                    }
                    else
                    {
                        //Simplify them
                        SimplifyVersionString(ref mapString);
                        SimplifyVersionString(ref musicString);

                        //Get the version info of the existing files
                        Tuple<int, int> versionInfo = ReadVersionInfo();

                        //If the current map template is a lower version than the new update, then update it
                        if (versionInfo.Item1 < Int32.Parse(mapString))
                        {
                            newMapUpdateString = mapString;
                            MapDownload();
                        }

                        //If the music one is less, then flag it as needing an update so the UI can ask the user if they want to download it
                        //It only actually writes the version info after the music mod has been successfully updated (justUpdatedMusic)
                        if (versionInfo.Item2 < Int32.Parse(musicString))
                        {
                            if (justUpdatedMusic)
                            {
                                WriteVersionInfo("", musicString);
                            }
                            else
                            {
                                MusicNeedsUpdate = true; //Not setting music current version just yet, that happens once the file is *actually* downloaded
                            }
                        }
                    }
                }
                else
                {
                    Logger.Warn("Failed to download or open changelog.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to download changelog.");
                Logger.Error(e);
            }

            //TryCopyMap();
        }

        //Returns a tuple containing the version info for the map and music mod, respectively
        public Tuple<int, int> ReadVersionInfo()
        {
            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData";
            string curVersionPath = dataDir + separator + "tsorcCurrentVer.txt"; //Stored file recording current map and music mod versions
            string[] curVersionFile = File.ReadAllLines(curVersionPath);

            return new Tuple<int, int>(Int32.Parse(curVersionFile[0]), Int32.Parse(curVersionFile[1]));
        }

        //Writes the version info of the map and music to the file. Passing "" means that will not be overwritten.
        public void WriteVersionInfo(string mapVersion, string musicVersion)
        {
            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData";
            string curVersionPath = dataDir + separator + "tsorcCurrentVer.txt"; //Stored file recording current map and music mod versions

            //If no stored version file exists, create it with version 000000
            if (!File.Exists(curVersionPath))
            {
                using (StreamWriter versionFile = new StreamWriter(curVersionPath))
                {
                    versionFile.WriteLine("000000");
                    versionFile.WriteLine("000000");
                }
            }

            //If it's less than the current map string, download the new one and update the stored version.
            string[] curVersionFile = File.ReadAllLines(curVersionPath);

            if (mapVersion != "")
            {
                curVersionFile[0] = mapVersion;
            }
            if (musicVersion != "")
            {
                curVersionFile[1] = musicVersion;
            }

            File.WriteAllLines(curVersionPath, curVersionFile);
        }

        public void SimplifyVersionString(ref string s)
        {
            string output = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                {
                    output += s[i];
                }
            }

            //Append 0's to both strings to make them fixed-length, so that different sized version numbers are all read the same
            int initialLength = output.Length;
            for (int i = 0; i < 10 - initialLength; i++)
            {
                output += "0";
            }

            s = output;
        }

        public bool IsMapInvalid(string path)
        {
            if (!File.Exists(path))
            {
                return true;
            }
            else
            {
                Terraria.IO.WorldFileData worldData = Terraria.IO.WorldFile.GetAllMetadata(path, false);
                if (worldData == null || !worldData.IsValid)
                {
                    return true;
                }
            }

            return false;
        }

        public void MapDownload()
        {
            char separator = Path.DirectorySeparatorChar;
            string filePath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcBaseMapDownload.wld";
            string jsonPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcSoapstones.json";
            if (File.Exists(jsonPath)) File.Delete(jsonPath);

            Logger.Info("Attempting to download updated world template.");
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFileAsync(new Uri(VariousConstants.MAP_URL), filePath);
                    client.DownloadProgressChanged += MapDownloadProgressChanged;
                    client.DownloadFileCompleted += TryCopyMap;
                }
            }
            catch (WebException e)
            {
                Logger.Warn("Automatic world download failed ({0}). Connection to the internet failed or the file's location has changed.", e);
            }
            catch (Exception e)
            {
                Logger.Warn("Automatic world download failed ({0}).", e);
            }
            return;
        }

        public static void MapDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs downloadEvent)
        {
            MapDownloadProgress = downloadEvent.BytesReceived;
            MapDownloadTotalBytes = downloadEvent.TotalBytesToReceive;
        }


        //Checks if there is already a copy of the adventure map in the Worlds folder, and if not automatically copies one there.
        public void TryCopyMap(object sender = null, AsyncCompletedEventArgs downloadEvent = null)
        {
            char separator = Path.DirectorySeparatorChar;
            string userMapFileName = separator + "TheStoryofRedCloud.wld";
            string worldsFolder = Main.SavePath + separator + "Worlds";
            string dataDir = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData";
            string baseMapFileName = separator + "tsorcBaseMap.wld";
            string newMapFileName = separator + "tsorcBaseMapDownload.wld";

            FileInfo fileToCopy = new FileInfo(dataDir + newMapFileName);
            DirectoryInfo worlds = new DirectoryInfo(worldsFolder);
            bool worldExists = false;
            log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;

            //Check if the new file exists and is not too small (indicates corruption or failed download)
            if (IsMapInvalid(dataDir + newMapFileName))
            {
                fileToCopy.Delete();

                worldDownloadFailures++;
                if (worldDownloadFailures < 3)
                {
                    ((tsorcRevamp)ModLoader.GetMod("tsorcRevamp")).MapDownload();
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("The Story of Red Cloud failed to download the custom map automatically!\nYou must download it manually from our discord instead: https://discord.gg/UGE6Mstrgz", "TSORC: Map Download Failure!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }
            else
            {
                //If it exists and is valid, then overwrite the old map tempate with it
                fileToCopy.CopyTo(dataDir + baseMapFileName, true);

                //Delete the temporary file
                fileToCopy.Delete();

                //Update version info
                WriteVersionInfo(newMapUpdateString, "");
            }

            //Then check if there is a copy of the adventure map in the players worlds folder, and if not auto-copy one there
            if (!Directory.Exists(worldsFolder))
            {
                try
                {
                    Directory.CreateDirectory(worldsFolder);
                }
                catch (UnauthorizedAccessException e)
                {
                    thisLogger.Error("World directory creation failed ({0}). Try again with administrator privileges?", e);
                }
                catch (Exception e)
                {
                    thisLogger.Error("World directory creation failed ({0}).", e);
                }
            }

            foreach (FileInfo file in worlds.GetFiles("*.wld"))
            {
                if (file.FullName.Contains("TheStoryofRedCloud"))
                {
                    worldExists = true;
                    break;
                }
            }

            if (!worldExists && File.Exists(dataDir + baseMapFileName))
            {
                thisLogger.Info("Attempting to copy world.");
                try
                {
                    fileToCopy.CopyTo(worldsFolder + userMapFileName, false);
                }
                catch (System.Security.SecurityException e)
                {
                    thisLogger.Error("World copy failed ({0}). Try again with administrator privileges?", e);
                }
                catch (Exception e)
                {
                    thisLogger.Error("World copy failed ({0}).", e);
                }
            }
        }

        public bool IsMusicInvalid(string path)
        {
            //Try opening the file. If it fails, return false.
            try
            {
                FileStream musicModStream = File.OpenRead(path);
                BinaryReader reader = new BinaryReader(musicModStream);

                //Read from the file until we reach the point inside it where its hash is stored. Then keep reading a bit more, to reach the point where we can compute it ourselves, and do so.
                //If they don't match, then it is corrupt or incomplete.
                _ = reader.ReadBytes(4);
                _ = reader.ReadString();
                byte[] hash = reader.ReadBytes(20);
                _ = reader.ReadBytes(256);
                _ = reader.ReadInt32();
                byte[] computedHash = SHA1.Create().ComputeHash(musicModStream);
                bool mismatchFound = false;
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (hash[i] != computedHash[i])
                    {
                        mismatchFound = true;
                        break;
                    }
                }

                musicModStream.Close();

                if (mismatchFound)
                {
                    Logger.Warn("Hash mismatch on downloaded music mod file. File corrupt or incomplete.");
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                Logger.Warn("Failure opening downloaded music mod file. File corrupt or incomplete");
                return true;
            }
        }
        public static void MusicDownload()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;
            char separator = Path.DirectorySeparatorChar;
            string musicTempPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to

            thisLogger.Info("Attempting to download music file.");
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += MusicDownloadProgressChanged;
                    client.DownloadFileCompleted += MusicDownloadCompleted;
                    client.DownloadFileAsync(new Uri(VariousConstants.MUSIC_MOD_URL), musicTempPath);

                    DownloadingMusic = true;
                }
            }
            catch (WebException e)
            {
                thisLogger.Warn("Automatic music download failed ({0}). Connection to the internet failed or the file's location has changed.", e);
            }
            catch (Exception e)
            {
                thisLogger.Warn("Automatic world download failed ({0}).", e);
            }
        }

        public async Task<StreamReader> GetChangelogAsync()
        {
            char separator = Path.DirectorySeparatorChar;
            string changelogPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcChangelog.txt";
            log4net.ILog thisLogger = ModLoader.GetMod("tsorcRevamp").Logger;

            Logger.Info("Attempting to download changelog.");
            if (File.Exists(changelogPath))
            {
                File.Delete(changelogPath);
            }

            try
            {
                using HttpClient client = new();
                using var netstream = await client.GetStreamAsync(new Uri(VariousConstants.CHANGELOG_URL));
                using var fs = new FileStream(changelogPath, FileMode.CreateNew);
                await netstream.CopyToAsync(fs);
            }
            catch (Exception e)
            {
                //at least it isnt log and throw
                Logger.InfoFormat("GetChangelogAsync threw error {0}", e);
            }
            //NOT a using statement, because if we discard it here it wont be available later
            StreamReader r = null;
            if (File.Exists(changelogPath))
            {
                r = File.OpenText(changelogPath);
            }
            else
            {
                Logger.Error("Failed to check for map or music mod updates");
            }

            return r;
        }

        public void CreateDataDirectory()
        {
            char separator = Path.DirectorySeparatorChar;
            string dataDir = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData";
            Logger.Info("Directory " + dataDir + " not found. Creating directory.");
            try
            {
                Directory.CreateDirectory(dataDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Error("Data directory creation failed ({0}). Try again with administrator privileges?", e);
            }
            catch (Exception e)
            {
                Logger.Error("Data directory creation failed ({0}).", e);
            }
        }

        public static void MusicDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs downloadEvent)
        {
            MusicDownloadProgress = downloadEvent.ProgressPercentage;
        }

        public static void MusicDownloadCompleted(object sender, AsyncCompletedEventArgs downloadEvent)
        {
            char separator = Path.DirectorySeparatorChar;
            string musicTempPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to
            FileInfo musicModFileInfo = new FileInfo(musicTempPath);
            if (((tsorcRevamp)ModLoader.GetMod("tsorcRevamp")).IsMusicInvalid(musicTempPath))
            {
                //System.Windows.Forms.MessageBox.Show("Failed to download the music mod automatically!\nYou may need to download it manually from our discord instead: https://discord.gg/UGE6Mstrgz", "Music Mod Download Failure!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                musicModFileInfo.Delete();
                tsorcRevamp.musicModDownloadFailures++;
                ModContent.GetInstance<tsorcRevamp>().WriteVersionInfo("", "000000");
            }
            else
            {
                MusicDownloadProgress = 0;
                DownloadingMusic = false;
                DisableMusicAndReload();
            }
        }

        //Performs the tasks necessary to replace the old music mod file with the newly downloaded one
        public static void InstallMusicMod()
        {
            char separator = Path.DirectorySeparatorChar;
            string musicTempPath = Main.SavePath + separator + "ModConfigs" + separator + "tsorcRevampData" + separator + "tsorcMusic.tmod"; //Where the music mod is downloaded to
            string musicFinalPath = Main.SavePath + separator + "Mods" + separator + "tsorcMusic.tmod"; //Where the music mod should be moved to upon reload
            string configPath = Main.SavePath + separator + "Mods" + separator + "enabled.json"; //Where the config dedicing what mods to load is

            //First, check if the music mod is still enabled
            bool musicLoaded = false;


            if (ModLoader.TryGetMod("tsorcMusic", out _))
            {
                musicLoaded = true;
            }


            //If so, do not move it. Instead enable a special flag to prompt a reload once the initial load finishes (can't do it here while we're mid-load, or an error would occur)
            if (musicLoaded)
            {
                SpecialReloadNeeded = true;
            }

            //If not, then it's safe to delete the old one and move the new one in
            //Also, it flags music as updated so that the stored version file gets updated later
            //One more reload is required for enabling to take effect
            else
            {
                if (File.Exists(musicFinalPath))
                {
                    try
                    {
                        File.Delete(musicFinalPath);
                        File.Move(musicTempPath, musicFinalPath);
                        justUpdatedMusic = true;
                        ReloadNeeded = true;
                        tsorcRevamp.SpecialReloadNeeded = false;
                    }
                    catch
                    {
                        System.Windows.Forms.MessageBox.Show("Restarting to finish updating music mod!\nThis is totally normal, just hit OK and re-launch!", "Restarting!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Main.WeGameRequireExitGame();
                    }
                }
                else
                {
                    File.Move(musicTempPath, musicFinalPath);
                    justUpdatedMusic = true;
                    ReloadNeeded = true;
                }

            }
        }

        //Uses reflection to disable the music mod and force a reload
        public static void DisableMusicAndReload()
        {
            object[] modParam = new object[1] { "tsorcMusic" };
            typeof(ModLoader).GetMethod("DisableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
            typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
        }


        //Adds the music mod to the enabled config and forces a reload
        public static void EnableMusicAndReload()
        {
            object[] modParam = new object[1] { "tsorcMusic" };
            typeof(ModLoader).GetMethod("EnableMod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, modParam);
            typeof(ModLoader).GetMethod("Reload", BindingFlags.NonPublic | BindingFlags.Static).Invoke(default, new object[] { });
            ReloadNeeded = false;
            MusicNeedsUpdate = false;
        }
    }




    public class tsorcPacketID
    {
        //Bytes because packets use bytes
        public const byte SyncSoulSlot = 1;
        public const byte SyncEventDust = 2;
        public const byte SyncTimeChange = 3;
        public const byte DispelShadow = 4;
        public const byte DropSouls = 5;
        public const byte SyncPlayerDodgeroll = 6;
        public const byte SyncBonfire = 7;
        public const byte SpawnNPC = 8;
        public const byte SyncNPCExtras = 9;
        public const byte SyncMasterScroll = 10;
        public const byte SyncMinionRadius = 11;
        /// <summary>
        /// Teleport all players to the specified vector2
        /// </summary>
        public const byte TeleportAllPlayers = 12;
        /// <summary>
        /// Just deletes the NPC whose index is specified.
        /// </summary>
        public const byte DeleteNPC = 13;

        /// <summary>
        /// Syncs the curse points of a player
        /// </summary>
        public const byte SyncCurse = 14;
    }

    //config moved to separate file

    public class TilePlaceCode : GlobalItem
    {

        public override bool CanUseItem(Item item, Player player)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (item.createWall > 0)
                {
                    return false; //prevent placing walls
                }
                if (item.createTile > -1)
                {

                    if (tsorcRevamp.PlaceAllowed.Contains(item.createTile))
                    {
                        return true; //allow placing tiles in PlaceAllowed
                    }

                    else if (tsorcRevamp.CrossModTiles.Contains(item.createTile))
                    {
                        return true; //allow placing certain tiles from other mods
                    }

                    else if (tsorcRevamp.PlaceAllowedModTiles.Contains(item.createTile))
                    {
                        return true; //allow placing certain tiles from this mod
                    }

                    else return false; //disallow using item if it places other tiles
                }
                return true; //allow using items if they do not create tiles
            }
            return base.CanUseItem(item, player); //use default value
        }
    }

    public class TileKillCode : GlobalTile
    {

        public override bool CanKillTile(int x, int y, int type, ref bool blockDamaged)
        {
            if (tsorcRevamp.ActuationBypassActive)
            {
                return true;
            }

            if (Main.tile[x, y - 1].TileType == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {

                if (Main.tile[x, y - 1].TileType == TileID.Statues)
                {
                    return false;
                }

                bool right = !Main.tile[x + 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x + 1, y].TileType);
                bool left = !Main.tile[x - 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x - 1, y].TileType);
                bool below = !Main.tile[x, y - 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y - 1].TileType);
                bool above = !Main.tile[x, y + 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y + 1].TileType);
                if (x < 10 || x > Main.maxTilesX - 10)
                {//sanity
                    return false;
                }
                else if (y < 10 || y > Main.maxTilesY - 10)
                {//sanity 
                    return false;
                }
                else if (Main.tile[x, y] == null)
                {//sanity
                    return false;
                }
                else if (tsorcRevamp.KillAllowed.Contains(type))
                {//always allow KillAllowed
                    return true;
                }
                else if (tsorcRevamp.CrossModTiles.Contains(type))
                {//allow breaking placeable modded tiles from other mods
                    return true;
                }
                else if (tsorcRevamp.PlaceAllowedModTiles.Contains(type))
                {//allow breaking placeable modded tiles
                    return true;
                }
                else if (tsorcRevamp.Unbreakable.Contains(type))
                {//always disallow Unbreakable	
                    return false;
                }
                else if (right && left)
                {//if a tile has no neighboring tiles horizontally, allow breaking
                    return true;
                }
                else if (below && above)
                {//if a tile has no neighboring tiles vertically, allow breaking
                    return true;
                }
                else return false; //disallow breaking tiles otherwise
            }

            return base.CanKillTile(x, y, type, ref blockDamaged); //use default value
        }

        public override bool CanExplode(int x, int y, int type)
        {

            if (Main.tile[x, y - 1].TileType == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                if (Main.tile[x, y - 1].TileType == TileID.Statues)
                {
                    return false;
                }
                bool right = !Main.tile[x + 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x + 1, y].TileType);
                bool left = !Main.tile[x - 1, y].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x - 1, y].TileType);
                bool below = !Main.tile[x, y - 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y - 1].TileType);
                bool above = !Main.tile[x, y + 1].HasTile || tsorcRevamp.IgnoredTiles.Contains(Main.tile[x, y + 1].TileType);
                bool CanDestroy = false;
                if (type == TileID.Ebonsand || type == TileID.Amethyst || type == TileID.ShadowOrbs)
                { //shadow temple / corruption chasm stuff that gets blown up
                    CanDestroy = true;
                }

                //check cankilltiles stuff
                if ((right && left) || (above && below) || tsorcRevamp.KillAllowed.Contains(type) || (x < 10 || x > Main.maxTilesX - 10) || (y < 10 || y > Main.maxTilesY - 10) || (!Main.tile[x, y].HasTile))
                {
                    CanDestroy = true;
                }
                if (Main.tileDungeon[Main.tile[x, y].TileType]
                    || type == TileID.Silver
                    || type == TileID.Cobalt
                    || type == TileID.Mythril
                    || type == TileID.Adamantite
                    || (tsorcRevamp.Unbreakable.Contains(type))
                )
                {
                    CanDestroy = false;
                }
                if (!Main.hardMode && type == TileID.Hellstone)
                {
                    CanDestroy = false;
                }
                return CanDestroy;
            }



            else return base.CanExplode(x, y, type);


        }

        public override bool Slope(int i, int j, int type)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            else return base.Slope(i, j, type);
        }
    }

    public class WallKillCode : GlobalWall
    {
        public override void KillWall(int i, int j, int type, ref bool fail)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                fail = true;
            }
        }
        public override bool CanExplode(int i, int j, int type)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return base.CanExplode(i, j, type);
        }
    }
    public class MiscGlobalTile : GlobalTile
    {

        bool vortexNotif = false;
        bool nebulaNotif = false;
        bool stardustNotif = false;
        bool solarNotif = false;

        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                Player player = Main.LocalPlayer;
                var pos = new Vector2(i + 0.5f, j); // the + .5f makes the effect reach from equal distance to left and right
                var distance = Math.Abs(Vector2.Distance(player.Center, (pos * 16)));

                if (Main.tile[i, j].TileType == TileID.LunarMonolith && distance <= 800f && !player.dead && Main.tile[i, j].TileFrameY > 54)
                { //frameY > 54 means enabled

                    int style = Main.tile[i, j].TileFrameX / 36;
                    switch (style)
                    {
                        case 0:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerVortex) && !NPC.downedTowerVortex)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerVortex, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveVortex = true;
                                    NPC.ShieldStrengthTowerVortex = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!vortexNotif)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("World.MonolithLocked"), Color.Teal);
                                        vortexNotif = true;
                                    }
                                }
                            }
                            break;

                        case 1:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerNebula) && !NPC.downedTowerNebula)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerNebula, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveNebula = true;
                                    NPC.ShieldStrengthTowerNebula = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!nebulaNotif)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("World.MonolithLocked"), Color.Pink);
                                        nebulaNotif = true;
                                    }
                                }
                            }
                            break;

                        case 2:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerStardust) && !NPC.downedTowerStardust)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerStardust, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveStardust = true;
                                    NPC.ShieldStrengthTowerStardust = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!stardustNotif)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("World.MonolithLocked"), Color.Cyan);
                                        stardustNotif = true;
                                    }
                                }
                            }
                            break;

                        case 3:
                            if (!NPC.AnyNPCs(NPCID.LunarTowerSolar) && !NPC.downedTowerSolar)
                            {
                                if (tsorcRevampWorld.SuperHardMode)
                                {
                                    int p = NPC.NewNPC(new EntitySource_Misc("¯\\_(ツ)_/¯"), (i * 16) + 8, (j * 16) - 64, NPCID.LunarTowerSolar, 1);
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, p);
                                    NPC.TowerActiveSolar = true;
                                    NPC.ShieldStrengthTowerSolar = NPC.ShieldStrengthTowerMax;
                                    NetMessage.SendData(MessageID.UpdateTowerShieldStrengths);
                                }
                                else
                                {
                                    if (!solarNotif)
                                    {
                                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("World.MonolithLocked"), Color.OrangeRed);
                                        solarNotif = true;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            base.NearbyEffects(i, j, type, closer);
        }
    }

    //tConfig played nice with partially transparent textures, tModloader doesn't. This class helps fix that
    //Handles premultiplication, allowing textures with alpha transparency to be displayed correctly
    //To add a texture, add an enum for it under TransparentTextureType, and a KeyValuePair for it in the TransparentTextures dictionary like the others below
    //You can then get your premultiplied Texture2D from it wherever it's needed via TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.(YourEnum)]; 
    public static class TransparentTextureHandler
    {
        public static Dictionary<TransparentTextureType, Texture2D> TransparentTextures;
        public enum TransparentTextureType
        {
            PhasedMatterBlast,
            AntiGravityBlast,
            EnemyPlasmaOrb,
            ManaShield,
            CrazedOrb,
            MasterBuster,
            AntiMaterialRound,
            GlaiveBeam,
            GlaiveBeamItemGlowmask,
            GlaiveBeamHeldGlowmask,
            GenericLaser,
            GenericLaserTargeting,
            DarkLaser,
            DarkLaserTargeting,
            PulsarGlowmask,
            GWPulsarGlowmask,
            PolarisGlowmask,
            ToxicCatalyzerGlowmask,
            VirulentCatalyzerGlowmask,
            BiohazardGlowmask,
            HealingElixirGlowmask,
            ShatteredMoonlightGlowmask,
            GreySlashGlowmask,
            DarkDivineSpark,
            UltimaWeapon,
            UltimaWeaponGlowmask,
            DarkUltimaWeapon,
            DarkUltimaWeaponGlowmask,
            ReflectionShift,
            PhazonRound,
            MoonlightGreatsword,
            MoonlightGreatswordGlowmask,
            EstusFlask,
            CeruleanFlask,
            ElfinArrow,
            ElfinTargeting,
            HumanityPhantom,
            BarbarousThornBladeGlowmask,
            RedLaser,
            RedLaserTransparent,
            LightrifleFire,
            Lightning,
            BulletHellLaser,
            HeavenPiercerGlowmask,
            SoapstoneMessage,
            CrystalRay,
            SeveringDuskGlowmask,
            Pinwheel,
            PinwheelFireglow,
        }

        //All textures with transparency will have to get run through this function to get premultiplied
        public static void TransparentTextureFix()
        {
            //Generates the dictionary of textures
            TransparentTextures = new Dictionary<TransparentTextureType, Texture2D>()
            {
                {TransparentTextureType.PhasedMatterBlast, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/PhasedMatterBlast", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.AntiGravityBlast, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/AntiGravityBlast", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.EnemyPlasmaOrb, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyPlasmaOrb", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ManaShield, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/ManaShield", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.CrazedOrb, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/CrazedOrb", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MasterBuster, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/MasterBuster", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.AntiMaterialRound, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/AntiMaterialRound", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeam, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GlaiveBeamLaser", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeamItemGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/GlaiveBeam_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GlaiveBeamHeldGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/GlaiveBeamHeld_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GenericLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GenericLaser", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GenericLaserTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GenericLaserTargeting", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/DarkLaser", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkLaserTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/DarkLaserTargeting", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PulsarGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/Pulsar_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GWPulsarGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/GWPulsar_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PolarisGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/Polaris_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ToxicCatalyzerGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/ToxicCatalyzer_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.VirulentCatalyzerGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/VirulentCatalyzer_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BiohazardGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Ranged/Specialist/Biohazard_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.HealingElixirGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Potions/HealingElixir_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkDivineSpark, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/DarkCloud/DarkDivineSpark", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ShatteredMoonlightGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Melee/Boomerangs/ShatteredMoonlightProjectile_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.GreySlashGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/GreySlash_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.UltimaWeapon, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Broadswords/UltimaWeaponTransparent", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.UltimaWeaponGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Broadswords/UltimaWeaponGlowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkUltimaWeapon, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkUltimaWeapon", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.DarkUltimaWeaponGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkUltimaWeaponGlowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ReflectionShift, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Accessories/Mobility/ReflectionShift", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PhazonRound, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/PhazonRound", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MoonlightGreatsword, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Broadswords/MoonlightGreatsword", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.MoonlightGreatswordGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Broadswords/MoonlightGreatsword_Glowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.EstusFlask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/EstusFlask_drinking", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.CeruleanFlask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/CeruleanFlask_drinking", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ElfinArrow, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/ElfinArrow", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.ElfinTargeting, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/ElfinTargeting", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.HumanityPhantom, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/HumanityPhantom", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BarbarousThornBladeGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Shortswords/BarbarousThornBlade_Glow", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.RedLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/Ammo/RedLaserBeam", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.RedLaserTransparent, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyRedLaser", AssetRequestMode.ImmediateLoad)}, //A transparent and non-transparent version of this exists because the current focused energy beam laser projectile stacks a lot of beam midsections on top of each other, which fucks up transparency
                {TransparentTextureType.LightrifleFire, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Magic/LightrifleFire", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.Lightning, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyLightningStrike", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.BulletHellLaser, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Gwyn/BulletHellLaser", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.HeavenPiercerGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/HeavenPiercerGlowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.SoapstoneMessage, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Tiles/SoapstoneMessage_1", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.CrystalRay, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Ranged/CrystalRayTrail", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.SeveringDuskGlowmask, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Melee/Broadswords/SeveringDuskGlowmask", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.Pinwheel, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Pinwheel/Pinwheel", AssetRequestMode.ImmediateLoad)},
                {TransparentTextureType.PinwheelFireglow, (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/Pinwheel/Pinwheel_Fireglow", AssetRequestMode.ImmediateLoad)},

            };


            //Runs each entry through the XNA's premultiplication function
            foreach (Texture2D textureEntry in TransparentTextures.Values)
            {
                Color[] buffer = new Color[textureEntry.Width * textureEntry.Height];
                textureEntry.GetData(buffer);
                for (int j = 0; j < buffer.Length; j++)
                {
                    buffer[j] = Color.FromNonPremultiplied(buffer[j].R, buffer[j].G, buffer[j].B, buffer[j].A);
                }
                textureEntry.SetData(buffer);
            }
        }
    };

}
