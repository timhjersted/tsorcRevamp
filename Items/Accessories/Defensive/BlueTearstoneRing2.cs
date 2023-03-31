using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class BlueTearstoneRing2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blue Tearstone Ring II");
            Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis enchanted blue tearstone from Catarina boosts the defense of its wearer by 25 and reduces damage taken by 12%" +
                                "\n when life falls below 33%. This also reduces melee damage by 200%.");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 15;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlueTearstoneRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= (player.statLifeMax / 3))
            {
                player.statDefense += 25;
                player.endurance = 0.12f;
                player.GetDamage(DamageClass.Melee) -= 2f;
            }
        }

    }
}
