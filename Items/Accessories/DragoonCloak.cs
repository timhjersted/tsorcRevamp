using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragoonCloak : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Combines the effects of the Dark, Light and Darkmoon Cloak into one all-powerful protective cloak." +
                                 "\nThese effects are, however, significantly weaker");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 22;
            item.defense = 2;
            item.accessory = true;
            item.value = 2000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.StarCloak);
            recipe.AddIngredient(mod.GetItem("LightCloak"));
            recipe.AddIngredient(mod.GetItem("DarkCloak"));
            recipe.AddIngredient(mod.GetItem("DarkmoonCloak"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);

            player.statDefense += 4;
            player.lifeRegen += 2;
            player.starCloak = true;
            player.magicCrit += 3;
            player.magicDamage += .05f;


            if (player.statLife <= 120) {
                player.lifeRegen += 6;
                player.statDefense += 8;
                player.manaRegenBuff = true;
                player.starCloak = true;
                player.magicCrit += 3;
                player.magicDamage += .05f;


                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;

            }
        }
    }
}
