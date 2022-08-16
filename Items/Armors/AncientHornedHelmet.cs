/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientHornedHelmet : ModItem //To be reworked
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A treasure from ancient Plains of Havoc\nIncreases crit by 13%");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 5;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item); //TODO: Set this once it's reworked
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 13;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup(tsorcRevampSystems.CobaltHelmets, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
*/