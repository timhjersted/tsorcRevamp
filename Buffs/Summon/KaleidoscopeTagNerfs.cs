using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class KaleidoscopeTagNerfs : GlobalBuff
	{
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
			if(type == BuffID.RainbowWhipNPCDebuff)
			npc.GetGlobalNPC<KaleidoscopeTagNerfsNPC>().markedByKaleidoscope = true;
		}
	}

	public class KaleidoscopeTagNerfsNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByKaleidoscope;

		public override void ResetEffects(NPC npc)
		{
			markedByKaleidoscope = false;
		}

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByKaleidoscope && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				damage -= 10;
				if(crit)
				{
					if (Main.rand.NextBool(2))
					{
						crit = false;
					}
				}
			}
		}
	}
}
