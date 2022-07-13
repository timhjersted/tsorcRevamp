using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class ProtectRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Ring that guards against death." +
                                "\nPuts \"protect\" on wearer (+30 defense).");
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ItemID.Emerald, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 20);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.Protect>(), 60, false);
        }

    }
}

