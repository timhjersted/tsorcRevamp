using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Items.Armor.Melee.MagmaSet;

public class MagmaSetPlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Equipped && (target.HasBuff(BuffID.OnFire) || target.HasBuff(BuffID.OnFire3)))
        {
            target.AddBuff(ModContent.BuffType<Ignited>(), MagmaHelmet.DebuffDuration * 60);
        }
    }
}