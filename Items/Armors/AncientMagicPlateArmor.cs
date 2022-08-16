/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientMagicPlateArmor : ModItem //To be reworked
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Fueled by a magical gem in the chest\nIncreases damage by 4 flat" +
                               "\nSet Bonus: Grants 10% increased attack speed" +
                               "\nGrants 5-15% damage reduction upon hitting enemies a few times");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.1f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AncientHornedHelmet>() && legs.type == ModContent.ItemType<AncientMagicPlateGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {


            if (player.dpsDamage > 400 && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingHard>()))
            {
                {
                    player.AddBuff(ModContent.BuffType<Buffs.MagicPlatingHard>(), 1);
                }
            }
            else
            if (player.dpsDamage > 200 && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingMedium>()) && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingHard>()))
            {
                {
                    player.AddBuff(ModContent.BuffType<Buffs.MagicPlatingMedium>(), 1);
                }
            }
            else
            if (player.dpsDamage > 100 && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingLight>()) && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingMedium>()) && !player.HasBuff(ModContent.BuffType<Buffs.MagicPlatingHard>()))
            {
                player.AddBuff(ModContent.BuffType<Buffs.MagicPlatingLight>(), 1);
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBreastplate, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
*/