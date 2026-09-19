using Terraria;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Magic;

namespace tsorcRevamp.Systems.Electrocute;

public class ElectrocuteCooldown : CooldownDebuff
{
    public override bool PlaysSoundOnLastTick => true;
    public override void LastTickSoundsSettings(out float soundVolume)
    {
        LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(Electrocute)) + "_Ready";
        soundVolume = 2f;
    }
}