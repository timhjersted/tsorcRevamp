using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class SoulReaper : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases Dark Soul pick-up range and increases" +
                                "\nconsumable soul drop chance by 25%" +
                                "\nGives off a faint glow" +
                                "\nCan be upgraded with 7000 Dark Souls");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 5;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 5; //25% increase

            Lighting.AddLight(player.Center, 0.45f, 0.3f, 0.5f);

            if (Main.rand.NextBool(20))
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X - 20, (float)player.position.Y - 20), player.width + 40, player.height + 20, 21, 0, 0, 0, default, 1f);
                Main.dust[dust].velocity *= 0.25f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1f;
            }
        }

    }
}
