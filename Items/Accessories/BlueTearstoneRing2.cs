using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BlueTearstoneRing2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Blue Tearstone Ring II");
            Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis enchanted blue tearstone from Catarina boosts the defence of its wearer by 85 when in danger." +
                                "\nOtherwise, the ring gifts the wearer a normal +30 defense boost." +
                                "\nWhile worn, melee damage is reduced by 200%, making it a ring " +
                                "\nonly suited to mages and other ranged classes.");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = PriceByRarity.Purple_11;
            item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BlueTearstoneRing"), 1);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 5);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 15);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.meleeDamage -= 2f;
            player.meleeCrit = -50;
            if (player.statLife <= 80) {
                player.statDefense += 85;
            }
            else {
                player.statDefense += 30;
            }
        }

    }
}
