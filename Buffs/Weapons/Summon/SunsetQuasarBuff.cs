using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Summon.SunsetQuasar;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class SunsetQuasarBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SunsetQuasarToken>()] > 0)
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