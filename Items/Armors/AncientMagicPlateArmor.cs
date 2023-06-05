using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientMagicPlateArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Fueled by a magical gem in the chest\nIncreases all attack speed by 12%(doubled for melee)" +
                               "\nSet Bonus: Chance to gain stacks upon damaging anything" +
                               "\nCollect stacks to gain up to 28% damage reduction against the next hit"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 12;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.12f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.12f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AncientHornedHelmet>() && legs.type == ModContent.ItemType<AncientMagicPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.AddBuff(ModContent.BuffType<MagicPlating>(), 2);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBreastplate, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}