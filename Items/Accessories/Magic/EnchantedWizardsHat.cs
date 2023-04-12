using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    [AutoloadEquip(EquipType.Face)]
    [LegacyName("GrandWizardsHat")]
    public class EnchantedWizardsHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enchanted Wizard's Hat");
            /* Tooltip.SetDefault("Increases magic damage by 13% multiplicatively" +
                "\n+80 max Mana"); */
        }

        public override void SetDefaults()
        {
            Item.width = 88;
            Item.height = 70;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SorcererEmblem);
            recipe.AddIngredient(ItemID.WizardHat);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) *= 1.13f;
            player.statManaMax2 += 80;
        }

    }
}