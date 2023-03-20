using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra
{
	public class IrradiatedByShroomDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			Player player = Main.player[Main.myPlayer];
			npc.GetGlobalNPC<IrradiatedByShroomDebuffNPC>().IrradiatedByShroom = true;
			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 10, 10, DustID.CursedTorch);
			}
		}
	}

	public class IrradiatedByShroomDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool IrradiatedByShroom;

		public override void ResetEffects(NPC npc)
		{
			IrradiatedByShroom = false;
		}
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
            Player player = Main.player[Main.myPlayer];
			if (IrradiatedByShroom)
			{
				npc.lifeRegen -= (int)player.GetDamage(DamageClass.Ranged).ApplyTo(100);
			}
        }
	}
}