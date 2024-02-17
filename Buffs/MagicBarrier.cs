using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class MagicBarrier : ModBuff
    {
        public static int DefenseIncrease = 20;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += DefenseIncrease;
            Lighting.AddLight(player.Center, .450f, .450f, .600f);
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.Barrier>(), 0, 0f, player.whoAmI);
            }
        }
    }
}