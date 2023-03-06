using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs {
    public class GravityField : ModBuff {

        public override void SetStaticDefaults() {
            // DisplayName.SetDefault("Gravity Field");
            // Description.SetDefault("The active boss is negating the low gravity of space!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().GravityField = true;
        }
    }
}
