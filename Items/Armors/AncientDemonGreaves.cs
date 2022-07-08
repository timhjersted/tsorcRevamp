using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class AncientDemonGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+25% movement speed, can walk on heated grounds.\nIncreases your max number of minions by 1");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.value = 40000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.15f;
            player.fireWalk = true;
            player.maxMinions += 1;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianPants, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

