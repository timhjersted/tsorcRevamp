using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
	public class SunburnDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			Player player = Main.player[Main.myPlayer];
			npc.GetGlobalNPC<SunburnDebuffNPC>().Sunburnt = true;
            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 20, 20, DustID.GoldFlame);
            }
        }
	}

	public class SunburnDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Sunburnt;

		public override void ResetEffects(NPC npc)
		{
			Sunburnt = false;
		}
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
            Player player = Main.player[Main.myPlayer];
            int DoTPerS = (int)player.GetTotalDamage(DamageClass.Summon).ApplyTo(110);
            if (Sunburnt)
			{
				npc.lifeRegen -= DoTPerS * 2;
				damage += DoTPerS;
			}
        }
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		{
			if (Sunburnt && projectile.IsMinionOrSentryRelated)
            {
                if (Main.rand.NextBool(100 / (int)(Main.player[Main.myPlayer].GetTotalCritChance(DamageClass.Summon) / 2.5f)))
                {
                    modifiers.SetCrit();
                }
            }
		}
	}
}