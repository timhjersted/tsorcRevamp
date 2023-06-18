using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Items.Accessories.Expert
{

    [AutoloadEquip(EquipType.Shield)]


    public class DragonCrestShield : ModItem
    {
        public static float damageResistance = 75f;
        public static float damageResistance2 = 5f;
        public static float damageResistance3 = 25f;
        public static int staminaCost = 75;
        public static float BadStaminaRegen = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(damageResistance, damageResistance2, damageResistance3, staminaCost, BadStaminaRegen);
        public static tsorcRevampStaminaPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampStaminaPlayer>();
        }

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 5;
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);

            player.GetModPlayer<tsorcRevampPlayer>().DragonCrestShieldEquipped = true;

            player.GetModPlayer<tsorcRevampPlayer>().staminaShield = 1;

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= BadStaminaRegen / 100f;

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75 && player.itemAnimation == 0)
            {
                player.endurance += damageResistance / 100f;
                player.buffImmune[BuffID.OnFire] = true;
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75 && player.itemAnimation > 1)
            {
                player.endurance += damageResistance3 / 100f;
                player.buffImmune[BuffID.OnFire] = true;
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 75)
            {
                player.noKnockback = false;
                player.endurance += damageResistance2 / 100f;
                
            }
            else player.noKnockback = true;

            if (player.GetModPlayer<tsorcRevampPlayer>().SmoughShieldSkills)
            {
                staminaCost = SmoughArmor.StaminaShieldCost;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += BadStaminaRegen / 100f;
            }

        }

        //Drops from Oolicile Demon boss (expert drop)
        /*
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltShield, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        */
    }

}