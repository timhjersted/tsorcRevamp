using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.HandsOn)]

    public class MoltenRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Enchanted Molten Ring grants fire-walking ability and negates knockback" + 
								"\n+10% Melee Damage" + 
								"\nThe enchanted ring's power is fueled by a +5% mana cost");
        }
 
        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.accessory = true;
            item.value = PriceByRarity.Orange_3;
            item.rare = ItemRarityID.Orange;
        }
 
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HellstoneBar, 10);
			recipe.AddIngredient(mod.GetItem("EphemeralDust"), 6);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
 
        public override void UpdateEquip(Player player) {
			player.fireWalk = true;
			player.meleeDamage += 0.1f;
			player.noKnockback = true;
			player.manaCost += 0.05f;
        }
 
    }
}