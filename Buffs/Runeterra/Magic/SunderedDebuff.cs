using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
	public class SunderedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<SunderedDebuffNPC>().Sundered = true;
		}
	}

	public class SunderedDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Sundered;

		public override void ResetEffects(NPC npc)
		{
			Sundered = false;
		}
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
			if (Sundered && modifiers.DamageType == DamageClass.Magic)
            {
                modifiers.TargetDamageMultiplier *= 1.2f;
            }
        }
    }
}