using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class NightsCrackerBuff : ModBuff
	{
		public int attackspeed;
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Night Crack");
			// Description.SetDefault("+6% summon attack speed");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			attackspeed = Projectiles.Summon.Whips.NightsCrackerProjectile.NightCharges * 6;
			player.GetAttackSpeed(DamageClass.Summon) += attackspeed / 100;
		}
		public override void ModifyBuffTip(ref string tip, ref int rare)
		{
			tip = $"+{attackspeed}% summon attack speed";
		}
	}
}
