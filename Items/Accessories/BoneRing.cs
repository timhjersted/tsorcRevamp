using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.HandsOn)]

    public class BoneRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("+8% Ranged damage" +
                                "\n+8% Ranged critical strike chance");
        }
        public override void DrawHands(ref bool drawHands, ref bool drawArms)
        {
            drawHands = true;
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.defense = 2;
            item.accessory = true;
            item.value = PriceByRarity.Green_2;
            item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Bone, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.rangedDamage += 0.08f;
            player.rangedCrit += 8;
        }

    }
}
