using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace tsorcRevamp
{
    [Label("Config")]
    [BackgroundColor(30, 60, 40, 220)]
    public class tsorcRevampConfig : ModConfig
    {
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => true;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Header("Adventure Mode")]
        [Label("Adventure Mode: Main")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Prevents breaking and placing most blocks. \nIt also enables some features intended for the custom map. \n\"If the game lets you break it or place it, it's allowed!\"\nLeave this enabled if you're playing with the custom map!")]
        [DefaultValue(true)]
        public bool AdventureMode { get; set; }


        [Header("Gameplay Changes")]
        [Label("Souls Drop on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Drop all your Dark Souls when you die.\nIf \"Delete Dropped Souls on Death\" is enabled, \nyour Souls will drop after old Souls are deleted.\nDoes not take effect in multiplayer.\nDefaults to On")]
        [DefaultValue(true)]
        public bool SoulsDropOnDeath { get; set; }

        [Label("Delete Dropped Souls on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Dark Souls dropped by dying will be deleted if you die again\nbefore picking them up. Even if this option is disabled, your\nSouls will be deleted if you exit the game while your Souls\nare still on the ground. Does not take effect in multiplayer.\nDefaults to On")]
        [DefaultValue(true)]
        public bool DeleteDroppedSoulsOnDeath { get; set; }

        [Label("Boss Zen")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Boss Zen disables enemy spawns while a boss is alive.\nDefaults to On")]
        [DefaultValue(true)]
        public bool BossZenConfig { get; set; }

        [Label("Disable Melee/Whip Glove's Autoswing")]
        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(false)]
        [Tooltip("Disable the Melee/Whip Autoswing feature provided by the Feral Claws and it's upgrades, the Autofire option overrides this. Useful for whip stacking. Defaults to Off.")]
        public bool DisableGloveAutoswing { get; set; }


        [Header("Visual")]

        /*
        [Label("Legacy Music")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Warning! This music was disabled to protect streamers and youtubers who were having copyright issues with it, despite being fair use. Enable it for the classic experience, but we do not advise streaming or recording while it is active.")]
        [DefaultValue(false)]
        public bool LegacyMusic { get; set; }*/

        [Label("Soul Counter X position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(460)]
        [Tooltip("The X position of the Soul Counter.")]
        public int SoulCounterPosX { get; set; }

        [Label("Soul Counter Y position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(907)]
        [Tooltip("The Y position of the Soul Counter.")]
        public int SoulCounterPosY { get; set; }

        [Label("Estus Flask X position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(94)]
        [Tooltip("The X position of the Estus Flask.")]
        public int EstusFlaskPosX { get; set; }

        [Label("Estus Flask Y position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(144)]
        [Tooltip("The Y position of the Estus Flask.")]
        public int EstusFlaskPosY { get; set; }

        [Label("Cerulean Flask X position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(144)]
        [Tooltip("The X position of the Cerulean Flask.")]
        public int CeruleanFlaskPosX { get; set; }

        [Label("Cerulean Flask Y position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(344)]
        [Tooltip("The Y position of the Cerulean Flask.")]
        public int CeruleanFlaskPosY { get; set; }

        [Label("Charged Weapon Indicator Opacity")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Some charged weapons draw a circle around the cursor to indicate the\ncurrent charge level. The opacity of that circle can be set here.\nSet to 0 to disable charge circles.")]
        [DefaultValue(100)]
        public uint ChargeCircleOpacity { get; set; }

        [Label("Hide Read Soapstones")]
        [BackgroundColor(60, 140, 80, 192)]
        [DefaultValue(true)]
        [Tooltip("If enabled, soapstone messages that have been read will\nnot be shown until a button is clicked to show them.\nDefaults to on.")]
        public bool HideSoapstones { get; set; }

        [Label("Soapstone Size Increase")]
        [BackgroundColor(60, 140, 80, 192)]
        [Range(0, 100)]
        [DefaultValue(0)]
        [Tooltip("Increases the size of all soapstone dialogue boxes by a percentage.\nDefaults to 0%.")]
        public uint SoapstoneScale { get; set; }

        [Label("Stamina Use Tooltip")]
        [BackgroundColor(60, 140, 80, 192)]
        [Range(0, 100)]
        [DefaultValue(true)]
        [Tooltip("Show the stamina consumption tooltip for Bearer of the Curse mode.\nDefaults to On.")]
        public bool ShowStaminaTooltip { get; set; }

        [Label("Disable Gravitation Effect Screen Flip")]
        [BackgroundColor(200, 80, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [ReloadRequired]
        [DefaultValue(true)]
        [Tooltip("IN BETA! (Mostly working, but there will be some minor bugs)" +
            "\nFlipping gravity will only flip your character instead of the whole screen.")]
        public bool GravityFix { get; set; }


        [Label("Broadsword Rework")]
        [BackgroundColor(200, 80, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [ReloadRequired]
        [DefaultValue(true)]
        [Tooltip("IN BETA! \nMake broadswords great again. \nRequires reloading the world to have an effect.")]
        public bool BroadswordRework { get; set; }



        [Header("Sound")]

        [Label("Miakoda Volume")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Revamp Miakoda giving you Navi flashbacks?\nThis slider controls Miakoda's volume.\nSet to 0 to disable Miakoda sounds.")]
        [DefaultValue(100)]
        public uint MiakodaVolume { get; set; }

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
