using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Wall : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Wall");
            Description.SetDefault("Defense is increased by 50!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 50;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Lighting.AddLight(player.Center, .400f, .400f, .700f);
                Projectile.NewProjectile(player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), player.velocity.X, player.velocity.Y, mod.ProjectileType("Wall"), 0, 0f, player.whoAmI, 0f, 0f);

            }
        }
    }
}