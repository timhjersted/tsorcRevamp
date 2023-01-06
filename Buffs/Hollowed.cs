using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs; 
public class Hollowed : ModBuff {
    public override string Texture => "tsorcRevamp/Buffs/EmptyBuff_Grey";
    public override void SetStaticDefaults() {
        DisplayName.SetDefault("Hollowed");
        Description.SetDefault("Max HP reduced by 20%");
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex) {
        player.buffTime[buffIndex] = 2;
        player.GetModPlayer<tsrHollowedPlayer>().Hollowed = true;

    }
    private class tsrHollowedPlayer : ModPlayer {
        public bool Hollowed = false;
        public override void ResetEffects() {
            Hollowed = false;
        }
        public override void PostUpdate() {
            if (Hollowed) {
                //casting is ugly
                int hp = Player.statLifeMax2;
                hp *= 4;
                hp /= 5;
                Player.statLifeMax2 = hp;
            }
            Player.statLife = System.Math.Min(Player.statLife, Player.statLifeMax2);
        }
    }
}

