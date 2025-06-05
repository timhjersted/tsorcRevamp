using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Buffs.Weapons.Melee
{
    public class PilgrimSpontoonBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().PilgrimSpontoonBuff = true;
        }
    }
}