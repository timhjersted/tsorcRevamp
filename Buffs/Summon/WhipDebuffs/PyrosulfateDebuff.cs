using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class PyrosulfateDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<PyrosulfateDebuffNPC>().markedByPyrosulfate = true;
		}
	}

	public class PyrosulfateDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByPyrosulfate;

		public override void ResetEffects(NPC npc)
		{
			markedByPyrosulfate = false;
		}

		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByPyrosulfate && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				damage += 8;
				if (Main.rand.NextBool(33))
				{
					crit = true;
				}
			}
		}
	}
}