using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class DetonationSignalDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<DetonationSignalDebuffNPC>().markedByDetonationSignal = true;
		}
	}

	public class DetonationSignalDebuffNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByDetonationSignal;

		public override void ResetEffects(NPC npc)
		{
			markedByDetonationSignal = false;
		}

        // TODO: Inconsistent with vanilla, increasing damage AFTER it is randomised, not before. Change to a different hook in the future.
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			int buffIndex = 0;
			if (markedByDetonationSignal && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				damage *= 3;
				if (markedByDetonationSignal && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
                {
					Projectile.NewProjectile(Projectile.GetSource_None(), npc.Top, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT2Explosion, 0, 0, Main.myPlayer);
					SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.6f, PitchVariance = 0.3f });
					npc.AddBuff(ModContent.BuffType<DetonationSignalBuff>(), 4 * 60);
					foreach (int buffType in npc.buffType)
					{
						if (buffType == ModContent.BuffType<DetonationSignalDebuff>())
						{
							npc.DelBuff(buffIndex);
						}
						buffIndex++;
					}
				}
			}
		}
    }
}
