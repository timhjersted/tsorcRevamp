using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Summon.ShatteredReflection;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class ShatteredReflectionBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // If the minions exist reset the buff time, otherwise remove the buff from the player
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ShatteredReflectionProjectile>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
                player.GetModPlayer<tsorcRevampPlayer>().SetAuraState(tsorcAuraState.Nebula);
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}