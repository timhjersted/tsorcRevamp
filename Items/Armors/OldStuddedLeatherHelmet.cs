using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class OldStuddedLeatherHelmet : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 26;
            item.defense = 3;
            item.value = 900;
            item.rare = ItemRarityID.White;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<OldStuddedLeatherArmor>() && legs.type == ModContent.ItemType<OldStuddedLeatherGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.rangedDamage += 0.07f;
            player.rangedCrit += 5;
            player.moveSpeed += 0.1f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldLeatherHelmet"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
