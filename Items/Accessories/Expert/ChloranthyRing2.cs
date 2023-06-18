using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Expert
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing2 : ModItem
    {
        public static float StaminaRecoverySpeed = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaRecoverySpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.defense = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.expert = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ChloranthyRing>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRecoverySpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().StaminaReaper = 6;
            player.blackBelt = true;
            player.GetModPlayer<tsorcRevampPlayer>().ChloranthyRing2 = true;
        }

        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            foreach (Item i in player.armor)
            {
                if (i.ModItem is ChloranthyRing)
                {
                    return false;
                }
            }

            return base.CanEquipAccessory(player, slot, modded);
        }

    }
}
