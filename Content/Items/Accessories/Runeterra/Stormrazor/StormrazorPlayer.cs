using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.Stormrazor;

public class StormrazorPlayer : ModPlayer
{
    public bool Equipped;
    public float EnergizedStacks = 0;
    public override void ResetEffects()
    {
        Equipped = false;
    }
    public Vector2 LastPosition;
    public override void PostUpdateEquips()
    {
        if (Equipped)
        {
            LastPosition = Player.Center;
        }
    }

    public float Distance;
    public override void PostUpdate()
    {
        if (Equipped)
        {
            Distance = Vector2.Distance(Player.Center, LastPosition);
            if (EnergizedStacks < StormrazorItem.MaxEnergizedStacks)
            {
                EnergizedStacks += Distance / 20f;
            }
            Player.AddBuff(ModContent.BuffType<StormrazorEnergy>(), 2);
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Equipped &&
            (modifiers.DamageType == DamageClass.Melee || modifiers.DamageType == DamageClass.MeleeNoSpeed) &&
            EnergizedStacks >= StormrazorItem.MaxEnergizedStacks)
        {
            modifiers.FlatBonusDamage += StormrazorItem.OnHitFlatBonusDmg;
            modifiers.CritDamage.Flat += StormrazorItem.OnHitFlatBonusDmg;
            Player.AddBuff(ModContent.BuffType<StormrazorProc>(), StormrazorItem.MovespeedDuration * 60);
            EnergizedStacks = 0;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped
            && (hit.DamageType == DamageClass.Melee || hit.DamageType == DamageClass.MeleeNoSpeed)
            && EnergizedStacks < StormrazorItem.MaxEnergizedStacks)
        {
            EnergizedStacks += 3;
        }
    }
}