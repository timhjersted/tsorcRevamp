using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon; 
public class SunsetQuasarBuff : ModBuff {
    public override void SetStaticDefaults() {
        DisplayName.SetDefault("Sunset Quasar");
        Description.SetDefault("The small creature will fight to protect you");

        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true; 
    }

    public override void Update(Player player, ref int buffIndex) {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.SunsetQuasar.SunsetQuasarToken>()] > 0) {
            player.buffTime[buffIndex] = 18000;
        }
        else {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }
}