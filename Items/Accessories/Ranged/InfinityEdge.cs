using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Ranged
{
    public class InfinityEdge : ModItem
    {
        public static float CritDmgIncrease = 20f;
        public static float Dmg = 4f;
        public static float Crit = 4f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritDmgIncrease, Dmg, Crit);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronBroadsword);
            recipe.AddIngredient(ItemID.IronPickaxe);
            recipe.AddIngredient(ItemID.HunterCloak);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3400);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().InfinityEdge = true;
            player.GetDamage(DamageClass.Ranged) += Dmg / 100f;
            player.GetCritChance(DamageClass.Ranged) += Crit;
        }

    }
}