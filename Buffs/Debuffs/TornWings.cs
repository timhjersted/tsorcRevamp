using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    class TornWings : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player P, ref int buffIndex)
        {
            P.GetModPlayer<tsorcRevampPlayer>().TornWings = true;
        }
    }
}