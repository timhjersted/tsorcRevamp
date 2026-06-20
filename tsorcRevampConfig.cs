using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp
{
    public class tsorcRevampConfig : ILoadable
    {
        public void Load(Mod mod)
        {
        }

        public void Unload()
        {
        }

        internal const string GameplayConfigName = "01Gameplay";
        internal const string VisualConfigName = "02Visuals";
        internal const string SoundConfigName = "03Sound";
        internal const string ControlsConfigName = "04Controls";

        internal static void MigrateRenamedConfig(ModConfig config, string oldName)
        {
            string oldPath = Path.Combine(ConfigManager.ModConfigPath, config.Mod.Name + "_" + oldName + ".json");
            string newPath = Path.Combine(ConfigManager.ModConfigPath, config.Mod.Name + "_" + config.Name + ".json");
            if (!File.Exists(newPath) && File.Exists(oldPath))
            {
                JsonConvert.PopulateObject(File.ReadAllText(oldPath), config, ConfigManager.serializerSettings);
            }
        }

        // Forwarding properties for backward compatibility
        public bool AdventureMode
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().AdventureMode;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().AdventureMode = value;
        }

        public bool SoulsDropOnDeath
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsDropOnDeath;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsDropOnDeath = value;
        }

        public bool DeleteDroppedSoulsOnDeath
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DeleteDroppedSoulsOnDeath;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DeleteDroppedSoulsOnDeath = value;
        }

        public bool BossZenConfig
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().BossZenConfig;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().BossZenConfig = value;
        }

        public bool DisableGloveAutoswing
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableGloveAutoswing;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableGloveAutoswing = value;
        }

        public bool DisableAutomaticQuickMana
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableAutomaticQuickMana;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableAutomaticQuickMana = value;
        }

        public bool DisableRifleScopeZoom
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableRifleScopeZoom;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableRifleScopeZoom = value;
        }

        public bool DisableDragoonGreavesDoubleJump
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableDragoonGreavesDoubleJump;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableDragoonGreavesDoubleJump = value;
        }

        public bool DisableSupersonicWings2ExtraJumps
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableSupersonicWings2ExtraJumps;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableSupersonicWings2ExtraJumps = value;
        }

        public bool DisableModWingsFallControlDuringFlight
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableModWingsFallControlDuringFlight;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DisableModWingsFallControlDuringFlight = value;
        }

        public bool EnableSoulsModeMobilityLimit
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsModeMobilityLimit;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsModeMobilityLimit = value;
        }

        public bool SoulsModeMobilityLimit
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsModeMobilityLimit;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().SoulsModeMobilityLimit = value;
        }

        public bool ActiveShieldsRevamp
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().ActiveShieldsRevamp;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().ActiveShieldsRevamp = value;
        }

        public bool DebugMode
        {
            get => ModContent.GetInstance<tsorcRevampGameplayConfig>().DebugMode;
            set => ModContent.GetInstance<tsorcRevampGameplayConfig>().DebugMode = value;
        }

        public int SoulCounterPosX
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().SoulCounterPosX;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().SoulCounterPosX = value;
        }

        public int SoulCounterPosY
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().SoulCounterPosY;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().SoulCounterPosY = value;
        }

        public int EstusFlaskPosX
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().EstusFlaskPosX;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().EstusFlaskPosX = value;
        }

        public int EstusFlaskPosY
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().EstusFlaskPosY;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().EstusFlaskPosY = value;
        }

        public int CeruleanFlaskPosX
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().CeruleanFlaskPosX;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().CeruleanFlaskPosX = value;
        }

        public int CeruleanFlaskPosY
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().CeruleanFlaskPosY;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().CeruleanFlaskPosY = value;
        }

        public bool HideCeruleanFlask
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().HideCeruleanFlask;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().HideCeruleanFlask = value;
        }

        public uint ChargeCircleOpacity
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().ChargeCircleOpacity;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().ChargeCircleOpacity = value;
        }

        public bool DisableSoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableSoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableSoapstones = value;
        }

        public bool HideSoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().HideSoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().HideSoapstones = value;
        }

        public bool AutoOpenSoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().AutoOpenSoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().AutoOpenSoapstones = value;
        }

        public bool DisableStorySoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableStorySoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableStorySoapstones = value;
        }

        public bool DisableLoreSoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableLoreSoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableLoreSoapstones = value;
        }

        public bool DisableTutorialSoapstones
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableTutorialSoapstones;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableTutorialSoapstones = value;
        }

        public bool DisableLocationBanner
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableLocationBanner;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().DisableLocationBanner = value;
        }

        public uint SoapstoneScale
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().SoapstoneScale;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().SoapstoneScale = value;
        }

        public bool ShowStaminaTooltip
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().ShowStaminaTooltip;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().ShowStaminaTooltip = value;
        }

        public bool UseCustomResourceBars
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().UseCustomResourceBars;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().UseCustomResourceBars = value;
        }

        public bool HideOverheadStaminaBar
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().HideOverheadStaminaBar;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().HideOverheadStaminaBar = value;
        }

        public bool EnemyHealthBars
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().EnemyHealthBars;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().EnemyHealthBars = value;
        }

        public bool GravityFix
        {
            get => ModContent.GetInstance<tsorcRevampVisualConfig>().GravityFix;
            set => ModContent.GetInstance<tsorcRevampVisualConfig>().GravityFix = value;
        }

        public bool UseOriginalPlayerHurtSounds
        {
            get => ModContent.GetInstance<tsorcRevampSoundConfig>().UseOriginalPlayerHurtSounds;
            set => ModContent.GetInstance<tsorcRevampSoundConfig>().UseOriginalPlayerHurtSounds = value;
        }

        public uint MiakodaVolume
        {
            get => ModContent.GetInstance<tsorcRevampSoundConfig>().MiakodaVolume;
            set => ModContent.GetInstance<tsorcRevampSoundConfig>().MiakodaVolume = value;
        }

        public uint BonfireFlyVolume
        {
            get => ModContent.GetInstance<tsorcRevampSoundConfig>().BonfireFlyVolume;
            set => ModContent.GetInstance<tsorcRevampSoundConfig>().BonfireFlyVolume = value;
        }

        public uint BotCMechanicsVolume
        {
            get => ModContent.GetInstance<tsorcRevampSoundConfig>().BotCMechanicsVolume;
            set => ModContent.GetInstance<tsorcRevampSoundConfig>().BotCMechanicsVolume = value;
        }

        public bool RecommendedControls
        {
            get => ModContent.GetInstance<tsorcRevampControlsConfig>().RecommendedControls;
            set => ModContent.GetInstance<tsorcRevampControlsConfig>().RecommendedControls = value;
        }
    }

    [Label("$Mods.tsorcRevamp.Configs.tsorcRevampGameplayConfig.DisplayName")]
    public class tsorcRevampGameplayConfig : ModConfig
    {
        public override bool Autoload(ref string name)
        {
            name = tsorcRevampConfig.GameplayConfigName;
            return base.Autoload(ref name);
        }

        public override void OnLoaded() => tsorcRevampConfig.MigrateRenamedConfig(this, nameof(tsorcRevampGameplayConfig));

        public override ConfigScope Mode => ConfigScope.ServerSide;
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => true;

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampGameplayConfig.Headers.AdventureMode")]
        [DefaultValue(true)]
        public bool AdventureMode { get; set; }

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampGameplayConfig.Headers.GameplayChanges")]
        [DefaultValue(true)]
        public bool SoulsDropOnDeath { get; set; }

        [DefaultValue(true)]
        public bool DeleteDroppedSoulsOnDeath { get; set; }

        [DefaultValue(true)]
        public bool BossZenConfig { get; set; }

        [DefaultValue(true)]
        public bool SoulsModeMobilityLimit { get; set; }

        [DefaultValue(true)]
        public bool ActiveShieldsRevamp { get; set; }

        [DefaultValue(false)]
        public bool DebugMode { get; set; }

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampGameplayConfig.Headers.GameplayTweaks")]
        [DefaultValue(false)]
        public bool DisableGloveAutoswing { get; set; }

        [DefaultValue(false)]
        public bool DisableAutomaticQuickMana { get; set; }

        [DefaultValue(false)]
        public bool DisableRifleScopeZoom { get; set; }

        [DefaultValue(false)]
        public bool DisableDragoonGreavesDoubleJump { get; set; }

        [DefaultValue(false)]
        public bool DisableSupersonicWings2ExtraJumps { get; set; }

        [DefaultValue(false)]
        public bool DisableModWingsFallControlDuringFlight { get; set; }
    }

    [Label("$Mods.tsorcRevamp.Configs.tsorcRevampVisualConfig.DisplayName")]
    public class tsorcRevampVisualConfig : ModConfig
    {
        public override bool Autoload(ref string name)
        {
            name = tsorcRevampConfig.VisualConfigName;
            return base.Autoload(ref name);
        }

        public override void OnLoaded() => tsorcRevampConfig.MigrateRenamedConfig(this, nameof(tsorcRevampVisualConfig));

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampVisualConfig.Headers.Soapstones")]
        [BackgroundColor(200, 80, 80, 192)]
        [ReloadRequired]
        [DefaultValue(false)]
        public bool DisableSoapstones { get; set; }

        [DefaultValue(true)]
        public bool HideSoapstones { get; set; }

        [DefaultValue(false)]
        public bool AutoOpenSoapstones { get; set; }

        [DefaultValue(false)]
        public bool DisableStorySoapstones { get; set; }

        [DefaultValue(false)]
        public bool DisableLoreSoapstones { get; set; }

        [DefaultValue(false)]
        public bool DisableTutorialSoapstones { get; set; }

        [Range(0, 100)]
        [DefaultValue(0)]
        public uint SoapstoneScale { get; set; }

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampVisualConfig.Headers.UI")]
        [DefaultValue(true)]
        public bool UseCustomResourceBars { get; set; }

        [DefaultValue(false)]
        public bool HideOverheadStaminaBar { get; set; }

        [DefaultValue(false)]
        public bool EnemyHealthBars { get; set; }

        [DefaultValue(false)]
        public bool HideCeruleanFlask { get; set; }

        [DefaultValue(224)]
        public uint ChargeCircleOpacity { get; set; }

        [DefaultValue(false)]
        public bool DisableLocationBanner { get; set; }

        [Range(0, 100)]
        [DefaultValue(true)]
        public bool ShowStaminaTooltip { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(178)]
        public int SoulCounterPosX { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(70)]
        public int SoulCounterPosY { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(93)]
        public int EstusFlaskPosX { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(127)]
        public int EstusFlaskPosY { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(179)]
        public int CeruleanFlaskPosX { get; set; }

        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(130)]
        public int CeruleanFlaskPosY { get; set; }

        [Header("$Mods.tsorcRevamp.Configs.tsorcRevampVisualConfig.Headers.Other")]
        [BackgroundColor(200, 80, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [ReloadRequired]
        [DefaultValue(true)]
        public bool GravityFix { get; set; }
    }

    [Label("$Mods.tsorcRevamp.Configs.tsorcRevampSoundConfig.DisplayName")]
    public class tsorcRevampSoundConfig : ModConfig
    {
        public override bool Autoload(ref string name)
        {
            name = tsorcRevampConfig.SoundConfigName;
            return base.Autoload(ref name);
        }

        public override void OnLoaded() => tsorcRevampConfig.MigrateRenamedConfig(this, nameof(tsorcRevampSoundConfig));

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(false)]
        public bool UseOriginalPlayerHurtSounds { get; set; }

        [DefaultValue(5)]
        public uint MiakodaVolume { get; set; }

        [DefaultValue(100)]
        public uint BonfireFlyVolume { get; set; }

        [DefaultValue(50)]
        public uint BotCMechanicsVolume { get; set; }
    }

    [Label("$Mods.tsorcRevamp.Configs.tsorcRevampControlsConfig.DisplayName")]
    public class tsorcRevampControlsConfig : ModConfig
    {
        public override bool Autoload(ref string name)
        {
            name = tsorcRevampConfig.ControlsConfigName;
            return base.Autoload(ref name);
        }

        public override void OnLoaded() => tsorcRevampConfig.MigrateRenamedConfig(this, nameof(tsorcRevampControlsConfig));

        public override ConfigScope Mode => ConfigScope.ClientSide;

        internal static bool Loaded;
        internal static bool LastRecommendedControls;

        [DefaultValue(true)]
        public bool RecommendedControls { get; set; }

        public override void OnChanged()
        {
            if (!Loaded)
            {
                Loaded = true;
                LastRecommendedControls = RecommendedControls;
                return;
            }

            bool controlsMatch = tsorcRevamp.RecommendedControlBindingsMatch();
            if (RecommendedControls && !LastRecommendedControls)
            {
                tsorcRevamp.ApplyRecommendedControlBindings(onlyIfDefaultOrOldDefault: false);
                controlsMatch = tsorcRevamp.RecommendedControlBindingsMatch();
                RecommendedControls = controlsMatch;
            }
            else if (RecommendedControls && !controlsMatch)
            {
                RecommendedControls = false;
            }

            LastRecommendedControls = RecommendedControls;
        }
    }
}
