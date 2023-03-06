using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class GreenBlossom : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Green Blossom");
            // Description.SetDefault("Stamina recovery speed increased!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += 0.3f;

            /*if (Main.rand.NextBool(4))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X, player.position.Y + 10), player.width, player.height, 43, 0, -1f, 100, Color.White, Main.rand.NextFloat(.8f, 1f))];
                dust.noGravity = true;
                dust.velocity.X *= 0;
            }*/
        }
    }
}
