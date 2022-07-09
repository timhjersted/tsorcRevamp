using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("OldStuddedLeatherArmor")]
    [LegacyName("OldLeatherArmor")]
    [AutoloadEquip(EquipType.Body)]
    public class LeatherArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases ranged damage by 1 flat\nSet bonus: +5% Ranged Damage, 20% less chance to consume ammo");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged).Flat += 1;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
