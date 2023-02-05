
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    public abstract class Crest : ModItem
    { //crests are using an abstract so we dont have 6 different .cs files with essentially nothing in them
        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 12;
            Item.rare = ItemRarityID.Blue;
            Item.maxStack = 1;
        }
    }

    public class CrestOfFire : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Fire");
            Tooltip.SetDefault("A shard of blazing fury\n" +
                               "You don't know what it's for, but you're sure it's important.\n" +
                               "[c/00ffd4:After defeating The Rage, Miakoda tells you about the Wyvern Mage that lives high atop the mountain above these caverns] - \n" +
                               "[c/00ffd4:an ally of Attraidies.] [c/ffbf00:\"If he is helping the Mindflayer King, he could know where Asha is.\"]");
        }
    }

    public class CrestOfWater : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Water");
            Tooltip.SetDefault("A shard of crystallized water\n" +
                               "A second piece. 'Are these artifacts part of a puzzle?'\n" +
                               "[c/00ffd4:This shard contains a riddle:] \"[c/ffbf00:Beneath the Burning hot sun,]\n" +
                               "[c/ffbf00:sheltered from the blistering Western Winds, Black Smoke with a faint Violet Glow illuminates the path.]\"");
        }
    }
    public class CrestOfEarth : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Earth");
            Tooltip.SetDefault("A shard of pure minerals\n" +
                               "One of eight\n" +
                               "[c/00ffd4:This one contains a riddle:] \n" +
                               "\"[c/ffbf00:Atop the Twin Peaks of Arazium, we gaze at Sky Above, and the Great Chasm Below. We are the Eyes of the Ancient One.]\"");
        }
    }

    public class CrestOfCorruption : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Corruption");
            Tooltip.SetDefault("A shard of glowing demonite, oozing with phazon\n" +
                               "One of eight. [c/00ffd4:On the underside it says simply:]\n" +
                               "\"[c/ffbf00:Below a Broken, familiar Bridge, you will find me.]\"");
        }
    }

    public class CrestOfSky : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.maxStack = 3;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crests of Sky");
            Tooltip.SetDefault("Three shards of oxygenated carbonate\n" +
                               "Combined this makes one of eight. [c/00ffd4:On its side it contains a clue:]\n" +
                               "\"[c/ffbf00:Two paths lead to my entrance. Below the Oasis in the Desert, Serris unlocks the Way.]\n" +
                               "\"[c/ffbf00:From below a familiar, Violet Smoke, a Chlorophyte Gate guards the other.]\"");
                
        }
    }

    public class CrestOfSteel : Crest
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crest of Steel");
            Tooltip.SetDefault("A shard of burning hot steel\n" +
                               "One of eight.\n" +
                               "[c/00ffd4:A riddle is etched into the metal:] \n" +
                               "\"[c/ffbf00:Above the Machine's Throne, Below the Dryad's Cove: A Locked Door guards an Ancient Pyramid, long ago forgotten.]\"");
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
                               "[c/00ffd4:Threaded between its strands, another riddle:] \n" +
                               "\"[c/ffbf00:You've found the Key. But where is the Lock?]\"");
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
                              "One of eight.\n" +
                               "When I fit all the pieces together, there's still something missing.\n" +
                               "[c/00ffd4:This crest contains the final riddle:]\n" +
                               "\"[c/ffbf00:Towering Heights, Far to the West. The Molten Eye of Attraidies looks to the Sky Above. He is near.]\"");
                                //[c/ffbf00: ] is yellow
        }                       //[c/00ffd4: is light blue
    }
}
