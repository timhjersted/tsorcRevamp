using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    class PlasmaWhirlwindDash : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sweeping Blade");
            Description.SetDefault("Invulnerability and melee damage boost");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            int dustID = Dust.NewDust(player.position, player.width, player.height, DustID.CoralTorch, Scale: 3);
            Main.dust[dustID].noGravity = true;
            if (player.HeldItem.type == ModContent.ItemType<PlasmaWhirlwind>())
            {
                player.GetDamage(DamageClass.Melee) *= 3f;
            }
        }
    }
}
