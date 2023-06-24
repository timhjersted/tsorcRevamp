using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class BlueHerosShirt : ModItem
    {
        public static float Dmg = 30f;
        public static float MeleeSpeed = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MeleeSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 16;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += Dmg / 100f;
            player.GetDamage(DamageClass.Generic) += MeleeSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosShirt, 1);
            recipe.AddIngredient(ItemID.Flipper, 1);
            recipe.AddIngredient(ItemID.DivingHelmet, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.HerosShirt, 1);
            recipe2.AddIngredient(ItemID.DivingGear, 1);
            recipe2.AddIngredient(ItemID.MythrilBar, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.HerosShirt, 1);
            recipe3.AddIngredient(ItemID.JellyfishDivingGear, 1);
            recipe3.AddIngredient(ItemID.MythrilBar, 1);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.Register();

            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient(ItemID.HerosShirt, 1);
            recipe4.AddIngredient(ItemID.ArcticDivingGear, 1);
            recipe4.AddIngredient(ItemID.MythrilBar, 1);
            recipe4.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe4.AddTile(TileID.DemonAltar);
            recipe4.Register();
        }
    }
}
