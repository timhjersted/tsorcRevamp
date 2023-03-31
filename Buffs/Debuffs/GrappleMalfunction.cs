using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    class GrappleMalfunction : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Grapple Malfunction");
            Description.SetDefault("Something is causing your grapple to sieze up!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }


        public override void Update(Player player, ref int buffIndex)
        {
            player.grappling[0] = -1;
            player.grapCount = 0;
            for (int p = 0; p < 1000; p++)
            {
                if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7)
                {
                    Main.projectile[p].Kill();
                }
            }
        }
    }
}
