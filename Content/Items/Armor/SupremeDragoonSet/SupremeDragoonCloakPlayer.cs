using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak;

namespace tsorcRevamp.Content.Items.Armor.SupremeDragoonSet;

public class SupremeDragoonCloakPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void ApplySupremeDragoonCloak(in int currentHealth, in int threshold,
        float efficiency = 1f)
    {
        var dragoonCloakPlayer = Player.GetModPlayer<DragoonCloakPlayer>();
        dragoonCloakPlayer.Visual = false;
        bool hasNoDecorativeShirt = Player.armor[11].IsAir;
        
        dragoonCloakPlayer.ApplyDragoonCloak(currentHealth, threshold, efficiency);

        if (hasNoDecorativeShirt)
        {
            int dust = Dust.NewDust(new Vector2(Player.position.X, Player.position.Y), Player.width, Player.height, 21, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 150, Color.White, 0.5f);
            Main.dust[dust].noGravity = true;
        }
        
        dragoonCloakPlayer.Equipped  = false;
    }
}