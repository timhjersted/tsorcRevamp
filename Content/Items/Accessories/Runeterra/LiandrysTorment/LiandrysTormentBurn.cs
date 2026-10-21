using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.EmptyIcons;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;

public class LiandrysTormentBurn : ModBuff
{
    public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EmptyBuff));

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        npc.GetGlobalNPC<LiandrysTormentNPC>().Burning  = true;
        Dust.NewDust(npc.position, npc.width, npc.height, DustID.FireworkFountain_Blue, 0, 0, 0, default, 0.25f);
    }
}