using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee;

namespace tsorcRevamp.Systems.LethalTempo;

public class LethalTempoPlayer : ModPlayer
{
    public bool LethalTempo;
    public const float BaseAttackSpeedMult = 0.7f;
    public const float MaxAttackSpeedMult = 1.301f;
    public const int Duration = 3;
    public const int MaxStacks = 20;
    public const float BonusAttackSpeedPerStack = (MaxAttackSpeedMult - BaseAttackSpeedMult) / (float)MaxStacks;
    public int Stacks = 0;
    public bool AppliedLethalTempo;

    public List<int> ItemTypeExceptions = new List<int>()
    {
    };

    public override void ResetEffects()
    {
        LethalTempo = false;
    }

    public override void PreUpdateBuffs()
    {
        if (Systems.LethalTempo.LethalTempo.Enabled)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.HeldItem.DamageType == DamageClass.Melee || Player.HeldItem.DamageType == DamageClass.MeleeNoSpeed)
                {
                    Player.GetAttackSpeed(DamageClass.Melee) *= BaseAttackSpeedMult + 
                                                                (Stacks * BonusAttackSpeedPerStack);
                    if (!Player.HasBuff(ModContent.BuffType<LethalTempo>()))
                    {
                        Player.AddBuff(ModContent.BuffType<LethalTempo>(), 2);
                    }
                    LethalTempo = true;
                    
                    if (Player.ItemAnimationEndingOrEnded) //this resets it perfectly for each swing
                    {
                        AppliedLethalTempo = false; 
                    }
                }
            }

        }
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (LethalTempo && Player.HasBuff(ModContent.BuffType<LethalTempo>()) && !AppliedLethalTempo)
        {
            if (Stacks < MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoStack") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.002f }, Player.Center);
            }
            else if (Stacks == MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/LethalTempoFullyStacked") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.003f }, Player.Center);
            }
            Player.AddBuff(ModContent.BuffType<LethalTempo>(), Duration * 60);
            AppliedLethalTempo  = true;
        }
    }
}