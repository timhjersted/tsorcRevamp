using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
    public class NondescriptOwlBuff : ModBuff 
    {
        public override void SetStaticDefaults() 
        {
            DisplayName.SetDefault("Owl Archer");
            Description.SetDefault("The owl will fire at your enemies!");
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) 
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Archer.NondescriptOwlProjectile>()] > 0) 
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
