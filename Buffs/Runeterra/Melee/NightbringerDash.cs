using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class NightbringerDash : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs(PlasmaWhirlwindDash.MeleeDamage, PlasmaWhirlwindDash.PercentHealthDamage, PlasmaWhirlwindDash.HealthDamageCap);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            var dust = Dust.NewDustDirect(player.position, player.width, player.height, DustID.Torch, Scale: 3f);
            dust.noGravity = true;

            if (player.HeldItem.type == ModContent.ItemType<Nightbringer>())
            {
                player.GetDamage(DamageClass.Melee) += PlasmaWhirlwindDash.MeleeDamage / 100f;
                player.GetModPlayer<tsorcRevampPlayer>().SweepingBladeDamage = true;
            }
        }
    }
}
