using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    public class AtmaWeapon : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword that draws power from the wielder.\n" +
                                "The true form of your father's sword revealed.\n" +
                                "Does 105 damage when at full health, and 80 damage at half health, scaling with current HP.");

        }

        public override void SetDefaults() {

            item.stack = 1;
            item.rare = ItemRarityID.Pink;
            item.damage = 105;
            item.height = 58;
            item.knockBack = (float)9;
            item.maxStack = 1;
            item.melee = true;
            item.autoReuse = true;
            item.useAnimation = 19;
            item.useTime = 19;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 4000000;
            item.width = 58;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Excalibur, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat) {
            mult = ((float)player.statLife / (player.statLife) * 50) + 55;
        }
    }
}
