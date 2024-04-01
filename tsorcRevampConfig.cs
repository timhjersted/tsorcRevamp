using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace tsorcRevamp
{
    [Label("Config")]
    [BackgroundColor(30, 60, 40, 220)]
    public class tsorcRevampConfig : ModConfig
    {
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => true;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Header("Mods.tsorcRevamp.Configs.tsorcRevampConfig.Headers.AdventureMode")]
        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        public bool AdventureMode { get; set; }


        [Header("Mods.tsorcRevamp.Configs.tsorcRevampConfig.Headers.GameplayChanges")]
        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        public bool SoulsDropOnDeath { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        public bool DeleteDroppedSoulsOnDeath { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        public bool BossZenConfig { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool DisableGloveAutoswing { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool DisableAutomaticQuickMana { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool DisableRifleScopeZoom { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool DisableDragoonGreavesDoubleJump { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool DisableSupersonicWings2ExtraJumps { get; set; }


        [Header("Mods.tsorcRevamp.Configs.tsorcRevampConfig.Headers.Visual")]

        /*
        [Label("Legacy Music")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Warning! This music was disabled to protect streamers and youtubers who were having copyright issues with it, despite being fair use. Enable it for the classic experience, but we do not advise streaming or recording while it is active.")]
        [DefaultValue(false)]
        public bool LegacyMusic { get; set; }*/

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(460)]
        public int SoulCounterPosX { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(907)]
        public int SoulCounterPosY { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(111)]
        public int EstusFlaskPosX { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(124)]
        public int EstusFlaskPosY { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(130)]
        public int CeruleanFlaskPosX { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(224)]
        public int CeruleanFlaskPosY { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        public bool HideCeruleanFlask { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(224)]
        public uint ChargeCircleOpacity { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        public bool HideSoapstones { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [Range(0, 100)]
        [DefaultValue(0)]
        public uint SoapstoneScale { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [Range(0, 100)]
        [DefaultValue(true)]
        public bool ShowStaminaTooltip { get; set; }

        [BackgroundColor(200, 80, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [ReloadRequired]
        [DefaultValue(true)]
        public bool GravityFix { get; set; }



        [Header("Mods.tsorcRevamp.Configs.tsorcRevampConfig.Headers.Sound")]

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(5)]
        public uint MiakodaVolume { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(100)]
        public uint BonfireFlyVolume { get; set; }

        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(50)]
        public uint BotCMechanicsVolume { get; set; }

        /*
        [Label("Auto-Update Adventure Map")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Automatically download updates to the adventure map.\nAround 6MB, updates every few weeks.\nAdventure map updates will not affect existing worlds, only newly created ones.")]
        [DefaultValue(true)]
        public bool AutoUpdateMap { get; set; }

        [Label("Auto-Update Music Mod")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Automatically download updates to the music mod.\nAround 100MB, updates every few months.")]
        [DefaultValue(true)]
        public bool AutoUpdateMusic { get; set; }*/
    }
}
