using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class PoisonbloodRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("One of the infamous bite rings commissioned by Sir Arstor of Carim." +
                                "\nDespite the dreadful rumors surrounding its creation, this ring is an unmistakable asset," +
                                "\ndue to its ability to prevent bleeding and becoming poisoned." +
                                "\n+2 HP Regeneration and 6 defense");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.lifeRegen = 2;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("PoisonbiteRing").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("BloodbiteRing").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.statDefense += 6;
        }

    }
}

