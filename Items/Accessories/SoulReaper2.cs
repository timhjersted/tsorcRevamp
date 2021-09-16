using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SoulReaper2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Soul Reaper II");
            Tooltip.SetDefault("Greatly increases Dark Soul pick-up range and" +
                               "\nincreases consumable soul drop chance by 50%" +
                               "\nLashes out with a sickle in retaliation" +
                               "\nGives off an eerie aura");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 50000;
            item.rare = ItemRarityID.Pink;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 7000);
            recipe.AddIngredient(mod.GetItem("SoulReaper"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 10;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 10; //50% increase
            player.GetModPlayer<tsorcRevampPlayer>().SoulSickle = true;


            Lighting.AddLight(player.Center, 0.4f, 0.7f, 0.6f);

            if (Main.rand.Next(25) == 0)
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X - 20, (float)player.position.Y - 20), player.width + 40, player.height + 20, 89, 0, 0, 0, default, .5f);
                Main.dust[dust].velocity *= 0.25f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1f;
            }
        }

    }
}
