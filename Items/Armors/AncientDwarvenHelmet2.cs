using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Head)]
    class AncientDwarvenHelmet2 : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Dwarven Helmet II");
            Tooltip.SetDefault("Set bonus grants +8 defense, +9% melee damage, and +9% melee speed. \n+6 life regen when health falls below 80, +3 otherwise.");
        }

        public override void SetDefaults() {
            item.height = item.width = 18;
            item.defense = 4;
            item.value = 15000;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<AncientDwarvenArmor>() && legs.type == ModContent.ItemType<AncientDwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player) {
            player.statDefense += 8;
            player.meleeDamage += 0.09f;
            player.meleeSpeed += 0.09f;
            if (player.statLife < 80) player.lifeRegen += 6;
            else player.lifeRegen += 3;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<AncientDwarvenHelmet>(), 500);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
