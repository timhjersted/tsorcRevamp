using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class ArcherOfLumeliaHairStyle : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Archer of Lumelia Hairstyle");
            Tooltip.SetDefault("Gifted with bows, repeaters, and other long range weapons");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 1;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ArcherOfLumeliaShirt>() && legs.type == ModContent.ItemType<ArcherOfLumeliaPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.rangedCrit += 23;
            player.rangedDamage += 0.15f;
            player.archery = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.AdamantiteMask, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
