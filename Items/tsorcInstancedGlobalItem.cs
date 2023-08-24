using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Prefixes;
using Terraria.Localization;
using Humanizer;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items
{
    public class tsorcInstancedGlobalItem : GlobalItem
    {
        public byte blessed;
        public float refreshing;
        public Color slashColor = Color.DarkGray;
        public tsorcSlashStyle slashStyle;

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

                if (item.prefix == ModContent.PrefixType<Blessed>()) { tooltipText = Language.GetTextValue("Mods.tsorcRevamp.Prefixes.Blessed.Tooltip").FormatWith(Blessed.BlessedLifeRegen); }
                if (item.prefix == ModContent.PrefixType<Refreshing>()) { tooltipText = Language.GetTextValue("Mods.tsorcRevamp.Prefixes.StaminaRegen.Tooltip").FormatWith(Refreshing.RefreshingPower); }
                if (item.prefix == ModContent.PrefixType<Revitalizing>()) { tooltipText = Language.GetTextValue("Mods.tsorcRevamp.Prefixes.StaminaRegen.Tooltip").FormatWith(Revitalizing.RevitalizingPower); }
                if (item.prefix == ModContent.PrefixType<Invigorating>()) { tooltipText = Language.GetTextValue("Mods.tsorcRevamp.Prefixes.StaminaRegen.Tooltip").FormatWith(Invigorating.InvigoratingPower); }
                
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

        public override void UpdateAccessory(Item item, Player player, bool hideVisual) 
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += refreshing;
        }
    }

    public enum tsorcSlashStyle
    {
        Metal = 0,
        LightMagic = 1,
        DarkMagic = 2,
        Scifi = 3,
        //TODO: Add more as needed, such as firey
    }
}
