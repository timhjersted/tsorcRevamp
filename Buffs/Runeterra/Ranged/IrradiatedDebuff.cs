using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class IrradiatedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<IrradiatedDebuffNPC>().Irradiated = true;

			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 10, 10, DustID.PoisonStaff);
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
            var player = Main.LocalPlayer;
            int DoTPerS = (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(220) + (int)(player.GetTotalCritChance(DamageClass.Ranged) / 100f * 220f);
            if (Irradiated)
			{
				npc.lifeRegen -= DoTPerS * 2;
				damage += DoTPerS;
            }
        }
	}
}