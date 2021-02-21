using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientBrassHelmet : ModItem
    {

        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 20;
            item.defense = 2;
            item.value = 6000;
            item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientBrassArmor>() && legs.type == ModContent.ItemType<AncientBrassGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.statDefense += 6;
            player.moveSpeed += 0.1f;
            player.rangedDamage += 0.05f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
