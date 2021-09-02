using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientHornedHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A treasure from ancient Plains of Havoc.\n+5% critical chance");
        }

        public override void SetDefaults()
        {
            item.width = 26;
            item.height = 20;
            item.defense = 9;
            item.value = 8000000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.rangedCrit += 5;
            player.meleeCrit += 5;
            player.magicCrit += 5;
            player.thrownCrit += 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientMagicPlateArmor>() && legs.type == ModContent.ItemType<AncientMagicPlateGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.allDamage += 0.06f;
            player.statManaMax2 += 40;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
