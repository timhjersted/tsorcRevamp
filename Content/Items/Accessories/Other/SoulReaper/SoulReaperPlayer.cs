using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Content.Items.Accessories.Other.SoulReaper;

public class SoulReaperPlayer : ModPlayer
{
    public bool EquippedN2;
    public override void ResetEffects()
    {
        EquippedN2 = false;
    }

    public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
    {
        if (EquippedN2 && Main.myPlayer == Player.whoAmI)
        {
            if (!Main.hardMode)
            {
                Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<SoulSickle>(), hurtInfo.SourceDamage * 2, 7f, Player.whoAmI);
            }
            else
            {
                Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<SoulSickle>(), hurtInfo.SourceDamage * 4, 9f, Player.whoAmI);
            }
        }
    }

    public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
    {
        if (EquippedN2 && Main.myPlayer == Player.whoAmI)
        {
            if (!Main.hardMode)
            {
                Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<SoulSickle>(), hurtInfo.SourceDamage * 2, 6f, Player.whoAmI);
            }
            else
            {
                Projectile.NewProjectile(Player.GetSource_Misc("Soul Sickle"), Player.Center, new Vector2(Player.velocity.X * 0.0001f, 0f), ModContent.ProjectileType<SoulSickle>(), hurtInfo.SourceDamage * 4, 8f, Player.whoAmI);
            }
        }
    }
}