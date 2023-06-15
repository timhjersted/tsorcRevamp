using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class NightbringerDashCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            var dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Torch, Scale: 3f);
            dust.noGravity = true;
        }
    }
}
