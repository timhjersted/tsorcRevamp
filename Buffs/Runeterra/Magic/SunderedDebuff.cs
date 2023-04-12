using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
	public class SunderedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<SunderedDebuffNPC>().Sundered = true;
		}
	}

	public class SunderedDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Sundered;

		public override void ResetEffects(NPC npc)
		{
			Sundered = false;
		}

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (Sundered && !projectile.npcProj && !projectile.trap && projectile.DamageType == DamageClass.Magic)
            {
                    damage += (int)((float)projectile.damage * 0.2f);
            }
        }
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (Sundered && item.DamageType == DamageClass.Magic)
            {
                    damage += (int)((float)item.damage * 0.2f);
            }
        }
    }
}