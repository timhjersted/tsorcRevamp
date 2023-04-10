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
			int DoTPerS = (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(80) + (int)(player.GetTotalCritChance(DamageClass.Ranged) / 100f * 80f);
            if (Electrified)
			{
				npc.lifeRegen -= DoTPerS * 2;
				damage += DoTPerS;
            }
        }
	}
}