using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Debuffs
{
    public class GrappleMalfunction : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.grappling[0] = -1;
            player.grapCount = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                var projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.aiStyle == ProjAIStyleID.Hook)
                {
                    projectile.Kill();
                }
            }
        }
    }
}
