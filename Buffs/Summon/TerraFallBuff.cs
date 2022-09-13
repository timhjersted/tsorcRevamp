using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class TerraFallBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Gaia's Fall");
			Description.SetDefault("+25% whip speed\nTerra Energy fights for you");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}
		public override void Update(Player player, ref int buffIndex)
        {
			player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.25f;
            if (player.whoAmI == Main.myPlayer)
            {
                int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(115); //115 is the base dmg of Terra Fall
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>()] == 0)
                {
                    Projectile.NewProjectileDirect(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>(), whipDamage, 1f, Main.myPlayer);
                }
            }
        }
    }
}
