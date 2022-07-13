using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class MirkwoodElvenLeatherArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Gifted with ranged combat and healing. High defense not necessary.\n25% chance not to consume ammo\nSet enchantment adds 20% Ranged Crit, +20 Ranged Dmg, +9 Life Regen\nAn even more powerful set when combined with the right accessories");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
