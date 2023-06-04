using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class PlasmaWhirlwindDash : ModBuff
    {
        public static float MeleeDamage = 100f;
        public static float PercentHealthDamage = 3.4f;
        public static int HealthDamageCap = 150;
        public override LocalizedText Description => base.Description.WithFormatArgs(MeleeDamage, PercentHealthDamage, HealthDamageCap);
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
                player.GetDamage(DamageClass.Melee) += MeleeDamage / 100f;
                player.GetModPlayer<tsorcRevampPlayer>().SweepingBladeDamage = true;
            }
        }
    }
}
