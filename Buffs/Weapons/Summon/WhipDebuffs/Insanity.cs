using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs
{
    public class Insanity : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Insane = true;
            if (Main.GameUpdateCount % 5 == 0)
            {
                Dust.NewDust(npc.Center, 10, 10, DustID.Asphalt, newColor: Color.Black, Scale: 1.25f);
            }
        }
    }
}