using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Systems.Electrocute;

public class ElectrocuteCooldown : CooldownDebuff
{
    public override bool PlaysSoundOnLastTick => false; //no sound for now
}