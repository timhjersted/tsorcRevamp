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
            Main.buffNoTimeDisplay[Type] = false; // Display the countdown timer!
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int minionType = ModContent.ProjectileType<Projectiles.Summon.Archer.ArcherSpirit>();
            int maxTimeLeft = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == minionType)
                {
                    if (proj.ModProjectile is Projectiles.Summon.Archer.ArcherSpirit spirit)
                    {
                        if (spirit.lifetime > maxTimeLeft)
                        {
                            maxTimeLeft = spirit.lifetime;
                        }
                    }
                }
            }

            if (maxTimeLeft > 0)
            {
                player.buffTime[buffIndex] = maxTimeLeft;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
