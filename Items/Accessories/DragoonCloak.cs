using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragoonCloak : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Combines the effects of the Dark, Light and Darkmoon Cloak into one all-powerful protective cloak." +
                                 "\nA large amount of Dark Souls were used to preserve each cloak's potency.");
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 22;
            Item.defense = 2;
            Item.accessory = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("LightCloak"));
            recipe.AddIngredient(Mod.GetItem("DarkCloak"));
            recipe.AddIngredient(Mod.GetItem("DarkmoonCloak"));
            recipe.AddIngredient(ItemID.ChlorophyteBar, 3);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);

            player.statDefense += 5;
            player.lifeRegen += 4;
            player.starCloak = true;
            player.magicCrit += 5;
            player.magicDamage += .05f;


            if (player.statLife <= 120) {
                player.lifeRegen += 12;
                player.statDefense += 15;
                player.manaRegenBuff = true;
                player.magicCrit += 15;
                player.magicDamage += .15f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
