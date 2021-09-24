using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class CovenantOfArtorias : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Covenant of Artorias");
            Tooltip.SetDefault("Allows you to enter The Abyss when worn. Remove the ring to escape from The Abyss!" +
                                "\nAlso grants immunity to lava, knockback, and fire blocks." +
                                "\n+7% Melee speed" +
                                "\n+7% Move speed" +
                                "\n+7% Damage" +
                                "\n+7% Critical strike chance");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 26;
            item.accessory = true;
            item.value = PriceByRarity.Cyan_9;
            item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 16000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.allDamage += 0.07f;
            player.moveSpeed += 0.07f;
            player.magicCrit += 7;
            player.meleeCrit += 7;
            player.rangedCrit += 7;
            player.lavaImmune = true;
            player.noKnockback = true;
            player.fireWalk = true;
            player.enemySpawns = true;
        }
        

    }
}
