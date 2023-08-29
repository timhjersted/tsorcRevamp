using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs;

class EarthAlignment : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Alignment of the Earth");
        Description.SetDefault("The world's gravity offers no resistance to your flight \n" +
                               "The entire planet's energy is lifting you up to face to Attraidies' dark power...");

        Main.debuff[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
    }

    //Constantly sets the players wingtime to 60, making it infinite as long as they have the buff.
    public override void Update(Player player, ref int buffIndex)
    {
        player.wingTime = 200;
    }
}
