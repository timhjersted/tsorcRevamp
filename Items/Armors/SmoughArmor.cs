using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class SmoughArmor : ModItem
    {
        public static int StaminaShieldCost = 40;
        public static float BadAtkSpeedManaCost = 50f;
        public static float CritChance = 100f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BadAtkSpeedManaCost, CritChance);
        public override void SetStaticDefaults()
        {
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
            player.GetModPlayer<tsorcRevampPlayer>().SmoughShieldSkills = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<SmoughHelmet>() && legs.type == ModContent.ItemType<SmoughGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SmoughAttackSpeedReduction = true;
            player.GetAttackSpeed(DamageClass.Generic) *= 1f - BadAtkSpeedManaCost / 100f;
            player.manaCost += BadAtkSpeedManaCost / 100f;
            player.GetCritChance(DamageClass.Generic) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperChainmail);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
