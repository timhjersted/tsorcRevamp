using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    class DwarvenArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases minion damage by 12%\nIncreases your max number of minions by 1");
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 18;
            Item.value = 12000;
            Item.rare = ItemRarityID.Pink;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.12f;
            player.maxMinions += 1;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
