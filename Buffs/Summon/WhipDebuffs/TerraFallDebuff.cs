using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class TerraFallDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<TerraFallDebuffNPC>().markedByTerraFall = true;
		}
	}

	public class TerraFallDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByTerraFall;

		public override void ResetEffects(NPC npc)
		{
			markedByTerraFall = false;
		}


		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByTerraFall && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				//Projectile.NewProjectile(Projectile.GetSource_None(), Main.player[projectile.owner].Center, (Main.player[projectile.owner].Center - npc.Center), ModContent.ProjectileType<Projectiles.Summon.Whips.MasterWhipProjectile>(), 200, 1f, Main.myPlayer);
				damage += 20;
				if (Main.rand.NextBool(10))
				{
					crit = true;
				}
			}
		}
	}
}