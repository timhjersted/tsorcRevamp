using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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
        public const float ShunpoBlinkImmunityTime = 0.5f; //in seconds
        public const int Cooldown = 20; //in seconds
        public override void Update(Player player, ref int buffIndex)
        {
            player.immune = true;
            player.GetModPlayer<tsorcRevampPlayer>().ShunpoTimer--;
        }
    }
}
