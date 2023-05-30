using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class PlasmaWhirlwindDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.CoralTorch, Scale: 3f);
            dust.noGravity = true;

            if (player.HeldItem.type == ModContent.ItemType<PlasmaWhirlwind>())
            {
                player.GetDamage(DamageClass.Melee) += 1f;
                player.GetModPlayer<tsorcRevampPlayer>().SweepingBladeDamage = true;
            }
        }
    }
}
