using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.CovenantOfArtorias;

public class CovenantOfArtoriasPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void PostUpdateEquips()
    {
        if (!Equipped)
        {
            return;
        }

        int abyssBuffType = ModContent.BuffType<Abyss>();

        // Immunity, not a shortened timer: buffImmune is cleared before UpdateBuffs and set again here (after it),
        // so it stands through the NPC/projectile phase of the same frame and AddBuff refuses the debuff outright.
        // The old approach pinned the timer to 1, but Abyss.Update still ran its penalties every tick the debuff
        // was re-applied (Artorias' surge re-applies it every tick), and the icon and net packets never went away.
        // EnterTheAbyss (the visuals/drops/world state) is set separately by the ring item, so it is unaffected.
        Player.buffImmune[abyssBuffType] = true;

        // Immunity only blocks new applications; lift a debuff that was already on when the ring was equipped.
        if (Player.HasBuff(abyssBuffType))
        {
            Player.ClearBuff(abyssBuffType);
        }
    }
}
