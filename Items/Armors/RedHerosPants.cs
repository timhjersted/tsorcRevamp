using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class RedHerosPants : ModItem
    {
        public static float MoveSpeed = 15f;
        public static int MinionSlots = 2;
        public static float MaxStamina = 15f;
        public static float StaminaRegen = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MinionSlots, MaxStamina, StaminaRegen);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 13;
            Item.rare = ItemRarityID.Yellow;
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
            recipe.AddIngredient(ModContent.ItemType<BlueHerosPants>());
            recipe.AddIngredient(ItemID.SoulofFright, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 13000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

