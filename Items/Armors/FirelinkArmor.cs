using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class FirelinkArmor : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Armors/FirelinkArmor";

        public const float GenericDamage = 12f;
        public const float Endurance = 8f;
        public const int MinionSlots = 1;
        public const float MaxStamina = 15f;
        public const int SoulCost = 80000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(GenericDamage, Endurance, MinionSlots, MaxStamina);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.defense = 36;
            Item.rare = ItemRarityID.Expert;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += GenericDamage / 100f;
            player.endurance += Endurance / 100f;
            player.maxMinions += MinionSlots;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
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