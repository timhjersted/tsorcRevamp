using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Magic.CelestialCloak;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems.CeruleanFlask;


public class CeruleanFlaskItems : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        Player player = Main.LocalPlayer;
        var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
        if (modPlayer.SoulsMode && player.whoAmI == Main.myPlayer && (item.type == ItemID.ManaFlower || item.type == ItemID.ArcaneFlower || item.type == ItemID.MagnetFlower || item.type == ItemID.ManaCloak || item.type == ModContent.ItemType<CelestialCloakItem>()))
        {
            int add = item.type == ModContent.ItemType<CelestialCloakItem>() ? 10 : 2;
            tooltips.Insert(ttindex + add, new TooltipLine(Mod, "DrinkTime", LangUtils.GetTextValue("CommonItemTooltip.CeruleanManaFlower", CeruleanFlaskPlayer.CeruleanManaFlowerStrength)));
        }
        if (modPlayer.SoulsMode && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaRegenerationPotion && ttindex != -1)
        {
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "RestorationTime", LangUtils.GetTextValue("Items.VanillaItems.CeruleanManaRegenerationPotion", CeruleanFlaskPlayer.ManaRegenPotRestorationTimerBonus)));
        }
    }
}