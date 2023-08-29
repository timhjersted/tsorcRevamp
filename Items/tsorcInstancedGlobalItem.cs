using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Prefixes;

namespace tsorcRevamp.Items;

public class tsorcInstancedGlobalItem : GlobalItem
{
    public byte blessed;
    public float refreshing;

    public tsorcInstancedGlobalItem()
    {
        blessed = 0;
        refreshing = 0;
    }

    public override bool InstancePerEntity => true;

    public override GlobalItem Clone(Item item, Item itemClone)
    {
        tsorcInstancedGlobalItem myClone = (tsorcInstancedGlobalItem)base.Clone(item, itemClone);
        myClone.blessed = blessed;
        return myClone;
    }

    public override int ChoosePrefix(Item item, UnifiedRandom rand)
    {
        /*if ((item.damage > 0) && item.maxStack == 1 && rand.NextBool(30)) //We don't want it ever rolling, only via the Emerald Herald
			{
				return mod.PrefixType("Blessed");
			}*/

        return -1;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (!item.social && item.prefix > 0) {
            string tooltipText = null;

            if (item.prefix == ModContent.PrefixType<Blessed>()) { tooltipText = "+1 life regen while held"; }
            if (item.prefix == ModContent.PrefixType<Refreshing>()) { tooltipText = "+4% stamina recovery speed"; }
            if (item.prefix == ModContent.PrefixType<Revitalizing>()) { tooltipText = "+6% stamina recovery speed"; }
            if (item.prefix == ModContent.PrefixType<Invigorating>()) { tooltipText = "+8% stamina recovery speed"; }
            
            if (tooltipText != null) {
                TooltipLine line = new TooltipLine(Mod, "tsorcRevamp:Prefix", tooltipText) { IsModifier = true };
                tooltips.Add(line);
            }
        }
    }

    public override void NetSend(Item item, BinaryWriter writer)
    {
        writer.Write(blessed);
    }

    public override void NetReceive(Item item, BinaryReader reader)
    {
        blessed = reader.ReadByte();
    }

    public override void UpdateAccessory(Item item, Player player, bool hideVisual) {
        player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += refreshing;
    }
}
