using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class WeightOfShadow : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Applies a downward force to the player and applies many (but not all) of the effects of crippled, dramatically reducing their vertical mobility
            if (player.velocity.Y != 0)
            {
                player.velocity.Y += 0.5f;
            }

            player.GetModPlayer<tsorcRevampPlayer>().ShadowWeight = true;
        }

    }
}
