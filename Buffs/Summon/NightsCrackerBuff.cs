using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Summon
{
	public class NightsCrackerBuff : ModBuff
	{
		public float AttackSpeed;
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = false;
		}

        public override void Update(Player player, ref int buffIndex)
        {
			AttackSpeed = player.GetModPlayer<tsorcRevampPlayer>().NightsCrackerStacks * 6f;
			player.GetAttackSpeed(DamageClass.Summon) += AttackSpeed / 100;
		}
		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
		{
			tip = $"+{AttackSpeed}% summon attack speed";
		}
	}
}
