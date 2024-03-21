using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Placeable.Trophies
{
    public abstract class TrophyItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }
        public abstract int TileID { get; }
        public abstract int Rarity { get; }
        public abstract int Value { get; }
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID);

            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = Rarity;
            Item.value = Value;
        }
    }
}