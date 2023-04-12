using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MarilithHold : ModBuff
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Held");
            // Description.SetDefault("You can't move!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
