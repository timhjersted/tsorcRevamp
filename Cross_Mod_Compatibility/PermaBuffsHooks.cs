using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp;
using Terraria.ID;

namespace PermaBuffs
{
    public partial class PermaBuffsPostBuffUpdateHooks
    {
        public static void NeverBuffCurse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
        {
            buffType = ModContent.BuffType<Curse>();
            if (player == null)
                return;

            if (!isPermaBuffed)
            {
                var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

                modPlayer.CurseActive = false;
                modPlayer.powerfulCurseActive = false;
            }
        }
    }
}
