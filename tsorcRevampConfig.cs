using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace tsorcRevamp {
    [Label("Config")]
    [BackgroundColor(30, 60, 40, 220)]
    public class tsorcRevampConfig : ModConfig {
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => true;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Header("Gameplay Changes")]
        [Label("Adventure Mode")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Adventure mode prevents breaking and placing most blocks. \nIt also enables some features intended for the custom map. \n\"If the game lets you break it or place it, it's allowed!\"\nLeave this enabled if you're playing with the custom map!")]
        [DefaultValue(false)]
        public bool AdventureMode { get; set; }

        [Label("Souls Drop on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Drop all your Dark Souls when you die.\nIf \"Delete Dropped Souls on Death\" is enabled, \nyour Souls will drop after old Souls are deleted.\nDefaults to On")]
        [DefaultValue(true)]
        public bool SoulsDropOnDeath { get; set; }

        [Label("Delete Dropped Souls on Death")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Any Dark Souls in the world will be deleted when a player dies.\nEven if this option is disabled, your Souls will be deleted \nif over 400 items are active in the world after you die, \nor if you exit the game while your Souls are still on the ground.\nDefaults to On")]
        [DefaultValue(true)]
        public bool DeleteDroppedSoulsOnDeath { get; set; }
        /*
        [Label("Rename Skeletron")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Renames Skeletron to Gravelord Nito.\nOnly turn this off if you are experiencing \ncrashes or other strange behavior when \nyou attempt to summon Skeletron.\nDefaults to On")]
        [DefaultValue(true)]
        [ReloadRequired]
        public bool RenameSkeletron { get; set; }
        */

        [Label("Legacy Mode")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Legacy mode disables new additions from the Revamp team.\nTurn this on if you want to play the original \nStory of Red Cloud experience as it was in tConfig. \nSome changes and improvements will not be disabled. \nRequires a reload. \nDefaults to Off")]
        [DefaultValue(false)]
        [ReloadRequired]
        //todo items must be manually tagged as legacy. make sure we got them all
        //todo before release, set this to constant and comment out the legacy mode block
        public bool LegacyMode { get; set; }

        [Label("Boss Zen")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Boss Zen disables enemy spawns while a boss is alive.\nDefaults to On")]
        [DefaultValue(true)]
        public bool BossZenConfig { get; set; }


        [Header("Options")]
        [Label("Miakoda Volume")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Revamp Miakoda giving you Navi flashbacks?\nThis slider controls Miakoda's volume.\nSet to 0 to disable Miakoda sounds.")]
        [DefaultValue(100)]
        public uint MiakodaVolume;

        

        [Label("Soul Counter X position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 3840)]
        [DefaultValue(220)]
        [Tooltip("The X position of the Soul Counter.")]
        public int SoulCounterPosX { get; set; }

        [Label("Soul Counter Y position")]
        [BackgroundColor(60, 140, 80, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0, 2160)]
        [DefaultValue(70)]
        [Tooltip("The Y position of the Soul Counter.")]
        public int SoulCounterPosY { get; set; }

    }
}
