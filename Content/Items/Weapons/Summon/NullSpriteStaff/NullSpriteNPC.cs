using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.NullSpriteStaff;

public class NullSpriteNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public const int DissolveSize = 2000;
    public int[] NullSpriteDissolveDuration = new int[DissolveSize];
    public bool IsDissolving;

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (IsDissolving)
        {
            float dmgCollected = 0;
            foreach (int duration in NullSpriteDissolveDuration)
            {
                if (duration > 0)
                {
                    dmgCollected += NullSpriteStaffItem.DissolveDmgPerStack;
                }
            }
            modifiers.ScalingBonusDamage += dmgCollected / 100f;
        }
    }
}