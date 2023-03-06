using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class BlueTearstoneRing2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Blue Tearstone Ring II");
            /* Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis enchanted blue tearstone from Catarina boosts the defence of its wearer by 85 when in danger." +
                                "\nOtherwise, the ring gifts the wearer a normal +30 defense boost." +
                                "\nWhile worn, melee damage is reduced by 200%, making it a ring " +
                                "\nonly suited to mages and other ranged classes."); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
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
            player.GetDamage(DamageClass.Melee) -= 2f;
            player.GetCritChance(DamageClass.Melee) = -50;
            if (player.statLife <= 80)
            {
                player.statDefense += 85;
            }
            else
            {
                player.statDefense += 30;
            }
        }

    }
}
