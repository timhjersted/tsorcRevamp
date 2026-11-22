using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Ranged.ShellSet;

namespace tsorcRevamp.Content.Items.Armor.Magic.MimeticSet;

public class MimeticSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyMimeticSetBonus(in int currentHealth, in int threshold)
    {
        if (currentHealth <= threshold)
        {
            Player.manaRegenBonus += MimeticSuit.ManaRegen;
            Player.manaCost -= MimeticHat.ManaCost / 100f;

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                    DustID.Torch, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}