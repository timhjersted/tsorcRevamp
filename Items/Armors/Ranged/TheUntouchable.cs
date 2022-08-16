using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Legs)]
    public class TheUntouchable : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("4% increases ranged damage and critical strike chance\n4% increased move speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 13;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.04f;
            player.GetCritChance(DamageClass.Ranged) += 4;
            player.moveSpeed += 0.04f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedGreaves, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

