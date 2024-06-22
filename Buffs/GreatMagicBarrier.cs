using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GreatMagicBarrier : ModBuff
    {
        public static int Defense = 60;
        public override LocalizedText Description => base.Description.WithFormatArgs(Defense);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += Defense;
            Lighting.AddLight(player.Center, .7f, .7f, .45f);
            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GreatMagicBarrier>()] == 0)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.GreatMagicBarrier>(), 0, 0f, player.whoAmI);
            }
        }
    }
}