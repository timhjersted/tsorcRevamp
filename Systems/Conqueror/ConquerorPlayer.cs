using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Conqueror;

public class ConquerorPlayer : ModPlayer
{
    public bool Conqueror;
    public const float BaseDamageMult = 0.4f;
    public const float MaxDmgMult = 0.801f;
    public const float MaxWhipDmgMult = 1.201f;
    public const int Duration = 3;
    public int Stacks = 0;
    public const int MaxStacks = 12;
    public const float BonusDmgPerStack = (MaxDmgMult - BaseDamageMult) / (float)MaxStacks;
    public const float BonusWhipDmgPerStack = ((MaxWhipDmgMult - BaseDamageMult) / (float)MaxStacks) - ((MaxDmgMult - BaseDamageMult) / (float)MaxStacks);
    public const float FullyStackedRegenBonus = 2f;
    public bool AppliedConqueror;

    public override void ResetEffects()
    {
        Conqueror = false;
    }

    public override void PreUpdateBuffs()
    {
        if (Systems.Conqueror.Conqueror.Enabled)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.HeldItem.DamageType == DamageClass.Summon ||
                    Player.HeldItem.DamageType == DamageClass.SummonMeleeSpeed)
                {
                    if (!Player.HasBuff(ModContent.BuffType<Conqueror>()))
                    {
                        Player.AddBuff(ModContent.BuffType<Conqueror>(), 2);
                    }
                    Conqueror = true;
                    
                    if (Player.ItemAnimationEndingOrEnded) //this resets it perfectly for each swing
                    {
                        AppliedConqueror = false; 
                    }
                }
                Player.GetDamage(DamageClass.Summon) *= BaseDamageMult + (Stacks * BonusDmgPerStack);
                Player.GetDamage(DamageClass.SummonMeleeSpeed) /= BaseDamageMult + (Stacks * BonusDmgPerStack);
                Player.GetDamage(DamageClass.SummonMeleeSpeed) *= BaseDamageMult + (Stacks * BonusDmgPerStack) + (Stacks * BonusWhipDmgPerStack);
                Player.GetDamage(DamageClass.MagicSummonHybrid) /= BaseDamageMult + (Stacks * BonusDmgPerStack); //neutralizing Conqueror damage changes
            }
        }
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (item.DamageType == DamageClass.SummonMeleeSpeed && Conqueror && !AppliedConqueror)
        {
            if (Stacks < MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, Player.Center);
            }
            else if (Stacks == MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, Player.Center);
            }
            Player.AddBuff(ModContent.BuffType<Conqueror>(), Duration * 60);
            AppliedConqueror  = true;
        }
        if (Conqueror && tsorcRevamp.EnemiesOOA.Contains(target.type) && !AppliedConqueror)
        {
            if (Stacks < MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, Player.Center);
            }
            else if (Stacks == MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, Player.Center);
            }
            Player.AddBuff(ModContent.BuffType<Conqueror>(), Duration * 60);
            AppliedConqueror  = true;
        }
    }

    public override void UpdateLifeRegen()
    {
        if (Stacks == MaxStacks)
        {
            Player.lifeRegen += (int)FullyStackedRegenBonus;
        }
    }
}