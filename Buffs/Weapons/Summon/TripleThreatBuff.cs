using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.TripleThreat;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class TripleThreatBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // If the minions exist reset the buff time, otherwise remove the buff from the player
            bool anyRetinazer = player.ownedProjectileCounts[ModContent.ProjectileType<FriendlyRetinazer>()] > 0;
            bool anySpazmatism = player.ownedProjectileCounts[ModContent.ProjectileType<FriendlySpazmatism>()] > 0;
            bool anyCataluminance = player.ownedProjectileCounts[ModContent.ProjectileType<FriendlyCataluminance>()] > 0;
            if (anyRetinazer || anySpazmatism || anyCataluminance)
            {
                player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.TripleThreat);
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