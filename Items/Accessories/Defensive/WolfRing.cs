using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive;

public class WolfRing : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("One of the rings worn by Artorias." +
                            "\nPress the Wolf Ring key to increase life regen and damage taken temporarily" +
                            "\nRemoves the life regen if hit during the effect and puts it on a long cooldown" +
                            "\n+12 defense within the Abyss" +
                            "\nGrants Acid Venom imbue effect" +
                            "\nImbue effect can be toggled by hiding the accessory.");
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.defense = 6;
        Item.accessory = true;
        Item.value = PriceByRarity.Red_10;
        Item.rare = ItemRarityID.Red;
    }


    public override void UpdateEquip(Player player)
    {
        player.GetModPlayer<tsorcRevampPlayer>().WolfRing = true;
        if (Main.bloodMoon)
        { // Apparently this is the flag used in the Abyss?
            player.statDefense += 12;
        }
    }
    public override void UpdateAccessory(Player player, bool hideVisual) {
        if (!hideVisual) player.AddBuff(BuffID.WeaponImbueVenom, 1, false);
    }

}