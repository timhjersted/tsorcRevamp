using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Items;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfSpirituality;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra;

public class RuneterraOrbPlayer : ModPlayer
{
    public int EssenceThief = 0;
    public int SpiritRushCharges = 3;
    public float SpiritRushTimer = 0f;
    public int SpiritRushSoundStyle = 0;
    public float SpiritRushCooldown = 0f;
    public Vector2 SpiritRushVelocity;

    public override void PreUpdateMovement()
    {
        if (SpiritRushTimer > 0f)
        {
            Player.velocity = SpiritRushVelocity;
            Player.RefreshMovementAbilities();
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.specialAbility.JustReleased)
        {
                if (Player.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>() && Player.statMana >= (Player.GetManaCost(Player.HeldItem) * OrbOfSpirituality.DashCostMultiplier) && !Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDashCooldown>()))
                {
                    Player.AddBuff(ModContent.BuffType<OrbOfSpiritualityDash>(), OrbOfSpirituality.DashBuffDuration * 60);
                    Player.statMana -= Player.GetManaCost(Player.HeldItem) * OrbOfSpirituality.DashCostMultiplier;
                }
                if (Player.HasBuff(ModContent.BuffType<OrbOfSpiritualityDash>()) && SpiritRushCooldown <= 0f && SpiritRushCharges > 0)
                {
                    Player.immune = true;
                    SpiritRushVelocity = Player.DirectionTo(Main.MouseWorld) * 25f;
                    SpiritRushTimer = 0.3f;
                    SpiritRushCooldown = 1f;
                    Player.SetImmuneTimeForAllTypes(60);
                    if (SpiritRushSoundStyle == 0)
                    {
                        SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash1") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 1)
                    {
                        SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash2") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle += 1;
                    }
                    else
                    if (SpiritRushSoundStyle == 2)
                    {
                        SoundEngine.PlaySound(new SoundStyle( UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_Dash3") with { Volume = RuneterraOrb.OrbSoundVolume });
                        SpiritRushSoundStyle = 0;
                    }
                    SpiritRushCharges--;
                }
        }
    }
}