﻿using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class RadioactiveBlindingLaserCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/Ranged/OmegaSquadRifle/BlindingLaserReady";
            LastTickSoundVolume = 1f;
        }
    }
}
