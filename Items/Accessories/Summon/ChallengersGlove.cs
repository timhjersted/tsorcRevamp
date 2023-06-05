using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Summon
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class ChallengersGlove : ModItem
    {
        public static float SummonDamage = 12;
        public static float SummonAttackSpeed = 12;
        public static float WhipRangeIncrease = 10;
        public static float WhipCritDamage = 12;
        public static float WhipCritHitboxSize = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonDamage, SummonAttackSpeed, WhipRangeIncrease, WhipCritDamage, WhipCritHitboxSize);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.defense = 8;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetKnockback(DamageClass.SummonMeleeSpeed) += 2f;
            player.autoReuseGlove = true;
            player.GetDamage(DamageClass.Summon) += SummonDamage / 100f;
            player.GetAttackSpeed(DamageClass.Summon) += SummonAttackSpeed / 100f;
            player.whipRangeMultiplier += WhipRangeIncrease / 100f;
            player.aggro += 400;
            player.GetModPlayer<tsorcRevampPlayer>().ChallengersGloveCritDamage = true;
            player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize = WhipCritHitboxSize;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AvengerEmblem);
            recipe.AddIngredient(ItemID.BerserkerGlove);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.TinkerersWorkbench);

            recipe.Register();
        }
    }
}