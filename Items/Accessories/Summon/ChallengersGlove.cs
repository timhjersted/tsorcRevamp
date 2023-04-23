using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class ChallengersGlove : ModItem
    {
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
            player.GetDamage(DamageClass.Summon) += 0.12f;
            player.GetAttackSpeed(DamageClass.Summon) += 0.12f;
            player.whipRangeMultiplier += 0.1f;
            player.aggro += 400;
            player.GetModPlayer<tsorcRevampPlayer>().WhipCritDamage250 = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AvengerEmblem);
            recipe.AddIngredient(ItemID.BerserkerGlove);
            recipe.AddTile(TileID.TinkerersWorkbench);

            recipe.Register();
        }
    }
}