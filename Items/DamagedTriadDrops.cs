using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items;

class DamagedCrystal : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A powerful crystal, once charged with illuminant hallowed light\n" +
            "Perhaps it can be repaired?");
    }

    public override void SetDefaults()
    {
        Item.width = 21;
        Item.height = 21;
        Item.rare = ItemRarityID.White;
        Item.value = 0;
    }
}
class DamagedFlameNozzle : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A nozzle once used to expel searing jets of cursed fire\n" +
            "Perhaps it can be repaired?");
    }

    public override void SetDefaults()
    {
        Item.width = 21;
        Item.height = 21;
        Item.rare = ItemRarityID.White;
        Item.value = 0;
    }
}
class DamagedLaser : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A laser that once fired cataclysmic torrents of energy\n" +
            "Perhaps it can be repaired?");
    }

    public override void SetDefaults()
    {
        Item.width = 21;
        Item.height = 21;
        Item.rare = ItemRarityID.White;
        Item.value = 0;
    }
}
class DamagedRemote : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A broken remote, humming with a strange frequency\n" +
            "Perhaps it can be repaired?");
    }

    public override void SetDefaults()
    {
        Item.width = 21;
        Item.height = 21;
        Item.rare = ItemRarityID.White;
        Item.value = 0;
    }
}
