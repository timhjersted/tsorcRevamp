using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Expert;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class MythrilRamDebuff : ModBuff
    {
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<MythrilRamDebuffNPC>().RammedByMythril = true;
        }
    }

    public class MythrilRamDebuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool RammedByMythril;

        public override void ResetEffects(NPC npc)
        {
            RammedByMythril = false;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (RammedByMythril)
            {
                modifiers.FinalDamage *= 1f + MythrilBulwark.Vulnerability / 100f;
            }
        }
    }
}