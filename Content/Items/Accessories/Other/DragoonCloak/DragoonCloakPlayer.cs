using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;

namespace tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak;

public class DragoonCloakPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyDragoonCloak(in int currentHealth, in int threshold, float efficiency = 1f)
    {
        var lightCloakPlayer = Player.GetModPlayer<LightCloakPlayer>();
        lightCloakPlayer.Visual = false;
        
        var darkCloakPlayer = Player.GetModPlayer<DarkCloakPlayer>();
        
        var shadowmoonCloakPlayer = Player.GetModPlayer<ShadowmoonCloakPlayer>();
        shadowmoonCloakPlayer.Visual = false;

        if (currentHealth <= threshold)
        {
            lightCloakPlayer.ApplyLightCloak(currentHealth, threshold, efficiency);
            darkCloakPlayer.ApplyDarkCloak(currentHealth, threshold, efficiency);
            shadowmoonCloakPlayer.ApplyShadowMoonCloak(currentHealth, threshold, efficiency);
        }

        if (Visual)
        {
            int i2 = (int)(Player.position.X + (float)(Player.width / 2) + (float)(8 * Player.direction)) / 16;
            int j2 = (int)(Player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
            if (currentHealth <= threshold)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height, 21, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 245, Color.White, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        
        lightCloakPlayer.Equipped = false;
        darkCloakPlayer.Equipped = false;
        shadowmoonCloakPlayer.Equipped = false;
    }
}