using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class GreatMagicShield : ModBuff
    {
        public static int DefenseIncrease = 25;
        public static float DamagePenalty = 20f;
        public static float Slowness = 15f;
        public override LocalizedText Description => base.Description.WithFormatArgs(DefenseIncrease, DamagePenalty, Slowness);
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 25;
            player.GetDamage(DamageClass.Generic) *= 1f - DamagePenalty / 100f;
            player.moveSpeed *= 1f - Slowness / 100f;

            Lighting.AddLight(player.Center, .400f, .400f, .700f);
            Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, player.velocity, ModContent.ProjectileType<Projectiles.GreatMagicShield>(), 0, 0f, player.whoAmI);
        }
    }
}