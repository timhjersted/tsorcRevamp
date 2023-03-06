using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("GreavesOfArtorias")]
    [AutoloadEquip(EquipType.Legs)]
    public class ArtoriasGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Artorias' Greaves");
            /* Tooltip.SetDefault("Enchanted armor of Artorias." +
                "\nIncreases maximum mana by 100" +
                "\nIncreases your movement speed by 50%" +
                "\nIncreases your attack speed by 24%(doubled for melee)"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 100;
            player.moveSpeed += 0.5f;
            player.GetAttackSpeed(DamageClass.Generic) += 0.24f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.24f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

