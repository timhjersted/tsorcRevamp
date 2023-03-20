using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra
{
	public class IrradiatedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			Player player = Main.player[Main.myPlayer];
			npc.GetGlobalNPC<IrradiatedDebuffNPC>().Irradiated = true;
			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 10, 10, DustID.CursedTorch);
			}
		}
	}

	public class IrradiatedDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Irradiated;

		public override void ResetEffects(NPC npc)
		{
			Irradiated = false;
		}
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
            Player player = Main.player[Main.myPlayer];
			if (Irradiated)
			{
				npc.lifeRegen -= (int)player.GetDamage(DamageClass.Ranged).ApplyTo(50);
			}
        }
	}
}