using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Ranged;

namespace tsorcRevamp.Content.Items.Armor.Ranged.KrakenSet;

public class KrakenSetPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void ArmorSetBonusActivated()
    {
        if (Equipped)
        {
            DoKrakenCast();
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.KrakensCast.JustReleased && Equipped)
        {
            DoKrakenCast();
        }
    }
    public void DoKrakenCast()
    {                
        Projectile Tsunami = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Main.MouseWorld, Vector2.Zero,
            ModContent.ProjectileType<KrakenTsunami>(), (int)Player.GetTotalDamage(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseDmg),
            Player.GetTotalKnockback(DamageClass.Ranged).ApplyTo(KrakenCarcass.TsunamiBaseKnockback), Player.whoAmI);
    }
}