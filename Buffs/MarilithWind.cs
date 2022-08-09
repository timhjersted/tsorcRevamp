using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MarilithWind : ModBuff
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Storm Wind");
            Description.SetDefault("You're being blown by fierce wind!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
