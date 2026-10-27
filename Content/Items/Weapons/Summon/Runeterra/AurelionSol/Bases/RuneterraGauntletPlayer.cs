using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.InterstellarVessel;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;

public class RuneterraGauntletPlayer : ModPlayer
{
    public int RuneterraMinionHitSoundCooldown = 0;
    public bool Turboboost = false;
    public int TurboboostControlsCooldown = 0; //so you don't accidentally active it while trying to adjust radius
    public int MinimumMinionCircleRadius = 150;
    public int MaximumMinionCircleRadius = 500;
    public float MinionCircleRadius = 150;
    public int CenterOfTheUniverseStardustCount = 0;
    
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.specialAbility.JustReleased)
        {
                bool holdingControlsAndOrSummonWeapon = (Player.HasItem(ModContent.ItemType<InterstellarVesselGauntlet>()) || Player.HasItem(ModContent.ItemType<CenterOfTheUniverse>())) 
                                                        && (Player.HeldItem.DamageType == DamageClass.SummonMeleeSpeed || Player.HeldItem.DamageType == DamageClass.Summon);
                bool hasBuff = Player.HasBuff(ModContent.BuffType<InterstellarCommander>()) || Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                bool hasBoost = Player.HasBuff(ModContent.BuffType<Turboboost>())
                                          || Player.HasBuff(ModContent.BuffType<TurboboostUniversal>());
                bool hasCooldown = Player.HasBuff(ModContent.BuffType<TurboboostCooldown>())
                                   || Player.HasBuff(ModContent.BuffType<TurboboostUniversalCooldown>());
                
                if (holdingControlsAndOrSummonWeapon && hasBuff && !(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) 
                    && !hasBoost && !hasCooldown && TurboboostControlsCooldown < 0)
                {
                    bool hasCenterOfTheUniverse = Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                    
                    if (!hasCenterOfTheUniverse)
                    {
                        Player.AddBuff(ModContent.BuffType<Turboboost>(), RuneterraGauntlets.BoostDuration * 60);
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(InterstellarVesselSound)) + "_BoostActivation") with { Volume = InterstellarVesselGauntlet.SoundVolume });
                    }

                    if (hasCenterOfTheUniverse)
                    {
                        Player.AddBuff(ModContent.BuffType<TurboboostUniversal>(), RuneterraGauntlets.BoostDuration * 60);
                        SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound)) + "_BoostActivation") with { Volume = CenterOfTheUniverse.SoundVolume });
                    }
                }
                else if (holdingControlsAndOrSummonWeapon && hasBuff 
                         && !(Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) && hasBoost)
                {
                    bool hasCenterOfTheUniverse = Player.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());
                    
                    if (!hasCenterOfTheUniverse)
                    {
                        int buffIndex = Player.FindBuffIndex(ModContent.BuffType<Turboboost>());
                        Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs.Turboboost.OnRemoval(buffIndex, Player);
                        Player.DelBuff(buffIndex);
                    }

                    if (hasCenterOfTheUniverse)
                    {
                        int buffIndex = Player.FindBuffIndex(ModContent.BuffType<TurboboostUniversal>());
                        TurboboostUniversal.OnRemoval(buffIndex, Player);
                        Player.DelBuff(buffIndex);
                    }
                }
        }


        if (tsorcRevamp.specialAbility.Current && (Main.keyState.IsKeyDown(Keys.LeftAlt) || Main.keyState.IsKeyDown(Keys.RightAlt)) 
                                               && (Player.HeldItem.type == ModContent.ItemType<ScorchingPoint>() 
                                                   || Player.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() 
                                                   || Player.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>()))
        {
            if (Player.direction == 1)
            {
                MinionCircleRadius -= 1.5f;
                if (MinionCircleRadius < MinimumMinionCircleRadius)
                {
                    MinionCircleRadius = MinimumMinionCircleRadius;
                }
            }
            else
            {
                MinionCircleRadius += 1.5f;
                if (MinionCircleRadius > MaximumMinionCircleRadius)
                {
                    MinionCircleRadius = MaximumMinionCircleRadius;
                }
            }
            Dust.NewDustDirect(Player.Center, 10, 10, DustID.FlameBurst, 0.5f, 0.5f, 0, Color.Firebrick, 0.5f);
            TurboboostControlsCooldown = 61; //so you don't accidentally activate Interstellar Boost when you jsut wanted to adjust the circle radius
        }
    }
}