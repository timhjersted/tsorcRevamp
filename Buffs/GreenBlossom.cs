using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GreenBlossom : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.3f;
        }
    }
}
