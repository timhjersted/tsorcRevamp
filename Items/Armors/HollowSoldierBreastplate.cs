using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class HollowSoldierBreastplate : ModItem
    {
        public const float Dmg = 20f;
        public const float MaxStaminaAndRegen = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, Dmg / 2, MaxStaminaAndRegen);
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
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<HollowSoldierHelmet>() && legs.type == ModContent.ItemType<HollowSoldierWaistcloth>();
        }
        public override void UpdateArmorSet(Player player)
        {
            if (!player.HasBuff(BuffID.RapidHealing) && player.lifeRegenCount > 0) //Only block life regen if not healing from RapidHealing and if it positive regen. This allows negative regen from debuffs to still deal damage.
            {
                player.lifeRegenCount = 0;
                player.lifeRegenTime = 0;
            }
            player.GetCritChance(DamageClass.Generic) += Dmg / 2;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + MaxStaminaAndRegen / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStaminaAndRegen / 100f;
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