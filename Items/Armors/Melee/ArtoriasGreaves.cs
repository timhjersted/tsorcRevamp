using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("GreavesOfArtorias")]
    [AutoloadEquip(EquipType.Legs)]
    public class ArtoriasGreaves : ModItem
    {
        public static float AtkSpeedMult = 24f;
        public static float MoveSpeed = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeedMult, MoveSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 43;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) *= 1f + AtkSpeedMult / 100f;
            player.moveSpeed += MoveSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BeetleLeggings);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

