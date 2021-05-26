using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Head)]
    class AncientDwarvenHelmet : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Set bonus grants +4 defense, +7% melee damage, and +7% melee speed. \n+4 life regen when health falls below 80, +1 otherwise.");
        }

        public override void SetDefaults() {
            item.height = item.width = 18;
            item.defense = 4;
            item.value = 10000;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) {
            return body.type == ModContent.ItemType<AncientDwarvenArmor>() && legs.type == ModContent.ItemType<AncientDwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player) {
            player.statDefense += 4;
            player.meleeDamage += 0.07f;
            player.meleeSpeed += 0.07f;
            if (player.statLife < 80) player.lifeRegen += 4;
            else player.lifeRegen += 1;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
