using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Other.Trinity;

public class TrinityPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (Equipped)
        {
            Player.AddBuff(BuffID.RapidHealing, 120);
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.Distance(Player.Center) <= 320f) 
                {
                    npc.AddBuff(BuffID.Venom, 240);

                    if (Main.rand.NextFloat() < 0.33f)
                    {
                        npc.AddBuff(BuffID.Frozen, 90);
                    }
                }
            }
        }
    }
}