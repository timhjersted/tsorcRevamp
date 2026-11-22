using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.ShadowCloakSet;

public class ShadowCloakSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyShadowCloakSetBonus(in int currentHealth, in int threshold)
    {
        Player.GetJumpState(ExtraJump.SantankMount).Enable();
        Player.shadowArmor = true;
        if (currentHealth <= threshold)
        {
            Player.panic = true;

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                    DustID.VilePowder, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 245, Color.Violet, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        
        if (Main.rand.NextBool(7) && Visual)
        {
            int dust2 = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height,
                DustID.VilePowder, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 200, Color.Violet, 1.0f);
            Main.dust[dust2].noGravity = true;

            Main.dust[dust2].noGravity = true;
        }
    }
}