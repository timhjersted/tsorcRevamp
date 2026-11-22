using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Melee.MagmaSet;

public class MagmaSetNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public bool Ignited;
    public override void ResetEffects(NPC npc)
    {
        Ignited = false;
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (Ignited)
        {
            modifiers.FlatBonusDamage += MagmaHelmet.OnHitDmg;
        }
    }
}