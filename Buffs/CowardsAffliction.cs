using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class CowardsAffliction : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Coward's Affliction");
            Description.SetDefault("Do not flee from the Lord of Cinder.");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            canBeCleared = false;
            
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().CowardsAffliction = true;
        }
    }
}
