using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class Mjolnir : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Shatter the earth" +
                                "\nBreaks walls and trees with amazing speed" +
                                "\nAlso retains the pickaxe strength of the Pwnhammer" +
                                "\nHold the cursor away from you to wield only as a weapon");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.useTime = 14;
            item.pick = 100;
            item.axe = 120;
            item.hammer = 200;
            item.damage = 44;
            item.knockBack = 15;
            item.autoReuse = true;
            item.useTurn = true;
            item.scale = 1.4f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.value = PriceByRarity.LightRed_4;
            item.melee = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Pwnhammer, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void MeleeEffects(Player player, Rectangle rectangle) {
            Color color = new Color();
            //This is the same general effect done with the Fiery Greatsword
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 15, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;
        }

    }
}
