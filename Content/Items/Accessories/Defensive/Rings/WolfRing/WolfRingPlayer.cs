using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings.WolfRing;

public class WolfRingPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public void Update()
    {
        if (Equipped)
        {
            Player.buffImmune[BuffID.Frozen] = true;
            Player.buffImmune[BuffID.Blackout] = true;
            Player.buffImmune[BuffID.Obstructed] = true;
            Player.buffImmune[BuffID.Venom] = true;
            if (Player.GetModPlayer<tsorcRevampPlayer>().EnterTheAbyss)
            {
                Player.statDefense += WolfRingItem.AbyssDef;
            }
        }
    }

    public override void PostUpdateEquips()
    {
        Update();
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (tsorcRevamp.WolfRing.JustReleased)
        {
            if (Equipped && !Player.HasBuff(ModContent.BuffType<RejuvenationCooldown>()))
            {
                Player.AddBuff(ModContent.BuffType<Rejuvenation>(), 5 * 60);
                Player.AddBuff(ModContent.BuffType<RejuvenationCooldown>(), 25 * 60);
            }
        }
    }
}