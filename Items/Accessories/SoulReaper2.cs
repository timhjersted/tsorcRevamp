using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class SoulReaper2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul Reaper II");
            /* Tooltip.SetDefault("Greatly increases Dark Soul pick-up range and" +
                               "\nincreases consumable soul drop chance by 50%" +
                               "\nLashes out with a sickle in retaliation" +
                               "\nGives off an eerie aura"); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddIngredient(ModContent.ItemType<SoulReaper>(), 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SoulReaper += 10;
            player.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult += 10; //50% increase
            player.GetModPlayer<tsorcRevampPlayer>().SoulSickle = true;


            Lighting.AddLight(player.Center, 0.4f, 0.7f, 0.6f);

            if (Main.rand.NextBool(25))
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X - 20, (float)player.position.Y - 20), player.width + 40, player.height + 20, 89, 0, 0, 0, default, .5f);
                Main.dust[dust].velocity *= 0.25f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1f;
            }
        }

    }
}
