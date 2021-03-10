using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class CurseBuildup : ModBuff {

        public override void SetDefaults() {
            DisplayName.SetDefault("Curse Buildup");
            Description.SetDefault("When the counter reaches 30, something bad happens. Curse buildup is at "/* + level*/);
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
       /* public override void Update(Player player, ref int buffIndex) {
            if ((29 < Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel < 39) && player.statLifeMax > 20) { //if curse is between 30 and 39 inclusive, and the player has max hp to lose
                player.statLifeMax -= 20;
                Main.NewText("You have been cursed! -20 HP!");
                Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel = 39; //set it to 40. dont curse the player twice for one curse event.
            }
            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel > 90) {
                Main.NewText("You feel powerful!");
                player.AddBuff(ModContent.BuffType<Invincible>(), 600, false);
                player.AddBuff(ModContent.BuffType<Strength>(), 2600, false);
                Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel = 0;
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex) {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel += Main.rand.Next(4, 9);
            return true;
        }*/
    }
}
