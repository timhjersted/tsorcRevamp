using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.YoungHunter;
using tsorcRevamp.Items.Weapons.Summon;

namespace tsorcRevamp.Buffs.Accessories
{
    public class YoungHunterBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // If the minions exist reset the buff time, otherwise remove the buff from the player
            if (player.ownedProjectileCounts[ModContent.ProjectileType<YoungHunter>()] > 0)
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