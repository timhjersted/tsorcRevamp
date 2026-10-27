using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Melee.PortlyPlateSet;

public class PortlyPlateSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyPortlyPlateSetBonus(in int currentHealth, in int threshold)
    {
        Player.lifeRegen += (int)PortlyPlateHelmet.LifeRegen1;
        Player.noKnockback = true;
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<PortlyPlateRollHitbox>()] == 0 && Main.myPlayer == Player.whoAmI)
        {
            Projectile.NewProjectile(Projectile.GetSource_None(), Player.position, Vector2.Zero,
                ModContent.ProjectileType<PortlyPlateRollHitbox>(), PortlyPlateHelmet.BaseDamage, 11.5f, Player.whoAmI);
        }
        if (currentHealth <= threshold)
        {
            Player.lifeRegen += (int)PortlyPlateHelmet.LifeRegen2;
            if (Visual)
            {
                Dust.NewDust(Player.Center, 10, 10, DustID.AmberBolt);
            }
        }
    }
}