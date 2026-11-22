using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Accessories.Damage.Trinity;

public class TrinityGlobalItem : GlobalItem
{
    public override bool? UseItem(Item item, Player player)
    {
        var modPlayer = player.GetModPlayer<TrinityPlayer>();
        
        if (modPlayer.Equipped && !player.HasBuff(ModContent.BuffType<TrinityCooldown>()) && !player.HasBuff(ModContent.BuffType<TrinityBuff>()))
        {
            if (player.HeldItem.DamageType == DamageClass.Melee || player.HeldItem.DamageType == DamageClass.MeleeNoSpeed)
            {
                modPlayer.Buffed1 = DamageClass.Ranged;
                modPlayer.Buffed2 = DamageClass.Magic;
                player.AddBuff(ModContent.BuffType<TrinityBuff>(), TrinityItem.Duration * 60);
            }
            else if (player.HeldItem.DamageType == DamageClass.Ranged)
            {
                modPlayer.Buffed1 = DamageClass.Melee;
                modPlayer.Buffed2 = DamageClass.Magic;
                player.AddBuff(ModContent.BuffType<TrinityBuff>(), TrinityItem.Duration * 60);
            }
            else if (player.HeldItem.DamageType == DamageClass.Magic || player.HeldItem.DamageType == DamageClass.MagicSummonHybrid)
            {
                modPlayer.Buffed1 = DamageClass.Melee;
                modPlayer.Buffed2 = DamageClass.Ranged;
                player.AddBuff(ModContent.BuffType<TrinityBuff>(), TrinityItem.Duration * 60);
            }
        }
        return base.UseItem(item, player);
    }
}