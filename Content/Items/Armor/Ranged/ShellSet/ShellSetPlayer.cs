using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Ranged.ShellSet;

public class ShellSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyShellSetBonus(in int currentHealth, in int threshold)
    {
        Player.ammoCost80 = true;
        
        if (currentHealth <= threshold)
        {
            Player.GetCritChance(DamageClass.Ranged) += MathF.Min(Player.GetWeaponCrit(Player.HeldItem), ShellHelmet.CritCap);

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                    DustID.Torch, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 100, Color.Black, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}