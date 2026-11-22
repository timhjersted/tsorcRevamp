using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.EmptyIcons;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.ImperialMandate;

public class ImperialMandateMark : ModBuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EmptyBuff));
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        if (npc.HasBuff(ModContent.BuffType<ImperialMandateCooldown>()))
        {
            npc.DelBuff(buffIndex);
        }
    }
}