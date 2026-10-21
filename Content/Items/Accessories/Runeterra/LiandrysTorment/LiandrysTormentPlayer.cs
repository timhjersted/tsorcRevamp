using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.LiandrysTorment;

public class LiandrysTormentPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped && (hit.DamageType == DamageClass.Magic || hit.DamageType == DamageClass.MagicSummonHybrid))
        {
            target.AddBuff(ModContent.BuffType<LiandrysTormentBurn>(), LiandrysTormentItem.BurnDuration * 60);
        }
    }
}