using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Runeterra.Stormrazor.Sounds;

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
                EnergizedStacks += Distance / StormrazorItem.EnergizedStackDivisor;
                if (EnergizedStacks >= StormrazorItem.MaxEnergizedStacks)
                {
                    SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(StormrazorSound)) + "_Ready") with 
                        { Volume = 0.5f }, Player.Center);
                }
            }
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
            Player.AddBuff(ModContent.BuffType<StormrazorEnergized>(), StormrazorItem.MovespeedDuration * 60);
            EnergizedStacks = 0;
            int rando = Main.rand.Next(1, 5);
            SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(StormrazorSound)) + "_Hit" + rando) with 
                { Volume = 0.8f }, Player.Center);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped
            && (hit.DamageType == DamageClass.Melee || hit.DamageType == DamageClass.MeleeNoSpeed)
            && EnergizedStacks < StormrazorItem.MaxEnergizedStacks)
        {
            EnergizedStacks += 5;
            if (EnergizedStacks >= StormrazorItem.MaxEnergizedStacks)
            {
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(StormrazorSound)) + "_Ready") with 
                    { Volume = 0.5f }, Player.Center);
            }
        }
    }
}