using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.Shield)]
    public class AncientDemonShield : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Powerful Greatshield that reduces damage taken by 5%, inherits Obsidian Shield's effects and gives thorns buff");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.defense = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.noKnockback = true;
            player.thorns += 1f;
            player.fireWalk = true;
            player.endurance += 0.05f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianShield);
            recipe.AddIngredient(ModContent.ItemType<SpikedIronShield>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
