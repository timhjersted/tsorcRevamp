using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class VenomDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<VenomDebuffNPC>().Venomized = true;

			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Top, 10, 10, DustID.Venom);
			}
		}
	}

	public class VenomDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Venomized;

		public override void ResetEffects(NPC npc)
		{
			Venomized = false;
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			var player = Main.LocalPlayer;
            int DoTPerS = (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(20) + (int)(player.GetTotalCritChance(DamageClass.Ranged) / 100f * 20f);
            if (Venomized)
			{
				npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
        }
	}
}