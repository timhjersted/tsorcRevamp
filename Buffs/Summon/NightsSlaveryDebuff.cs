using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class NightsSlaveryDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<NightsSlaveryDebuffNPC>().markedByNightsSlavery = true;
		}
	}

	public class NightsSlaveryDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByNightsSlavery;

		public override void ResetEffects(NPC npc)
		{
			markedByNightsSlavery = false;
		}

		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByNightsSlavery && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				int whipDamage = (int)(Main.player[projectile.owner].GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(42)); //42 is the base dmg of the Searing Lash
				int tagbonusdamage = 0;
				if (npc.HasBuff(BuffID.BlandWhipEnemyDebuff))
				{
					tagbonusdamage += 4;
				}
				if (npc.HasBuff(BuffID.ThornWhipNPCDebuff))
				{
					tagbonusdamage += 6;
				}
				if (npc.HasBuff(BuffID.BoneWhipNPCDebuff))
				{
					tagbonusdamage += 7;
				}
				if (npc.HasBuff(BuffID.SwordWhipNPCDebuff))
				{
					tagbonusdamage += 9;
				}
				if (npc.HasBuff(BuffID.MaceWhipNPCDebuff))
				{
					tagbonusdamage += 5;
				}
				if (npc.HasBuff(BuffID.RainbowWhipNPCDebuff))
				{
					tagbonusdamage += 20;
				}
				if (npc.HasBuff(ModContent.BuffType<EnchantedWhipDebuff>()))
				{
					tagbonusdamage += 4;
				}
				if (npc.HasBuff(ModContent.BuffType<PolarisLeashDebuff>()))
				{
					tagbonusdamage += 7;
				}
				if (npc.HasBuff(ModContent.BuffType<NightsSlaveryDebuff>()))
				{
					tagbonusdamage += 5;
				}
				if (npc.HasBuff(ModContent.BuffType<PyrosulfateDebuff>()))
				{
					tagbonusdamage += 8;
				}
				damage += (int)((projectile.damage + tagbonusdamage) * 0.42f * whipDamage * 0.01);
				damage += 5;
				if (Main.rand.NextBool(33))
				{
					crit = true;
				}
			}
		}
	}
}
