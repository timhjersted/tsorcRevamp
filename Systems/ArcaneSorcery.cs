using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems;

public class ArcaneSorceryPlayer : ModPlayer
{
    public bool Enabled = false;

    public override void ResetEffects()
    {
        Enabled = false;
    }

    public int MaxManaMult = 5;
    
    public const float CeruleanFlaskMaxManaScaling = 25f;
    public float MagicDamageAmp = 15f;
    public float MagicAttackSpeedAmp = 15f;

    public override void PostUpdateEquips()
    {
        if (Enabled && !Player.HasBuff(BuffID.ManaSickness))
        {
            Player.GetDamage(DamageClass.Magic) *= 1f + (MagicDamageAmp / 100f);
            Player.GetAttackSpeed(DamageClass.Magic) *= 1f + (MagicAttackSpeedAmp / 100f);
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (Enabled)
        {
            Player.statManaMax2 *= MaxManaMult;
        }
    }
}
class ManaStarDrawAmount
{
    internal static void ApplyManaStarAmount()
    {
        On_PlayerStatsSnapshot.ctor += CustomManaStarAmount;
    }

    private static void CustomManaStarAmount(On_PlayerStatsSnapshot.orig_ctor orig, ref PlayerStatsSnapshot self, Player player)
    {
        orig(ref self, player);
        var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
        if (arcaneSorceryPlayer.Enabled)
        {
            self.AmountOfManaStars = player.statManaMax2 / (20 * arcaneSorceryPlayer.MaxManaMult);
        }
    }
}