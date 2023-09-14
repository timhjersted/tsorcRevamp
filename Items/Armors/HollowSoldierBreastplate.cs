using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class HollowSoldierBreastplate : ModItem
    {
        public static float DmgMult = 15f;
        public static float MaxStamina = 10f;
        public static float StaminaRegen = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgMult, MaxStamina, StaminaRegen);
        public override void SetStaticDefaults()
        {
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
            player.lifeRegenCount = 0;
            player.lifeRegenTime = 0;
            player.GetDamage(DamageClass.Generic) *= 1f + DmgMult / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + MaxStamina / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + StaminaRegen / 100f;
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