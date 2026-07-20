using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class FirelinkLeggings : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/FirelinkLeggings";

        public const float MoveSpeed = 12f;
        public const float GenericDamage = 6f;
        public const int MinionSlots = 1;
        public const float StaminaRegen = 10f;
        public const int SoulCost = 80000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, GenericDamage, MinionSlots, StaminaRegen);

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetDamage(DamageClass.Generic) += GenericDamage / 100f;
            player.maxMinions += MinionSlots;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult *= 1f + StaminaRegen / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfCinder>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}