using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Shield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
            Description.SetDefault("Defense is increased by 62!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 62;
            Lighting.AddLight(player.Center, .7f, .7f, .45f);
            Projectile.NewProjectile(player.GetSource_Buff(Type), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), player.velocity.X, player.velocity.Y, Mod.Find<ModProjectile>("Shield").Type, 0, 0f, player.whoAmI, 0f, 0f);
        }
    }
}