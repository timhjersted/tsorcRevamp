using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class GenjiHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Amor from the East\n+25% magic damage");
        }
        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 20;
            item.defense = 9;
            item.value = 5000000;
            item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GenjiArmor>() && legs.type == ModContent.ItemType<GenjiGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.magicCrit += 20;
            player.statManaMax2 += 100;
            player.manaRegen += 3;
        }

        public override void UpdateEquip(Player player)
        {
            player.magicDamage += 0.25f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteHeadgear, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
