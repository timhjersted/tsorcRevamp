using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class LightCloak : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Light Cloak");
            Tooltip.SetDefault("Light Cloak activates +12 Life Regen when health falls below 150\n" +
                                "+4 life regen normally");

        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 200000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            if (player.statLife <= 150) {
                player.lifeRegen += 12;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else {
                player.lifeRegen += 4;
            }
        }
    }
}
