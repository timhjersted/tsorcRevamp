using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace tsorcRevamp {
    [Label("Config")]
    [BackgroundColor(30, 60, 40, 220)]
    public class tsorcRevampConfig : ModConfig {
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Header("Gameplay Changes")]
        [Label("Adventure Mode")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Adventure mode prevents breaking and placing most blocks. \nIt also enables some features intended for the custom map. \nLeave this enabled if you're playing with the custom map!")]
        [DefaultValue(true)]
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


        [Header("Options")]
        [Label("Mute Miakoda")]
        [BackgroundColor(60, 140, 80, 192)]
        [Tooltip("Miakoda giving you Navi flashbacks?\nTurn this on if you want to mute Revamp mode Miakoda.\nDefaults to Off")]
        [DefaultValue(false)]

        public bool MuteMiakoda { get; set; }
    }
}
