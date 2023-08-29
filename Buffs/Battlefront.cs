using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class Battlefront : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Battlefront");
        Description.SetDefault("You've never felt more ready for a fight...");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetDamage(DamageClass.Generic) += 0.15f;
        player.GetCritChance(DamageClass.Generic) += 15;
        player.thorns += 2f;
        player.enemySpawns = true;
    }
}
