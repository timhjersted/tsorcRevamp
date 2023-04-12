using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class MorningStarTagNerfs : GlobalBuff
	{
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
			if(type == BuffID.MaceWhipNPCDebuff)
			npc.GetGlobalNPC<MorningStarTagNerfsNPC>().markedByMorningStar = true;
		}
	}

	public class MorningStarTagNerfsNPC : GlobalNPC
	{
		// This is required to store information on entities that isn't shared between them.
		public override bool InstancePerEntity => true;

		public bool markedByMorningStar;

		public override void ResetEffects(NPC npc)
		{
			markedByMorningStar = false;
		}

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			// Only player attacks should benefit from this buff, hence the NPC and trap checks.
			if (markedByMorningStar && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type]))
			{
				modifiers.FinalDamage.Flat -= 4;
                if (Main.rand.NextBool(2))
                {
                    modifiers.DisableCrit();
                }
            }
		}
	}
}