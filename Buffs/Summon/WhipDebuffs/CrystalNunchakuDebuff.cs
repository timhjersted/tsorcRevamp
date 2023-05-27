using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.Summon.Whips;

namespace tsorcRevamp.Buffs.Summon.WhipDebuffs
{
	public class CrystalNunchakuDebuff : ModBuff
	{

        public override void SetStaticDefaults()
		{
			// This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
			// Other mods may check it for different purposes.
			BuffID.Sets.IsAnNPCWhipDebuff[Type] = true;
		}

		public override void Update(NPC npc, ref int buffIndex)
		{
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByCrystalNunchaku = true;
			npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuUpdateTick += 0.0167f;
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuUpdateTick > 15.02f)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuUpdateTick = 0f;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuStacks = 10;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuProc = false;
            }
			if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuUpdateTick >= 5f && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuUpdateTick <= 5.2f)
			{
                Dust.NewDust(npc.TopLeft, 10, 10, DustID.CrystalPulse);
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrystalNunchakuProc = true;
            }
        }
	}
}