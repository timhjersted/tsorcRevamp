using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class RedMageHat : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 30;
            item.height = 16;
            item.defense = 2;
            item.value = 6000;
            item.rare = ItemRarityID.Blue;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RedMageTunic>() && legs.type == ModContent.ItemType<RedMagePants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.statManaMax2 += 20;
            player.magicDamage += 0.08f;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Silk, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
