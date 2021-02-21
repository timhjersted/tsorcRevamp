using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientGoldenHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("It is the famous Helmet of the Stars.\n+5% Melee critical chance.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 4;
            item.value = 1500000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeCrit += 5;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientGoldenArmor>() && legs.type == ModContent.ItemType<AncientGoldenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "+10% melee damage, +20 mana";
            player.meleeDamage += 0.10f;
            player.statManaMax2 += 20;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
