using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.InfinityEdge;

public class InfinityEdgePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (modifiers.DamageType == DamageClass.Ranged && Equipped)
        {
            modifiers.CritDamage += InfinityEdgeItem.CritDmgIncrease / 100f;
        }
    }
}