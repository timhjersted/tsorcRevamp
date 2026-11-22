using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;

public class LightCloakPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyLightCloak(in int currentHealth, in int threshold, float efficiency = 1f)
    {
        Player.lifeRegen += (int)(LightCloakItem.LifeRegen1 * efficiency);
        if (currentHealth <= threshold)
        {
            Player.lifeRegen += (int)((LightCloakItem.LifeRegen2 - LightCloakItem.LifeRegen1) * efficiency);

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y),
                    Player.width, Player.height, 21, (Player.velocity.X) + (Player.direction * 1),
                    Player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}