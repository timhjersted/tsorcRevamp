using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Ranged.BlueTearstoneRing;

public class BlueTearstoneRingProj : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public int Timer;

    public override bool PreAI(Projectile projectile)
    {
        Player player = Main.player[projectile.owner];
        if (projectile.DamageType == DamageClass.Ranged && player.GetModPlayer<BlueTearstoneRingPlayer>().Equipped)
        {
            Timer++;
        }
        return base.PreAI(projectile);
    }

    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        Player player = Main.player[projectile.owner];
        if (projectile.DamageType == DamageClass.Ranged && player.GetModPlayer<BlueTearstoneRingPlayer>().Equipped)
        {
            modifiers.ScalingBonusDamage += MathF.Min(BlueTearstoneRing.DmgCap / 100f, (BlueTearstoneRing.DmgPerTick * Timer) / 100f);
        }
    }
}