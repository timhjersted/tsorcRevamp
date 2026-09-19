using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Potions;

namespace tsorcRevamp.Buffs
{
    public class SoulSiphon : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon = true;
            player.GetModPlayer<DarkSoulPlayer>().SoulPickupRange += 5;
            player.GetModPlayer<DarkSoulPlayer>().ConsSoulChanceMult += SoulSiphonPotion.ConsSoulChanceAmplifier / 5;
        }
    }
}
