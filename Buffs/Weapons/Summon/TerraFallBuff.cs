using Humanizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.TerraFall;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Weapons.Summon
{
    public class TerraFallBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[ModContent.ProjectileType<TerraFallTerraprisma>()] == 0)
            {
                Projectile.NewProjectileDirect(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<TerraFallTerraprisma>(), 1, 1f, Main.myPlayer);
            }
        }
    }
}
