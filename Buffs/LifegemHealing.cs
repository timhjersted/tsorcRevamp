using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class LifegemHealing : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lifegem Healing");
            Description.SetDefault("Health is slowly being restored");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().LifegemHealing = true;

            if (Main.rand.NextBool(4))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X, player.position.Y + 10), player.width, player.height, 43, 0, -1f, 100, Color.White, Main.rand.NextFloat(.8f, 1f))];
                dust.noGravity = true;
                dust.velocity.X *= 0;
            }
        }
    }
}
