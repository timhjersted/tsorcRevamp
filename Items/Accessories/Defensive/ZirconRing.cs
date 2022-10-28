using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class ZirconRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Imbues weapons with fire, providing" +
                                "\n10% increased melee and whip damage, and all swords and whips inflict fire damage." +
                                "\nPlus Thorns Effect and +4 Defense.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            //recipe.AddIngredient(Mod.Find<ModItem>("EphemeralDust").Type, 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 9000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.AddBuff(BuffID.WeaponImbueFire, 60, false);
            player.statDefense += 4;
            player.thorns = 1f;
        }

    }
}

