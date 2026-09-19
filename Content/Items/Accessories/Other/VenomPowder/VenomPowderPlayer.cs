using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Accessories;

namespace tsorcRevamp.Content.Items.Accessories.Other.VenomPowder;

public class VenomPowderPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        ActivateVenomPowderEffect();
    }

    public void ActivateVenomPowderEffect()
    {
        if (!Equipped)
        {
            return;
        }
        
        Vector2 center = Player.Center;

        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.95f }, Player.Center);

        if (Main.myPlayer == Player.whoAmI)
        {
            int baseDamage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(35);
            int finalDamage = Main.DamageVar(baseDamage);

            Projectile.NewProjectile(
                Player.GetSource_Misc("VenomPowder"),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<VenomPowderProjectile>(),
                finalDamage,
                0.8f,
                Player.whoAmI
            );
        }
    }
}