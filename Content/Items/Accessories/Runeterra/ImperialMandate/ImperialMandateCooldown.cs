using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.EmptyIcons;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.ImperialMandate;

public class ImperialMandateCooldown : CooldownDebuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EmptyBuff));
    public override bool PlaysSoundOnLastTick => false;
}