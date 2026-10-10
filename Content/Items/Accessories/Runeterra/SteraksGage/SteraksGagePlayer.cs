using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.SteraksGage;

public class SteraksGagePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void PostUpdateEquips()
    {
        if (Equipped && Player.statLife < (Player.statLifeMax2 * SteraksGageItem.LifeThreshold / 100f) && !Player.HasBuff(ModContent.BuffType<SteraksGageCooldown>()))
        {
            Player.statLife += SteraksGageItem.ShieldHeal;
            SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(SteraksGagePlayer)) + "_Shield") with { Volume = 0.6f }, Player.Center);
            Player.AddBuff(ModContent.BuffType<SteraksGageCooldown>(), SteraksGageItem.Cooldown * 60);
        }
    }
}