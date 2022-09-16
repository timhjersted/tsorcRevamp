using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class ArcherOfLumeliaShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Archer of Lumelia Shirt");
            Tooltip.SetDefault("Gifted with lethal archery abilities" +
                "\n25% chance not to consume ammo." +
                "\nSet Bonus: +23% Ranged Crit, +15% Ranged Damage");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ArcherOfLumeliaHairStyle>() && legs.type == ModContent.ItemType<ArcherOfLumeliaPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 23;
            player.GetDamage(DamageClass.Ranged) += 0.15f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBreastplate, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
