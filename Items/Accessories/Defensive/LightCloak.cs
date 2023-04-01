using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class LightCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Light Cloak");
            Tooltip.SetDefault("Light Cloak grants 3 increased life regeneration" +
                               "When life falls below 40%, increases life regeneration by 11 total");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += 3;
            if (player.statLife <= (player.statLifeMax2 / 5 * 2))
            {
                player.lifeRegen += 8;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
