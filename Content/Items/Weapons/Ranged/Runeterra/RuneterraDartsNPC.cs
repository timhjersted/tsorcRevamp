using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra;

public class RuneterraDartsNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public Player LastHitter;
    
    public float DoDOT(NPC npc, bool flag, float baseDmg, bool lessPotency = false, bool isFire = false)
    {
        if (flag && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
        {
            float dotBase = LastHitter.GetTotalDamage(DamageClass.Ranged).ApplyTo(baseDmg * 1.5f) 
                            + LastHitter.GetTotalCritChance(DamageClass.Ranged) / 100f * baseDmg * 1.5f;
            return tsorcRevampGlobalNPC.DoDamageOverTime(npc, dotBase, lessPotency, isFire);
        }
        return 0;
    }
}