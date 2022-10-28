using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class MagicShield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Shield");
            Description.SetDefault("Defense is increased by 10!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 10;
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), player.velocity.X, player.velocity.Y, ModContent.ProjectileType<Projectiles.MagicShield>(), 0, 0, player.whoAmI, 0, 0f);
        }
    }
}