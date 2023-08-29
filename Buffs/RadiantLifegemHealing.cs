using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class RadiantLifegemHealing : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Radiant Lifegem Healing");
        Description.SetDefault("Health is slowly being restored");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<tsorcRevampPlayer>().RadiantLifegemHealing = true;

        if (Main.rand.NextBool(2))
        {
            Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X, player.position.Y + 10), player.width, player.height, 43, 0, -1.5f, 100, Color.White, Main.rand.NextFloat(1f, 1.2f))];
            dust.noGravity = true;
            dust.velocity.X *= 0;
        }
    }
}
