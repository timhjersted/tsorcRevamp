using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Melee.CrystalSet;

public class CrystalEnchantedSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyCrystalEnchantedSetBonus(in int currentHealth, in int threshold)
    {
        Player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
        Player.endurance += CrystalEnchantedHelmet.Resistance / 100f;
        if (currentHealth <= threshold)
        {
            Player.endurance += CrystalEnchantedHelmet.Resistance / 100f;

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                    DustID.Harpy, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 105, Color.Aqua, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}