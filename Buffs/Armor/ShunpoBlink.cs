using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Armor
{
    public class ShunpoBlink : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
        public const float ShunpoBlinkImmunityTime = 2f; //in seconds
        public const int Cooldown = 45; //in seconds
        public override void Update(Player player, ref int buffIndex)
        {
            player.immune = true;
            //player.GetModPlayer<tsorcRevampPlayer>().ShunpoTimer--;
        }
    }
}
