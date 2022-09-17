using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class SmoughArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases ranged damage by 3 flat\nSet Bonus: Grants sandstorm double jump\nReduces chance to consume ammo by 25%");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged).Flat += 2;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<SmoughHelmet>() && legs.type == ModContent.ItemType<SmoughGreaves>();
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
            recipe.AddIngredient(ItemID.FossilShirt, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
