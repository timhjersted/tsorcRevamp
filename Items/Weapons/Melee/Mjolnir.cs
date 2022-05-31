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
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.useTime = 14;
            Item.pick = 100;
            Item.axe = 120;
            Item.hammer = 200;
            Item.damage = 44;
            Item.knockBack = 15;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.scale = 1.4f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.melee = true;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.Pwnhammer, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 15000);
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
