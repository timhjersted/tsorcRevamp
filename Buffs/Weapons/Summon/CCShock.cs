using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class CCShock : ModBuff
    {
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CCShocked = true;

            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.WitherLightning, newColor: Color.Purple);
            }
        }
    }
}