using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Melee.MagmaSet
{
    public class Ignited : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<MagmaSetNPC>().Ignited = true;
            Dust.NewDust(npc.Center - npc.Size / 2, npc.width, npc.height, DustID.GoldFlame, 0, 1, 0, default, 1);
        }
    }
}
