using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Armor
{
    public class ArtoriasAbysswalkerPoise : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}