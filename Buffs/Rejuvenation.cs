using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class Rejuvenation : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Rejuvenation");
        Description.SetDefault("Healing wounds... so they may not open again");
        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.lifeRegen < 0)
        {
            player.lifeRegen = 0;
        }
        player.lifeRegen *= 2;
        player.lifeRegen += 50;
        player.endurance -= 1f;
        Dust.NewDust(player.TopLeft, player.width, player.height, DustID.DryadsWard);
    }
}
