using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    public abstract class Crest : ModItem { //crests are using an abstract so we dont have 6 different .cs files with essentially nothing in them
        public override void SetDefaults() {
            item.width = 12;
            item.height = 12;
            item.rare = ItemRarityID.Blue;
            item.maxStack = 1;
        }
    }

    public class CrestOfFire : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Fire");
            Tooltip.SetDefault("A shard of blazing fury\n" +
                               "You don't know what it's for, but you're sure it's important.\n" +
                               "After defeating The Rage, Miakoda tells you about the Wyvern Mage that lives high atop the mountain above these caverns - \n" +
                               "an ally of Attraidies. \"If he is helping the Mindflayer King, he could know where Asha is.\"");
        }
    }

    public class CrestOfWater : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Water");
            Tooltip.SetDefault("A shard of crystallized water\n" +
                               "A second piece. Are these artifacts part of a puzzle?\n" +
                               "This shard contains a riddle: \"Beneath the burning hot sun,\n" +
                               "heading against the western winds, a violet glow illuminates the path.\"");
        }
    }
    public class CrestOfEarth : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Earth");
            Tooltip.SetDefault("A shard of pure minerals\n" +
                               "One of eight\n" +
                               "This one contains a riddle: \n" +
                               "\"The Twin Peaks of Arazium. Twins from birth. Guardians of the Sky.\"");
        }
    }

    public class CrestOfCorruption : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Corruption");
            Tooltip.SetDefault("A shard of glowing demonite, oozing with phazon\n" +
                               "One of eight. On the underside it says simply:\n" +
                               "\"Below familiar waters, you will find me.\"");
        }
    }

    public class CrestOfSky : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Sky");
            Tooltip.SetDefault("Half of a shard of oxygenated carbonate\n" +
                               "Combined with the second half, this makes one of eight. On its side it contains a clue:\n" +
                               "\"Beneath the Burning Sun, Below the Purple Light, an Alternate Path opens the way.\"");
        }
    }

    public class CrestOfSteel : Crest {
        public override void SetDefaults() {
            base.SetDefaults();
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crest of Steel");
            Tooltip.SetDefault("A shard of burning hot steel\n" +
                               "One of eight.\n" +
                               "A riddle is etched into the metal: \n" +
                               "\"Above the machine's throne, below the Dryad's cove: A verdant realm lies lost to time.\"");
        }
    }
    public class CrestOfLife : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Life");
            Tooltip.SetDefault("A shard of woven vine, stronger than iron\n" +
                               "One of eight.\n" +
                               "Threaded between its strands, another riddle: \n" +
                               "\"You've found the key. But where is the lock?");
        }
    }
    public class CrestOfStone : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Stone");
            Tooltip.SetDefault("A shard of solar-charged masonry\n" +
                              "One of eight. Could this be the last one? \n" +
                               "\"When I fit all the pieces together, there's still something missing.\"\n" +
                               "This crest contains a riddle: \n" +
                               "\"Towering heights. The Eye of Attraidies looks to the sky above. He is near.\"");
        }
    }
}
