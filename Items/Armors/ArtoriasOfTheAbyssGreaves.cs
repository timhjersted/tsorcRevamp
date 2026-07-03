using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class ArtoriasOfTheAbyssGreaves : ModItem
    {
        public static float MoveSpeed = 22f;
        public static float MeleeSpeed = 14f;
        public static float MagicSpeed = 14f;
        public static float DodgeStaminaCostReduction = 10f;
        public const int SoulCost = 70000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MeleeSpeed, MagicSpeed, DodgeStaminaCostReduction);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = 100000;
            Item.rare = ItemRarityID.Yellow;
            Item.defense = 21;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Magic) += MagicSpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().ArtoriasAbysswalkerDodgeStaminaCostReduction = DodgeStaminaCostReduction;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
