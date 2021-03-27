using Terraria.ModLoader;

namespace tsorcRevamp.Items
{
    class BrokenStrangeMagicRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Broken Strange Magic Ring");
            Tooltip.SetDefault("A strange magic ring, broken into two pieces..." +
                               "\nMiakoda: \"To reforge this ring and restore its magical abilitiy," +
                               "\nyou'll need 7 White Titanite, 20 Cursed Souls and 1000 Dark Souls." +
                               "\nTo find what we need, we should seek out the corruption and the undergound.\"");
        }

        public override void SetDefaults()
        {
            item.maxStack = 1;
            item.width = 20;
            item.height = 20;
        }
    }
}
