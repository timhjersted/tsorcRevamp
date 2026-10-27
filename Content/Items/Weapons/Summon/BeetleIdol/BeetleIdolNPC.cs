using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Summon.BeetleIdol;

public class BeetleIdolNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool CCShocked;
    public Player LastHitter;
    public override void ResetEffects(NPC npc)
    {
        CCShocked = false;
    }
    public float DoDOT(NPC npc, bool flag, float baseDmg, bool lessPotency = false, bool isFire = false)
    {
        if (flag && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
        {
            float dotBase = LastHitter.GetTotalDamage(DamageClass.Summon).ApplyTo(baseDmg);
            return tsorcRevampGlobalNPC.DoDamageOverTime(npc, dotBase, lessPotency, isFire);
        }
        return 0;
    }
}