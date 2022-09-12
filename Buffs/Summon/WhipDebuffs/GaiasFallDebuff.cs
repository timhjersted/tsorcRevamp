using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class GaiasFallDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<GaiasFallDebuffNPC>().markedByGaiasFall = true;
		}
	}

	public class GaiasFallDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByGaiasFall;

		public override void ResetEffects(NPC npc)
		{
			markedByGaiasFall = false;
		}


		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByGaiasFall && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				damage += 15;
				if (Main.rand.NextBool(14))
				{
					crit = true;
				}
			}
		}
	}
}