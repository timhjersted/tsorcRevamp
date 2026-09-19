using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Accessories;

namespace tsorcRevamp.Content.Items.Accessories.Other.SporePowder;

public class SporePowderPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        ActivateSporePowderEffect();
    }

    public void ActivateSporePowderEffect()
    {
        if (!Equipped)
        {
            return;
        }
        Vector2 center = Player.Center;
        float radius = 120f;

        for (int i = 0; i < 190; i++)
        {
            Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
            int dust = Dust.NewDust(center + offset, 1, 1, 44, 0f, 0f, 75, default, 1.4f);
            Main.dust[dust].velocity = offset.SafeNormalize(Vector2.Zero) * 1f;
            Main.dust[dust].noGravity = false;
        }

        Terraria.Audio.SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.85f }, Player.Center);

        if (Main.myPlayer == Player.whoAmI)
        {
            int baseDamage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(13);
            int finalDamage = Main.DamageVar(baseDamage);

            Projectile.NewProjectile(
                Player.GetSource_Misc("SporePowder"),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<SporePowderProjectile>(),
                finalDamage,
                0.5f,
                Player.whoAmI
            );
        }
    }
}