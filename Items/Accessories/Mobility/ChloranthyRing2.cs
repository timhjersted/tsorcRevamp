using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class ChloranthyRing2 : ModItem
    {
        // The Chloranthy line is the dedicated stamina-regen accessory and is meant to be the BEST source of it:
        // 25% beats the Bottomless Green Tea Pot's 20%, which is correct because this costs an accessory slot
        // and the pot costs nothing to carry. A no-slot permanent should be the floor, not the ceiling.
        public static float StaminaRecoverySpeed = 25f;
        /// <summary>Percentage points of shield-hold movement slow this ring cancels (see
        /// tsorcRevampActiveShieldPlayer.ApplyBlockSlow). Ring II replaces Ring I rather than stacking.</summary>
        public static float ShieldSlowReduction = 8f;
        /// <summary>Percent cut from the post-spend stamina regen DELAY (tsorcRevampStaminaPlayer.PauseStaminaRegen).</summary>
        public static float RegenDelayReduction = 20f;
        /// <summary>Percentage points added to the normal stamina regen retained while a shield is raised.</summary>
        public static float BlockStaminaRegenBonus = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StaminaRecoverySpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
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
