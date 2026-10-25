using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.Magic.DragonScaleSet;

public class DragonScaleSetPlayer : ModPlayer
{
    public bool Equipped;
    public bool Visual;
    public override void ResetEffects()
    {
        Equipped = false;
        Visual = false;
    }

    public void ApplyDragonScaleSetBonus(in int currentHealth, in int threshold)
    {
        Player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
        if (currentHealth <= threshold)
        {
            Player.GetCritChance(DamageClass.Magic) += DragonScaleMail.SetBonusDmgCritChance;
            Player.GetDamage(DamageClass.Magic) += DragonScaleMail.SetBonusDmgCritChance / 100f;
            Player.manaRegenBonus += DragonScaleMail.ManaRegen;
            Player.starCloakItem = new Item(ItemID.StarCloak);

            if (Visual)
            {
                int dust = Dust.NewDust(new Vector2((float)Player.position.X, (float)Player.position.Y), Player.width, Player.height, 
                    DustID.DemonTorch, (Player.velocity.X) + (Player.direction * 1), Player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}