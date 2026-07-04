using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class ArtoriasOfTheAbyssArmor : ModItem
    {
        public static float MeleeCritChance = 20f;
        public static float MagicCritChance = 20f;
        public static float DR = 8f;
        public static float MaxStamina = 15f;
        public const int SoulCost = 70000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeCritChance, MagicCritChance, DR, MaxStamina);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = 170000;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.defense = 22;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += MeleeCritChance;
            player.GetCritChance(DamageClass.Magic) += MagicCritChance;
            player.endurance += DR / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
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
