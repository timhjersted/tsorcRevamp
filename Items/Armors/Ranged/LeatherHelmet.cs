using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [LegacyName("OldStuddedLeatherHelmet")]
    [LegacyName("OldLeatherHelmet")]
    [AutoloadEquip(EquipType.Head)]
    public class LeatherHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases ranged crit by 8%");
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.defense = 2;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<LeatherArmor>() && legs.type == ModContent.ItemType<LeatherGreaves>();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 0.08f;
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.05f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
