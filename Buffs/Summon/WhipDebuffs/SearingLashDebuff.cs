using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class SearingLashDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<SearingLashDebuffNPC>().markedBySearingLash = true;
		}
	}

	public class SearingLashDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedBySearingLash;

		public override void ResetEffects(NPC npc)
		{
			markedBySearingLash = false;
		}

		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			int whipDamage = (int)(Main.player[projectile.owner].GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(30)); //30 is the base dmg of Searing Lash
			int tagbonusdamage = 0;
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedBySearingLash && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				if(npc.HasBuff(BuffID.BlandWhipEnemyDebuff))
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
				if (npc.HasBuff(ModContent.BuffType<EnchantedWhipDebuff>()))
				{
					tagbonusdamage += 4;
				}
				if (npc.HasBuff(ModContent.BuffType<PolarisLeashDebuff>()))
				{
					tagbonusdamage += 7;
				}
                if (npc.HasBuff(ModContent.BuffType<NightsCrackerDebuff>()))
                {
                    tagbonusdamage += Projectiles.Summon.Whips.NightsCrackerProjectile.NightCharges * 2;
                }
                if (npc.HasBuff(ModContent.BuffType<PyrosulfateDebuff>()))
				{
					tagbonusdamage += 8;
				}
				if (npc.HasBuff(ModContent.BuffType<DragoonLashDebuff>()))
                {
					tagbonusdamage += 3;
                }
                if (npc.HasBuff(ModContent.BuffType<TerraFallDebuff>()))
                {
                    tagbonusdamage += Projectiles.Summon.Whips.TerraFallProjectile.TerraCharges * 5;
                }
                damage += (int)((projectile.damage + tagbonusdamage) * 0.66f * whipDamage * 0.01);
			}
		}
	}
}
