using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class RedHerosShirt : ModItem
    {
        public static float Dmg = 20f;
        public static float MeleeSpeed = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MeleeSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Yellow;
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
            recipe.AddIngredient(ModContent.ItemType<BlueHerosShirt>()); 
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ItemID.Ectoplasm, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
