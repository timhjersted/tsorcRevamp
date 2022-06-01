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
            Item.width = 24;
            Item.height = 22;
            Item.defense = 2;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.Bone, 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.GetDamage(DamageClass.Ranged) += 0.08f;
            player.GetCritChance(DamageClass.Ranged) += 8;
        }

    }
}
