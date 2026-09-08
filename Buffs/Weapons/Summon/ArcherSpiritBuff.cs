using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.Archer;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class ArcherSpiritBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true; // Permanent like a vanilla minion - no countdown to show.
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int minionType = ModContent.ProjectileType<Projectiles.Summon.Archer.ArcherSpirit>();
            bool minionAlive = false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == minionType)
                {
                    minionAlive = true;
                    break;
                }
            }

            if (minionAlive)
            {
                // Matches CheckActive's own Projectile.timeLeft refresh - the buff never actually
                // counts down to 0 on its own while a spirit is alive to keep re-arming it.
                player.buffTime[buffIndex] = 2;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
