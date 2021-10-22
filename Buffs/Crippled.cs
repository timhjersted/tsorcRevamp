using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Crippled : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Crippled");
            Description.SetDefault("Your mobility has been crippled!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player P, ref int buffIndex) 
        {
            P.GetModPlayer<tsorcRevampPlayer>().Crippled = true;
        }
    }
}