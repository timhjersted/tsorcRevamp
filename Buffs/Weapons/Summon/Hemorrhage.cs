using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class Hemorrhage : ModBuff
    {
        public int BleedStacks = 0;
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (BleedStacks > 8) //9 minimum, since the first hit applying the buff doesn't grant a stack, totaling 10 hits required to apply the effect
            {
                BleedStacks = 0;
                npc.StrikeNPC(npc.CalculateHitInfo(ForgottenImpHalberd.BleedProcBaseDmg / 2, 0, true, 0, DamageClass.Summon, false), false, true);
                float SpeedX = Main.rand.NextBool(2) ? 2f : -2f;
                float SpeedY = Main.rand.NextBool(2) ? 2f : -2f;
                Dust.NewDust(npc.TopLeft, npc.width, npc.height, DustID.Blood, SpeedX, SpeedY, 0, default, 2f);
                npc.DelBuff(buffIndex);
            }
        }
        public override bool ReApply(NPC npc, int time, int buffIndex)
        {
            BleedStacks++;
            return false;
        }
    }
}