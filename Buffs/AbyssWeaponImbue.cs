using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
	public class AbyssWeaponImbue : ModBuff
	{
		public override void SetStaticDefaults() {
			BuffID.Sets.IsAFlaskBuff[Type] = true;
			Main.meleeBuff[Type] = true;
			Main.persistentBuff[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
            player.GetDamage(DamageClass.Melee) += AbyssFlask.DamageCritIncrease / 100f;
            player.GetCritChance(DamageClass.Melee) += AbyssFlask.DamageCritIncrease;
			player.GetDamage(DamageClass.SummonMeleeSpeed) += AbyssFlask.DamageCritIncrease / 100f;
            player.GetCritChance(DamageClass.SummonMeleeSpeed) += AbyssFlask.DamageCritIncrease;
			player.GetModPlayer<AbyssWeaponEnchantement>().abyssWeaponImbue = true;
			player.MeleeEnchantActive = true; 
		}
	}
}