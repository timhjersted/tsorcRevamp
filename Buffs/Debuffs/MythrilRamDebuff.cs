using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
	public class MythrilRamDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

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
                modifiers.TargetDamageMultiplier *= 1.2f;
            }
        }
    }
}