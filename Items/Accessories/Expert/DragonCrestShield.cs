using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.Accessories.Expert;


[AutoloadEquip(EquipType.Shield)]


public class DragonCrestShield : ModItem
{
    public static float damageResistance = 0.75f;
    public static float damageResistance2 = 0.05f;
    public static float damageResistance3 = 0.25f;
    public static int staminaCost = 75;
    public static tsorcRevampStaminaPlayer ModPlayer(Player player)
    {
        return player.GetModPlayer<tsorcRevampStaminaPlayer>();
    }

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Unique shield of a nameless knight" +
                     $"\n[c/ffbf00:Reduces incoming damage by {damageResistance * 100}% when not attacking or {damageResistance3 * 100}% when attacking, but drains {staminaCost} stamina per hit]" +
                     "\nHolding the shield also prevents knockback and protects against fire but reduces stamina regen by 15%" +
                     $"\nGetting hit while low on stamina will stagger you and only reduce damage taken by 5%");
    }

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 28;
        Item.defense = 2;
        Item.rare = ItemRarityID.Green;
        Item.accessory = true;
        Item.expert = true;
        Item.value = PriceByRarity.Green_2;
    }

    public override void UpdateEquip(Player player)
    {
        base.UpdateEquip(player);

        player.GetModPlayer<tsorcRevampPlayer>().DragonCrestShieldEquipped = true;

        player.GetModPlayer<tsorcRevampPlayer>().staminaShield = 1;

        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult -= 0.15f;

        if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75 && player.itemAnimation == 0)
        {
            player.endurance += damageResistance;
            player.buffImmune[BuffID.OnFire] = true;
        }

        if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 75 && player.itemAnimation > 1)
        {
            player.endurance += damageResistance3;
            player.buffImmune[BuffID.OnFire] = true;
        }

        if (player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 75)
        {
            player.noKnockback = false;
            player.endurance += damageResistance2;
            
        }
        else player.noKnockback = true;

        if (player.GetModPlayer<tsorcRevampPlayer>().SmoughShieldSkills)
        {
            staminaCost = 40;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.15f;
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