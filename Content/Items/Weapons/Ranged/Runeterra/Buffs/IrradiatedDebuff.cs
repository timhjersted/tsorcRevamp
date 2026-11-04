using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class IrradiatedDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Irradiated = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Top, 10, 10, DustID.PoisonStaff);
            }
        }
    }
}