using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class PolarisLeashDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<PolarisLeashDebuffNPC>().markedByPolarisLeash = true;
		}
	}

	public class PolarisLeashDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByPolarisLeash;

		public override void ResetEffects(NPC npc)
		{
			markedByPolarisLeash = false;
		}

		// TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			int whipDamage = (int)(Main.player[projectile.owner].GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(30)); //30 is half of the base dmg of Polaris Leash
			Vector2 npctopleftvector = new Vector2(-90, -90);
			Vector2 fallingstarvector = new Vector2(15, 15);
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByPolarisLeash && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center + npctopleftvector, fallingstarvector, ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashFallingStar>(), whipDamage, 1f, Main.myPlayer);
				damage += 7;
			}
		}
	}
}
