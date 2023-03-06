using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class HollowSoldierBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Increases damage dealt by 10% multiplicatively" +
                "\nSet Bonus: Nullifies life regen, increases damage dealt by 15% multiplicatively again" +
                "\nIncreases maximum stamina and stamina regen by 10%"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) *= 1.1f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<HollowSoldierHelmet>() && legs.type == ModContent.ItemType<HollowSoldierWaistcloth>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lifeRegen = 0;
            player.GetDamage(DamageClass.Generic) *= 1.15f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1.1f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1.1f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1250);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}