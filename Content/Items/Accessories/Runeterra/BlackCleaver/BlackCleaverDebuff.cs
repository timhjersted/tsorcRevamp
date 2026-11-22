using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.EmptyIcons;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.BlackCleaver;

public class BlackCleaverDebuff : ModBuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EmptyBuff));
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        var globalNpc = npc.GetGlobalNPC<BlackCleaverNPC>();
        globalNpc.HasBuff = true;
        if (npc.buffTime[buffIndex] == 0)
        {
            globalNpc.Stacks = 0;
        }
    }
}