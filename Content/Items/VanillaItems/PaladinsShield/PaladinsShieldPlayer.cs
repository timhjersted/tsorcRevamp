using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Friendly;

namespace tsorcRevamp.Content.Items.VanillaItems.PaladinsShield;

public class PaladinsShieldPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void ApplyPaladinsShield()
    {
        Player.hasPaladinShield = true;
        if (Player.whoAmI != Main.myPlayer && Player.miscCounter % 10 == 0)
        {
            int myPlayer = Main.myPlayer;
            if (Main.player[myPlayer].team == Player.team && Player.team != 0)
            {
                float num4 = Player.position.X - Main.player[myPlayer].position.X;
                float num2 = Player.position.Y - Main.player[myPlayer].position.Y;
                if ((float)Math.Sqrt(num4 * num4 + num2 * num2) < 800f)
                {
                    Main.player[myPlayer].AddBuff(ModContent.BuffType<PaladinsShieldBuff>(), 20);
                }
            }
        }
    }

    public override void Load()
    {
        On_Player.ApplyEquipFunctional += BlockVanillaPaladinShield;
    }

    private void BlockVanillaPaladinShield(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
    {
        orig(self, currentItem, hideVisual);
        if (self.HasBuff(BuffID.PaladinsShield))
        {
            self.ClearBuff(BuffID.PaladinsShield);
        }
        if (currentItem.type == ItemID.PaladinsShield || currentItem.type == ItemID.FrozenShield || currentItem.type == ItemID.HeroShield)
        {
            self.hasPaladinShield = false;
        }
    }
}