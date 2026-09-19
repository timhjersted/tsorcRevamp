using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Melee
{
    public class BerserkerEmblem : ModItem
    {
        public static float MeleeDmgCrit = 12f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeDmgCrit);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.WarriorEmblem);
            recipe.AddIngredient(ItemID.EyeoftheGolem);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDmgCrit / 100f;
            player.GetCritChance(DamageClass.Melee) += MeleeDmgCrit;
        }
    }
}