using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Bands
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class BandOfCosmicPower : ModItem
    {
        public static int LifeRegen = 2;
        public static int MaxManaIncrease = 40;
        public static int ManaRegen = 25;
        public static float ManaRegenDelay = 100f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen, MaxManaIncrease, ManaRegen, ManaRegenDelay);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ManaRegenerationBand);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += LifeRegen;
            player.statManaMax2 += MaxManaIncrease;
            player.manaRegenBonus += ManaRegen;
            player.manaRegenDelayBonus += ManaRegenDelay / 100f;
        }

    }
}
