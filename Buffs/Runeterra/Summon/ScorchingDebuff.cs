using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class ScorchingDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<ScorchingDebuffNPC>().Scorched = true;

			if (Main.GameUpdateCount % 5 == 0)
			{
				Dust.NewDust(npc.Center, 10, 10, DustID.GoldFlame);
			}
		}
	}

	public class ScorchingDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Scorched;

		public override void ResetEffects(NPC npc)
		{
			Scorched = false;
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
			var player = Main.LocalPlayer;
			int DoTPerS = (int)player.GetTotalDamage(DamageClass.Summon).ApplyTo(10);

            if (Scorched)
			{
				npc.lifeRegen -= DoTPerS * 2;
				damage += DoTPerS;
			}
        }

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			if (Scorched && projectile.IsMinionOrSentryRelated)
			{
				if (Main.rand.NextBool(100 / (int)(Main.LocalPlayer.GetTotalCritChance(DamageClass.Summon) / 2.5f)))
				{
					modifiers.SetCrit();
				}
			}
		}
	}
}