using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class RedHerosShirt : ModItem
    {
        public static float Dmg = 20f;
        public static float MeleeSpeed = 20f;
        public static int SoulCost = 15000;
        public static int SoulCost2 = 2;
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
            recipe.AddIngredient(ItemID.SoulofFright, SoulCost2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
