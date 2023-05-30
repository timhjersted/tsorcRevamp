using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GreatMagicShield : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 25;
            player.GetDamage(DamageClass.Generic) *= 0.8f;
            player.moveSpeed *= 0.85f;

            Lighting.AddLight(player.Center, .400f, .400f, .700f);
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.GreatMagicShield>(), 0, 0f, player.whoAmI);
        }
    }
}