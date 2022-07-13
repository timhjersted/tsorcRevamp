using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Legs)]
    public class CrystalGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Dazzling armor cut from crystal.\nIncreases minion damage by 10%\nIncreases your max number of minions by 1\n+20% movement speed");
        }

        public override void UpdateEquip(Player player)
        {
                player.GetDamage(DamageClass.Summon) += 0.1f;
                player.maxMinions += 1;
                player.moveSpeed += 0.2f;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

