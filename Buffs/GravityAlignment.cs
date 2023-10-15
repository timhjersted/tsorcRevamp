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
            player.gravControl = true;
        }

        private class tsrGravityAlignmentPlayer : ModPlayer
        {
            public override void PostUpdateEquips()
            {
                int tilePos = (int)Player.position.X / 16;
                bool ocean = tilePos < 750 || tilePos > Main.maxTilesX - 750;
                bool underground = (Player.position.Y >= (Main.maxTilesY / 2.43309f) * 16); // Magic number

                if (((underground && Player.ZoneHallow && !ocean && !Player.ZoneDungeon) || Player.ZoneMeteor) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    Player.AddBuff(ModContent.BuffType<GravityAlignment>(), 2);
                }
            }
        }
    }
}
