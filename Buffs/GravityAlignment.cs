using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GravityAlignment : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            int tilePos = (int)player.position.X / 16;
            bool ocean = tilePos < 750 || tilePos > Main.maxTilesX - 750;
            bool underground = (player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); // Magic number

            if (((underground && player.ZoneHallow && !ocean && !player.ZoneDungeon) || player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                player.gravControl = true;
            }
        }
    }
}
