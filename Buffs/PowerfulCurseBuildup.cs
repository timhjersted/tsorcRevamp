using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Buffs {
    class PowerfulCurseBuildup : ModBuff {

        public override void SetDefaults() {
            DisplayName.SetDefault("Powerful Curse Buildup");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void ModifyBuffTip(ref string tip, ref int rare) {
            tip = "When the counter reaches 500, something terrible happens. Curse buildup is at " + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel;
        }
        public override void Update(Player player, ref int buffIndex) {
            if ((500 <= player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel) && player.statLifeMax > 100) { //if curse is 500 or above, and the player has max hp to lose
                player.statLifeMax -= 100;
                Main.NewText("You have been cursed! -100 HP!");
                player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel = 0; //Reset to 0
                player.AddBuff(ModContent.BuffType<Invincible>(), 1200); //20 seconds
                player.AddBuff(ModContent.BuffType<Strength>(), 7200); //2 minutes
                player.AddBuff(ModContent.BuffType<CrimsonDrain>(), 10800); //3 minutes
                player.AddBuff(BuffID.Clairvoyance, 36000); //10 minutes
            }
        }

        public override bool ReApply(Player player, int time, int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += Main.rand.Next(4, 10); //was 6, 14
            return true;
        }
    }
}
