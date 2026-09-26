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
        if (Equipped && Player.HasBuff(ModContent.BuffType<Abyss>()))
        {
            UsefulFunctions.AddPlayerBuffDuration(Player, ModContent.BuffType<Abyss>(), -99999999);
        }
    }
}