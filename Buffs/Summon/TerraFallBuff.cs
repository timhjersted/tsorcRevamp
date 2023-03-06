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
        public int attackspeed;
        public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Terraria's Fall");
			// Description.SetDefault("+12% summon attack speed\nTerra Energy fights for you");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}
		public override void Update(Player player, ref int buffIndex)
        {
            int whipDamage = (int)player.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(115); //115 is the base dmg of Terra Fall
            attackspeed = Projectiles.Summon.Whips.TerraFallProjectile.TerraCharges * 12;
            player.GetAttackSpeed(DamageClass.Summon) += attackspeed / 100;
            if (player.whoAmI == Main.myPlayer)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>()] == 0)
                {
                    Projectile.NewProjectileDirect(player.GetSource_Buff(buffIndex), player.Center, Vector2.One, ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallTerraprisma>(), whipDamage, 1f, Main.myPlayer);
                }
            }
        }
        public override void ModifyBuffTip(ref string tip, ref int rare)
        {
            tip = $"+{attackspeed}% summon attack speed\nTerra Energy fights for you(Unimplemented)";
        }
    }
}
