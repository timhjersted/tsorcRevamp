using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class StarlightShardRestoration : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().StarlightShardRestoration = true;

            if (Main.rand.NextBool(4))
            {
                Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X, player.position.Y + 10), player.width, player.height, 15, 0, -1f, 100, Color.White, Main.rand.NextFloat(.8f, 1f))]; //DustID 15 is for magic dust
                dust.noGravity = true;
                dust.velocity.X *= 0;
            }
        }
    }
}
