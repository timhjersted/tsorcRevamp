using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.Items.Accessories.Defensive.Shields
{

    [AutoloadEquip(EquipType.Shield)]


    public class DragonCrestShield : ModItem
    {
        public static float damageResistance = 90f;
        public static float damageResistance2 = 8f;
        public static float damageResistance3 = 45f;
        public static int staminaCost = 50;
        public static float BadStaminaRegen = 5f;
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
            Item.defense = 3; //this was at 5 once, ankh shield for reference, a rare and very hard to craft defensive accessory has 4!!! defense
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);

            if (tsorcRevampActiveShieldPlayer.ActiveFor(player))
            {
                // Active Shields Revamp replaces this shield's passive stamina-block with on-demand active
                // blocking; keep only the passive utility (fire + knockback immunity).
                player.buffImmune[BuffID.OnFire] = true;
                player.noKnockback = true;
                return;
            }

            player.GetModPlayer<tsorcRevampPlayer>().DragonCrestShieldEquipped = true;

            player.GetModPlayer<tsorcRevampPlayer>().staminaShield = 1;

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= BadStaminaRegen / 100f;

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 50 && player.itemAnimation == 0)
            {
                player.endurance += damageResistance / 100f;
                player.buffImmune[BuffID.OnFire] = true;
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 50 && player.itemAnimation > 1)
            {
                player.endurance += damageResistance3 / 100f;
                player.buffImmune[BuffID.OnFire] = true;
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 50)
            {
                //player.noKnockback = false; setting this to false cancels out all other immunity applied before this, just not setting it to true here is fine since player is not immune by default
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