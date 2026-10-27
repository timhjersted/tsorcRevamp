using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;

public class RuneterraOrbNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    
    public bool Sundered;
    public override void ResetEffects(NPC npc)
    {
        Sundered = false;
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (Sundered && modifiers.DamageType == DamageClass.Magic)
        {
            modifiers.FinalDamage *= 1f + OrbOfFlame.MagicSunder / 100f;
        }
    }
}