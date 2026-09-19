using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Magic.Bands
{
    [AutoloadEquip(EquipType.HandsOn)]

    [LegacyName("BandOfSupremeCosmicPower")]
    public class BandOfPhenomenalCosmicPower : ModItem
    {
        public const float LifeRegen = 5f;
        public const int MaxManaIncrease = 80;
        public const int ManaRegen = 45;
        public const float ManaRegenDelay = 130f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen / 2f, MaxManaIncrease, ManaRegen, ManaRegenDelay);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfGreatCosmicPower>());
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 16000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.lifeRegen += (int)LifeRegen;
            player.statManaMax2 += MaxManaIncrease;
            player.manaRegenBonus += ManaRegen;
            player.manaRegenDelayBonus += ManaRegenDelay / 100f;
        }

    }
}
