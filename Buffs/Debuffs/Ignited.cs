using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class Ignited : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<NPCs.tsorcRevampGlobalNPC>().Ignited = true;
            Dust.NewDust(npc.Center, npc.width, npc.height, DustID.GoldFlame, 0, 1, 0, default, 1);
        }
    }
}
