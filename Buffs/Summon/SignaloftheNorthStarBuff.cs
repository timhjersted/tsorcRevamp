using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class SignaloftheNorthStarBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("North Star");
			Description.SetDefault("The North Star will fight for you");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(66); //66 is 66% of the base dmg of Polaris Leash
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Whips.SignaloftheNorthStarNorthStar>()] == 0)
                {
                    Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.SignaloftheNorthStarNorthStar>(), whipDamage, 1f, Main.myPlayer);
                }
            }
        }
    }
}
