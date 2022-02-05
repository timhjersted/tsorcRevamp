using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class RTQ2Leggings : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("RTQ2 Leggings");
            Tooltip.SetDefault("+10% Movespeed, +20 mana");
        }
        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 6;
            item.value = 100000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.1f;
            player.statManaMax2 += 20;
        }


        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MeteorLeggings, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

