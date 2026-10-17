using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.NullSpriteStaff
{
    public class NullSpriteDissolving : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff"; //enemy only

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            var globalNPC = npc.GetGlobalNPC<NullSpriteNPC>();
            globalNPC.IsDissolving = true;
            for (int index = 1; index < NullSpriteNPC.DissolveSize; index++)
            {
                if (globalNPC.NullSpriteDissolveDuration[index] > 0)
                {
                    globalNPC.NullSpriteDissolveDuration[index]--;
                }
            }

            if (npc.buffTime[buffIndex] == 0)
            {
                for (int index = 0; index < NullSpriteNPC.DissolveSize; index++)
                {
                    if (globalNPC.NullSpriteDissolveDuration[index] > 0)
                    {
                        globalNPC.NullSpriteDissolveDuration[index]--;
                    }
                }
            }
        }
    }
}
