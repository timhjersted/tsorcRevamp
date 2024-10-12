using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.Summon;

namespace tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs
{
    public class TerraFallDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // This allows the debuff to be inflicted on NPCs that would otherwise be immune to all debuffs.
            // Other mods may check it for different purposes.
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.buffTime[buffIndex] == 1)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks = 0;
            }
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().TerraFallStacks == TerraFallItem.MaxStacks)
            {
                Dust.NewDust(npc.TopLeft, npc.width, npc.height, DustID.TerraBlade, Main.rand.NextFloat(), Main.rand.NextFloat(), 0, default, 1.25f);
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().markedByTerraFall = true;
            }
        }
    }
}