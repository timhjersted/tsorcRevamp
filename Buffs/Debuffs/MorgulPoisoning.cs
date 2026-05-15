using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Items.Accessories.Damage;

namespace tsorcRevamp.Buffs.Debuffs
{
	public class MorgulPoisoning : ModBuff
	{
		public override void SetStaticDefaults() 
        {
			Main.debuff[Type] = true;  
			Main.pvpBuff[Type] = false; 
			Main.buffNoSave[Type] = true; 
			BuffID.Sets.BuffTimeIsExtendedWithGameDifficulty[Type] = true; 
		}

		public override void Update(NPC npc, ref int buffIndex)
        {
			npc.GetGlobalNPC<MorgulPoisoningGlobalNPC>().lifeRegenDebuff = true;
		}
	}

	public class MorgulPoisoningGlobalNPC : GlobalNPC
	{
		public bool lifeRegenDebuff;

		public override bool InstancePerEntity => true; 

		public override void ResetEffects(NPC npc)
        {
			lifeRegenDebuff = false;
		}
		

		public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
			if (lifeRegenDebuff)
            {
				if (npc.lifeRegen > 0)
					npc.lifeRegen = 0;

				npc.lifeRegen -= 300; 

				if (damage < 30)
                {
					damage = 30; 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency / 2;
					damage = 60;
				}

                if (Main.rand.NextBool(10))
                { 
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.ToxicBubble, npc.velocity.X * 0.2f, npc.velocity.Y * 0.2f, 100, Color.Green, 1.5f);
                }
			}
		}
        public override void DrawEffects(NPC npc, ref Color drawColor) 
        {
            if (lifeRegenDebuff)
            {
                drawColor = Color.Lerp(drawColor, Color.Green, 0.25f);

                if (Main.rand.NextBool(5)) 
                { 
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, DustID.ToxicBubble, 0f, 0f, 100, Color.Green, 2f);
                    Main.dust[dustIndex].noGravity = true; 
                    Main.dust[dustIndex].velocity *= 0.75f;
                    Main.dust[dustIndex].fadeIn = 1.3f; 
                }
            }
        }
	}
}