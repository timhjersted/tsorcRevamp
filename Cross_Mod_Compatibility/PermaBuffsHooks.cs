using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp;

namespace PermaBuffs
{
    [JITWhenModsEnabled("PermaBuffs")]
    public partial class PermaBuffsHooks
    {
        public static void Curse(Player player, int buffSlotOnPlayer, bool isPermaBuffed, out int buffType)
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
