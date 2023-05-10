using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    class NightbringerDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            int dustID = Dust.NewDust(player.position, player.width, player.height, DustID.Torch, Scale: 3);
            Main.dust[dustID].noGravity = true;
            if (player.HeldItem.type == ModContent.ItemType<Nightbringer>())
            {
                player.GetDamage(DamageClass.Melee) += 1f;
                player.GetModPlayer<tsorcRevampPlayer>().SweepingBladeDamage = true;
            }
        }
    }
}
