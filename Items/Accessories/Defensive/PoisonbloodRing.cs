using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class PoisonbloodRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("One of the infamous bite rings commissioned by Sir Arstor of Carim." +
                                "\nDespite the dreadful rumors surrounding its creation, this ring is an unmistakable asset," +
                                "\ndue to its ability to prevent bleeding and becoming poisoned." +
                                "\nIncreases life regeneration by 1"); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 6;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<PoisonbiteRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BloodbiteRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.lifeRegen += 1;
        }

    }
}

