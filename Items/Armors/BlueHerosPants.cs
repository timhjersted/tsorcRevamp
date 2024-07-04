using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class BlueHerosPants : ModItem
    {
        public static float MoveSpeed = 10f;
        public static int MinionSlots = 1;
        public static float MaxStamina = 11f;
        public static float StaminaRegen = 11f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MinionSlots, MaxStamina, StaminaRegen);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.maxMinions += MinionSlots;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosPants, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

