using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class ShadowCloakGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.07f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.09f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1600);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

