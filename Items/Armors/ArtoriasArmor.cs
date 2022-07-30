using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("ArmorOfArtorias")]
    [AutoloadEquip(EquipType.Body)]
    public class ArtoriasArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Artorias' Armor");
            Tooltip.SetDefault("Enchanted armor of Artorias of the Abyss.\nPossesses the same power as the Titan Glove.\nSet bonus gives +37% damage, +50% move speed, 8 life regen, lava immunity and many other abilities.");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.kbGlove = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
