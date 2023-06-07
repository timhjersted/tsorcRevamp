using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Melee
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class MoltenRing : ModItem
    {
        public static float MeleeDmg = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MagmaStone);
            recipe.AddIngredient(ItemID.HellstoneBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
            player.magmaStone = true;
            player.inferno = true;
        }

    }
}