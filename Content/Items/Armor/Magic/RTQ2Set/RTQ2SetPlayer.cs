using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Magic.RTQ2Set;

public class RTQ2SetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;

    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyRTQ2SetBonus(in int currentHealth, in int threshold)
    {
        Player.GetAttackSpeed(DamageClass.Magic) += RTQ2Chestplate.AtkSpeed / 100f;
        Player.spaceGun = true;
        if (currentHealth <= threshold)
        {
            Player.statDefense += RTQ2Chestplate.Defense;

            if (Visual)
            {
                int dust2 = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                    DustID.RedTorch, (Player.velocity.X) + (Player.direction * 3), Player.velocity.Y, 100, Color.Red, 3.0f);
                Main.dust[dust2].noGravity = true;
            }
        }

        if (Visual)
        {
            int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                DustID.RedTorch, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;
        }
    }
}