
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Lore
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
        }
    }
}
