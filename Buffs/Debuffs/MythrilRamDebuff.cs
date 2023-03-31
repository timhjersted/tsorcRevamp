using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
	public class MythrilRamDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<MythrilRamDebuffNPC>().RammedByMythril = true;
		}
	}

	public class MythrilRamDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool RammedByMythril;

		public override void ResetEffects(NPC npc)
		{
			RammedByMythril = false;
		}

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (RammedByMythril && !projectile.npcProj && !projectile.trap)
            {
                    damage += (int)((float)projectile.damage * 0.2f);
            }
        }
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (RammedByMythril)
            {
                    damage += (int)((float)item.damage * 0.2f);
            }
        }
    }
}