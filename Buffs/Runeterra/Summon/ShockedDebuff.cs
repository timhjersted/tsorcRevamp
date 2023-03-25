using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
	public class ShockedDebuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = false;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			Player player = Main.player[Main.myPlayer];
			npc.GetGlobalNPC<ShockedDebuffNPC>().Shocked = true; 
			if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.Electric);
            }
        }
	}

	public class ShockedDebuffNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool Shocked;

		public override void ResetEffects(NPC npc)
		{
			Shocked = false;
		}
		public override void UpdateLifeRegen(NPC npc, ref int damage)
		{
            Player player = Main.player[Main.myPlayer];
			if (Shocked)
			{
				npc.lifeRegen -= (int)player.GetDamage(DamageClass.Summon).ApplyTo(55);
			}
        }
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if (Shocked && !projectile.npcProj && !projectile.trap && (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.IsAWhip[projectile.type]))
            {
                if (Main.rand.NextBool(100 / (int)MathF.Round(Main.player[Main.myPlayer].GetTotalCritChance(DamageClass.Generic) / 5f)))
                {
					crit = true;
				}
			}
		}
	}
}