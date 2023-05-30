using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class IrradiatedByShroomDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<IrradiatedByShroomDebuffNPC>().IrradiatedByShroom = true;

			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 20, 20, DustID.PoisonStaff);
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
            var player = Main.LocalPlayer;
            int DoTPerS = (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(500) + (int)(player.GetTotalCritChance(DamageClass.Ranged) / 100f * 500f);
            if (IrradiatedByShroom)
			{
				npc.lifeRegen -= DoTPerS * 2;
				damage += DoTPerS;
            }
        }
	}
}