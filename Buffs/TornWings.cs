using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class TornWings : ModBuff {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Torn Wings");
            Description.SetDefault("You can't fly!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player P, ref int buffIndex) 
        {
            P.GetModPlayer<tsorcRevampPlayer>().TornWings = true;
        }
    }
}