using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class Battlefront : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += BattlefrontPotion.DamageCritIncrease / 100f;
            player.GetCritChance(DamageClass.Generic) += BattlefrontPotion.DamageCritIncrease;
            player.thorns += BattlefrontPotion.Thorns / 100f;
            player.endurance -= BattlefrontPotion.ResistanceDecrease / 100f;
            player.enemySpawns = true;
        }
    }
}
