using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("AncientDwarvenArmor")]
    [AutoloadEquip(EquipType.Body)]
    public class AncientGoldenArmor : ModItem
    {
        public static float AtkSpeed = 11f;
        public static int FlatDmg = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed, FlatDmg);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<AncientGoldenHelmet>() && legs.type == ModContent.ItemType<AncientGoldenGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Melee).Flat += FlatDmg;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldChainmail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
