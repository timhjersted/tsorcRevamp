using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    class RingOfClarity : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Accessories/PoisonbloodRing";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The Ring of Clarity prevents confusion, gravitation disorientation, bleeding, and poisoning. \n+4 HP Regeneration and 9 defense");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.useAnimation = 100;
            Item.useTime = 100;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<PoisonbloodRing>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {

            player.buffImmune[BuffID.Confused] = true;
            player.buffImmune[BuffID.Gravitation] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.buffImmune[BuffID.Poisoned] = true;

            player.lifeRegen += 4;
            player.statDefense += 9;
        }
    }
}
