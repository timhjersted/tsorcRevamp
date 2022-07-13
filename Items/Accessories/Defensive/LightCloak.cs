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
            Tooltip.SetDefault("Light Cloak activates +12 Life Regen when health falls below 150\n" +
                                "+4 life regen normally");

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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= 150)
            {
                player.lifeRegen += 12;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                player.lifeRegen += 4;
            }
        }
    }
}
