using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Summon.Archer;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class NondescriptOwlBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<NondescriptOwlProjectile>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
