using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class LesserVesselBuff : ModBuff
    {
        // Reuse the Archer summon buff icon until bespoke art exists.
        public override string Texture => "tsorcRevamp/Buffs/Weapons/Summon/ArcherSpiritBuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false; // show the 2.5-minute countdown
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int minionType = ModContent.ProjectileType<Projectiles.Summon.VesselOfSouls.LesserVessel>();
            int maxTimeLeft = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == minionType
                    && proj.ModProjectile is Projectiles.Summon.VesselOfSouls.LesserVessel v && v.lifetime > maxTimeLeft)
                {
                    maxTimeLeft = v.lifetime;
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
