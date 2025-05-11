using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
    public class WitchkingsGrasp : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().WitchkingsGrasp = true;
        }
    }
}