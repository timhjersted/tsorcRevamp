using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Buffs.Accessories
{
    public class TrinityBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen += 2;

            player.statDefense += 12;

            player.GetDamage(DamageClass.Generic) *= 1.15f;

            player.moveSpeed *= 1.2f;
            player.runAcceleration *= 1.2f;
        }
    }
}
