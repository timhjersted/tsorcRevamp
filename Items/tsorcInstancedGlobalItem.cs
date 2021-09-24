using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.Items
{
	public class tsorcInstancedGlobalItem : GlobalItem
	{
		public byte blessed;

		public tsorcInstancedGlobalItem()
		{
			blessed = 0;
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
			if (!item.social && item.prefix > 0 && (item.prefix == mod.PrefixType("Blessed")))
			{
				int blessedBonus = blessed - Main.cpItem.GetGlobalItem<tsorcInstancedGlobalItem>().blessed;
				if (blessedBonus > 0)
				{
					TooltipLine line = new TooltipLine(mod, "Blessed", "+1 life regen while held")
					{
						isModifier = true
					};
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
	}
}
