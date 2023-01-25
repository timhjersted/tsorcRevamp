using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class WeightOfShadow : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Weight of Shadow");
            Description.SetDefault("The gravity of what you might become sets in...");
            Main.buffNoTimeDisplay[Type] = true;
            Main.debuff[Type] = true;
        }

        //Applies a downward force to the player and applies many (but not all) of the effects of crippled, dramatically reducing their vertical mobility
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.velocity.Y != 0)
            {
                player.velocity.Y += 0.5f;
            }

            player.GetModPlayer<tsorcRevampPlayer>().ShadowWeight = true;
        }

    }
}
