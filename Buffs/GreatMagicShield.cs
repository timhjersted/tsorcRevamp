using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class GreatMagicShield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Magic Shield");
            Description.SetDefault("Defense increased by 25, but damage reduced by 20% and speed reduced by 15%!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 25;
            player.GetDamage(DamageClass.Generic) *= 0.8f;
            player.moveSpeed *= 0.85f;
            Lighting.AddLight(player.Center, .400f, .400f, .700f);
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.position.X + (float)(player.width / 2), player.position.Y + (float)(player.height / 2), player.velocity.X, player.velocity.Y, ModContent.ProjectileType<Projectiles.GreatMagicShield>(), 0, 0f, player.whoAmI, 0f, 0f);
        }
    }
}