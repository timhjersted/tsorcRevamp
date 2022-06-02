using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class AnkorWatLeggings : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+20% Movespeed. +30% Magic Damage.");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.value = 100000;
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.20f;
            player.GetDamage(DamageClass.Magic) += 0.30f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

