using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.ImperialMandate;

public class ImperialMandatePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped && proj.IsMinionOrSentryRelated && !target.HasBuff(ModContent.BuffType<ImperialMandateCooldown>()))
        {
            target.AddBuff(ModContent.BuffType<ImperialMandateMark>(), ImperialMandateItem.Duration * 60);
        }
    }

    public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (item.DamageType == DamageClass.SummonMeleeSpeed && Equipped &&
            !target.HasBuff(ModContent.BuffType<ImperialMandateCooldown>()) &&
            target.HasBuff(ModContent.BuffType<ImperialMandateMark>()))
        {
            modifiers.FlatBonusDamage += target.life * ImperialMandateItem.CurrentPercentLifeDmg / 100f;
            target.AddBuff(ModContent.BuffType<ImperialMandateCooldown>(), ImperialMandateItem.Cooldown * 60);
        }
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (proj.DamageType == DamageClass.SummonMeleeSpeed && Equipped &&
            !target.HasBuff(ModContent.BuffType<ImperialMandateCooldown>()) &&
            target.HasBuff(ModContent.BuffType<ImperialMandateMark>()))
        {
            modifiers.FlatBonusDamage += target.life * ImperialMandateItem.CurrentPercentLifeDmg / 100f;
            target.AddBuff(ModContent.BuffType<ImperialMandateCooldown>(), ImperialMandateItem.Cooldown * 60);
        }
    }
}