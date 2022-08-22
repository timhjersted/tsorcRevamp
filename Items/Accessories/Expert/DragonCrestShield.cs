using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Accessories.Expert
{

    [AutoloadEquip(EquipType.Shield)]


    public class DragonCrestShield : ModItem
    {
        public static float damageResistance = 0.75f;
        public static float damageResistance2 = 0.10f;
        public static int staminaCost = 75;
        public static tsorcRevampStaminaPlayer ModPlayer(Player player)
        {
            return player.GetModPlayer<tsorcRevampStaminaPlayer>();
        }

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Shield of a nameless knight" +
                         $"\n[c/ffbf00:Reduces incoming damage by {damageResistance * 100}% and protects against fire, but drains {staminaCost} stamina per hit]" +
                         "\nHolding the shield also prevents knockback but reduces stamina regen by 15%" +
                         $"\nGetting hit while low on stamina will stagger you and only reduce damage taken by 10%");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {
            base.UpdateEquip(player);
            player.GetModPlayer<tsorcRevampPlayer>().staminaShield = 1;

            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= 0.15f;

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75)
            {
                player.endurance += damageResistance;
                player.buffImmune[BuffID.OnFire] = true;
            }

            if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 75)
            {
                player.noKnockback = false;
                player.endurance += damageResistance2;
                //player.GetDamage(DamageClass.Ranged) -= 0.25f;
                //player.GetDamage(DamageClass.Magic) -= 0.25f;
                //player.GetDamage(DamageClass.Summon) -= 0.25f;
            }
            else player.noKnockback = true;

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