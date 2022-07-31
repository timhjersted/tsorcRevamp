using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon {
    public class ArcherBuff : ModBuff {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Archer");
            Description.SetDefault("The archer will fight for you");
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Archer>()] > 0) {
                player.buffTime[buffIndex] = 18000;
            }
            else {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
