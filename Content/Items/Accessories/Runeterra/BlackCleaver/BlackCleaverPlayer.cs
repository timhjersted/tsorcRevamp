using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.BlackCleaver;

public class BlackCleaverPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        var globalNpc = target.GetGlobalNPC<BlackCleaverNPC>();
        if (globalNpc.HasBuff)
        {
            modifiers.Defense.Flat -= globalNpc.Stacks * BlackCleaverItem.DefenseReductionPerStack;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        var globalNpc = target.GetGlobalNPC<BlackCleaverNPC>();
        if (Equipped && hit.DamageType == DamageClass.Ranged)
        {
            target.AddBuff(ModContent.BuffType<BlackCleaverDebuff>(), BlackCleaverItem.DebuffDuration * 60);
            if (globalNpc.Stacks < BlackCleaverItem.MaxStacks)
            {
                globalNpc.Stacks++;
            }
        }
    }
}