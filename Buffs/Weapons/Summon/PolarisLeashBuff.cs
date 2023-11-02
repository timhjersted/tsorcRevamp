using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class PolarisLeashBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(PolarisLeashItem.BaseDamage * PolarisLeashItem.StarDamageScaling / 100f);
                if (player.ownedProjectileCounts[ModContent.ProjectileType<PolarisLeashPolaris>()] == 0)
                {
                    Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<PolarisLeashPolaris>(), whipDamage, 1f, Main.myPlayer);
                }
            }
        }
    }
}
