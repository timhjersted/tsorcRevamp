using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class NightbringerDashCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/Melee/Nightbringer/DashReady";
            LastTickSoundVolume = 2f;
        }
    }
}
