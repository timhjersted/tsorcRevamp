using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class ScoutsBoost2 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1.4f;
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate *= 1.2f;
        }
    }
}
