﻿using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Stiff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
        }
    }
}