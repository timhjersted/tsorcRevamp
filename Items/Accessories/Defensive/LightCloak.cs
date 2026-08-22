using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class LightCloak : ModItem
    {
        public const float LifeRegen1 = 4f;
        public const float LifeThreshold = 40f;
        public const float LifeRegen2 = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen1 / 2f, LifeThreshold, LifeRegen2 / 2f);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += (int)LifeRegen1;
            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.lifeRegen += (int)(LifeRegen2 - LifeRegen1);

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
