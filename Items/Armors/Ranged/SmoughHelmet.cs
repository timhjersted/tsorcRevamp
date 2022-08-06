using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    public class SmoughHelmet : ModItem 
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases ranged crit by 12%");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 5;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 12;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<SmoughArmor>() && legs.type == ModContent.ItemType<SmoughGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.hasJumpOption_Sandstorm = true;
            player.GetDamage(DamageClass.Ranged) += 0.1f;
            player.ammoCost75 = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FossilHelm, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
