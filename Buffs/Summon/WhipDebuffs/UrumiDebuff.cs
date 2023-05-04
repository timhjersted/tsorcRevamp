using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class UrumiDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<UrumiDebuffNPC>().markedByUrumi = true;
		}
	}

	public class UrumiDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool markedByUrumi;

		public override void ResetEffects(NPC npc)
		{
			markedByUrumi = false;
		}
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			if (markedByUrumi && !projectile.npcProj && !projectile.trap && projectile.IsMinionOrSentryRelated)
			{
				modifiers.ArmorPenetration += 5;
			}
		}
	}
}