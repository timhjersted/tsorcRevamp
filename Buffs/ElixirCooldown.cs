using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class ElixirCooldown : ModBuff {

        public override void SetDefaults() {
            DisplayName.SetDefault("Elixir Cooldown");
            Description.SetDefault("You cannot drink Holy War Elixirs!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            canBeCleared = false; //prevents nurse clearing
        }
    }
}
