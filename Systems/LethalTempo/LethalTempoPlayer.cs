using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.LethalTempo;

public class LethalTempoPlayer : ModPlayer
{
    public bool LethalTempo;
    public const float BaseAttackSpeedMult = 0.8f;
    public const float MaxAttackSpeedMult = 1.101f;
    public const int Duration = 5;
    public const int MaxStacks = 15;
    public const float BonusAttackSpeedPerStack = (MaxAttackSpeedMult - BaseAttackSpeedMult) / (float)MaxStacks;
    public const float BoltDmgMult = 10f;
    public const float BoltMaxDistanceBonusDmgMult = 0.5f;
    public const float MaxBoltDmgMult = BoltDmgMult * (1f + BoltMaxDistanceBonusDmgMult);
    public const float StaminaRestorationOnHit = 20f;
    public int Stacks = 0;
    public bool AppliedLethalTempo;
    
    public bool FallOffStarted;

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
                if (Player.HeldItem.DamageType == DamageClass.Melee ||
                    Player.HeldItem.DamageType == DamageClass.MeleeNoSpeed)
                {
                    if (!Player.HasBuff(ModContent.BuffType<LethalTempo>()) && !Player.dead)
                    {
                        Player.AddBuff(ModContent.BuffType<LethalTempo>(), 2);
                    }
                    LethalTempo = true;
                    
                    if (Player.ItemAnimationEndingOrEnded) //this resets it perfectly for each swing
                    {
                        AppliedLethalTempo = false; 
                    }
                    Player.GetAttackSpeed(DamageClass.Melee) *= BaseAttackSpeedMult + 
                                                                (Stacks * BonusAttackSpeedPerStack);
                }
            }
            if (Player.dead)
            {
                Stacks = 0;
            }

        }
    }

    public override void UpdateDead()
    {
        Stacks = 0;
    }

    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (LethalTempo && Player.HasBuff(ModContent.BuffType<LethalTempo>()) && !AppliedLethalTempo)
        {
            if (Stacks < MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(LethalTempo)) + "_Stack") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.002f }, Player.Center);
            }
            else if (Stacks == MaxStacks - 1)
            {
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(LethalTempo)) + "_FullyStacked") with 
                    { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.003f }, Player.Center);
            }
            Player.AddBuff(ModContent.BuffType<LethalTempo>(), Duration * 60);
            FallOffStarted = false;
            AppliedLethalTempo  = true;

            if (Stacks < MaxStacks)
            {
                var staminaPlayer = Player.GetModPlayer<tsorcRevampStaminaPlayer>();
                var modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
                int scaledUseAnimation = (int)(item.useAnimation / Player.GetAttackSpeed(item.DamageType));
                bool isToolWeapon = (item.pick != 0 || item.axe != 0 || item.hammer != 0)
                                    && (tsorcRevamp.WeaponClassifiedTools == null || !tsorcRevamp.WeaponClassifiedTools.Contains(item.type));
                float staminaMult = modPlayer.WeaponStaminaMult(item);
                float staminaCost = tsorcRevampStaminaPlayer.LegacyWeaponStaminaCost(scaledUseAnimation) * staminaMult;

                staminaPlayer.RestoreStamina(staminaCost * StaminaRestorationOnHit / 100f, "LethalTempoStack");
            }
        }

        if (LethalTempo && Player.HasBuff(ModContent.BuffType<LethalTempo>()) && Stacks == MaxStacks)
        {
            int dmg = Math.Max((int)(hit.SourceDamage * BoltDmgMult / 100f), 1);
            Vector2 randPosition = new Vector2(Main.rand.Next(-50, 51), Main.rand.Next(-50, 51));
            Projectile.NewProjectile(Player.GetSource_OnHit(target), Player.Center + randPosition,
                Vector2.Zero, ModContent.ProjectileType<LethalTempoBolt>(),
                dmg, 0, Main.myPlayer, target.whoAmI, 1);
        }
    }
}