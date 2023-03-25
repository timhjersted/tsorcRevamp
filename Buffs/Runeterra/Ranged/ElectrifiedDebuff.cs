using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
	public class ElectrifiedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			Player player = Main.player[Main.myPlayer];
			npc.GetGlobalNPC<ElectrifiedDebuffNPC>().Electrified = true;
			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 10, 10, DustID.Electric);
			}
		}
	}

	public class ElectrifiedDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Electrified;

		public override void ResetEffects(NPC npc)
		{
			Electrified = false;
		}
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
            Player player = Main.player[Main.myPlayer];
			if (Electrified)
			{
				npc.lifeRegen -= (int)player.GetDamage(DamageClass.Ranged).ApplyTo(40);
			}
        }
	}
}