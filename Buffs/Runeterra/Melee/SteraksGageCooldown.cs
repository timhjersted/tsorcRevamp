﻿using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class SteraksGageCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/SteraksGageReady") with { Volume = 1f });
            }
        }
    }
}