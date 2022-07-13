using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [LegacyName("AncientBrassGreaves")]
    [AutoloadEquip(EquipType.Legs)]
    public class BrassGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases minion damage by 2 flat + 8%\nIncreases movement speed by 22%");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 5;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon).Flat += 2f;
            player.GetDamage(DamageClass.Summon) += 0.08f;
            player.moveSpeed += 0.15f;
        }


        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BeeGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

