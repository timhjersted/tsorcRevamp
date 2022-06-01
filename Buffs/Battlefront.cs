using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Battlefront : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Battlefront");
            Description.SetDefault("You feel ready for battle");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 17;
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetCritChance(DamageClass.Magic) += 6;
            player.GetCritChance(DamageClass.Melee) += 6;
            player.GetCritChance(DamageClass.Ranged) += 6;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.pickSpeed += 0.2f;
            player.thorns += 2f;
            player.enemySpawns = true;
        }
    }
}
